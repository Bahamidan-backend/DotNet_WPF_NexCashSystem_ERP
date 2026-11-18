using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Business.Services
{
    public class PurchaseReturnService : IPurchaseReturnService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PurchaseReturnService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PurchaseReturn> GetByIdAsync(int id)
        {
            return await _unitOfWork.PurchaseReturns.GetByIdAsync(id);
        }

        public async Task<IEnumerable<PurchaseReturn>> GetAllAsync()
        {
            return await _unitOfWork.PurchaseReturns.GetAllAsync();
        }

        public async Task<string> GenerateReturnNumberAsync()
        {
            return await _unitOfWork.PurchaseReturns.GenerateReturnNumberAsync();
        }

        public async Task<IEnumerable<PurchaseReturn>> SearchAsync(string searchTerm, DateTime? fromDate, DateTime? toDate)
        {
            return await _unitOfWork.PurchaseReturns.SearchAsync(searchTerm, fromDate, toDate);
        }

        public async Task<PurchaseReturn> ProcessReturnAsync(PurchaseReturn purchaseReturn)
        {
            if (purchaseReturn.OriginalPurchaseId == 0)
                throw new ArgumentException("يجب اختيار فاتورة المشتريات الأصلية.");

            if (purchaseReturn.PurchaseReturnItems == null || !purchaseReturn.PurchaseReturnItems.Any())
                throw new ArgumentException("يجب إضافة عناصر للإرجاع.");

            var originalPurchase = await _unitOfWork.Purchases.GetWithItemsAsync(purchaseReturn.OriginalPurchaseId);
            if (originalPurchase == null)
                throw new ArgumentException("فاتورة المشتريات الأصلية غير موجودة.");

            var now = DateTime.Now;
            purchaseReturn.TotalAmount = purchaseReturn.PurchaseReturnItems.Sum(ri => ri.TotalAmount);
            purchaseReturn.ReturnDate = now;

            if (string.IsNullOrWhiteSpace(purchaseReturn.ReturnNumber) ||
                await _unitOfWork.GetContext().PurchaseReturns.AsNoTracking().AnyAsync(r => r.ReturnNumber == purchaseReturn.ReturnNumber))
            {
                purchaseReturn.ReturnNumber = await _unitOfWork.PurchaseReturns.GenerateReturnNumberAsync();
                while (await _unitOfWork.GetContext().PurchaseReturns.AsNoTracking().AnyAsync(r => r.ReturnNumber == purchaseReturn.ReturnNumber))
                {
                    purchaseReturn.ReturnNumber = $"PR-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
                }
            }

            _unitOfWork.ClearEntityTracking();
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var context = _unitOfWork.GetContext();
                var origItemMap = originalPurchase.PurchaseItems.ToDictionary(pi => pi.Id);

                var neededWhIds = purchaseReturn.PurchaseReturnItems
                    .Select(ri => origItemMap.TryGetValue(ri.OriginalPurchaseItemId, out var oi) ? oi.ProductId : null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .Distinct()
                    .ToList();

                var neededAddonIds = purchaseReturn.PurchaseReturnItems
                    .Select(ri => origItemMap.TryGetValue(ri.OriginalPurchaseItemId, out var oi) ? oi.AddonId : null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .Distinct()
                    .ToList();

                var whProductsDict = neededWhIds.Count > 0
                    ? await context.warehouseFromProducts.Where(w => neededWhIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id)
                    : new Dictionary<int, WarehouseFromProduct>();

                var addonsDict = neededAddonIds.Count > 0
                    ? await context.Addons.Where(a => neededAddonIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id)
                    : new Dictionary<int, Addon>();

                var previousReturns = await context.PurchaseReturns
                    .AsNoTracking()
                    .Include(pr => pr.PurchaseReturnItems)
                    .Where(pr => pr.OriginalPurchaseId == purchaseReturn.OriginalPurchaseId)
                    .ToListAsync();

                var prevReturnedByItemId = previousReturns
                    .SelectMany(pr => pr.PurchaseReturnItems)
                    .GroupBy(pri => pri.OriginalPurchaseItemId)
                    .ToDictionary(g => g.Key, g => g.Sum(pri => pri.Quantity));

                foreach (var item in purchaseReturn.PurchaseReturnItems)
                {
                    if (!origItemMap.TryGetValue(item.OriginalPurchaseItemId, out var originalItem))
                        throw new InvalidOperationException($"بند المشتريات الأصلي برقم {item.OriginalPurchaseItemId} غير موجود.");

                    decimal alreadyReturned = prevReturnedByItemId.TryGetValue(item.OriginalPurchaseItemId, out var prevQty) ? prevQty : 0m;
                    decimal maxReturnable = Math.Max(0m, originalItem.Quantity - alreadyReturned);
                    if (item.Quantity <= 0 || item.Quantity > maxReturnable)
                    {
                        throw new InvalidOperationException(
                            $"الكمية المرتجعة ({item.Quantity:N0}) تتجاوز الكمية المتبقية القابلة للإرجاع في الفاتورة الأصلية ({maxReturnable:N0}).");
                    }

                    if (originalItem.ProductId.HasValue && whProductsDict.TryGetValue(originalItem.ProductId.Value, out var whProduct))
                    {
                        if (PurchaseService.TryDetectCarcass(whProduct.Name, originalItem.Notes, whProduct.PortionUnit, out string animalTypeName, out int portionsPerCarcass))
                        {
                            decimal totalPortionsToReturn = item.Quantity * portionsPerCarcass;
                            int targetWhId = whProduct.WarehouseId ?? originalPurchase.WarehouseId ?? 1;
                            string meatName = $"لحم {animalTypeName} (نفر/جزء)";
                            var targetMeat = await context.warehouseFromProducts
                                .FirstOrDefaultAsync(w => w.WarehouseId == targetWhId && (w.Name == meatName || w.Name == $"لحم {animalTypeName} (ثمن/جزء)"));

                            if (targetMeat != null && targetMeat.StockQuantity < totalPortionsToReturn)
                            {
                                throw new InvalidOperationException(
                                    $"لا يمكن إرجاع ذبيحة '{whProduct.Name}' للمورد لأن أجزاء اللحم تم بيعها أو استهلاكها بالفعل! " +
                                    $"الرصيد المتبقي بالمستودع: {targetMeat.StockQuantity:N0} جزء، والمطلوب للإرجاع: {totalPortionsToReturn:N0} جزء.");
                            }
                        }
                        else if (PurchaseService.TryDetectBagWeight(whProduct.Name, originalItem.Notes, out decimal bagWeightKg))
                        {
                            decimal requiredKgToReturn = item.Quantity * bagWeightKg;
                            if (whProduct.StockQuantity < requiredKgToReturn)
                            {
                                throw new InvalidOperationException(
                                    $"لا يمكن إرجاع المادة '{whProduct.Name}' للمورد لأن الكمية المطلوبة ({requiredKgToReturn:N0} كجم / {item.Quantity:N0} كيس) تتجاوز الرصيد المتوفر في المستودع ({whProduct.StockQuantity:N0} كجم).");
                            }
                        }
                        else
                        {
                            if (whProduct.StockQuantity < item.Quantity)
                            {
                                var unitName = whProduct.PortionUnit.HasValue ? PortionUnitHelper.GetDisplayName(whProduct.PortionUnit.Value) : string.Empty;
                                throw new InvalidOperationException(
                                    $"لا يمكن إرجاع المادة '{whProduct.Name}' للمورد لأن الكمية المطلوبة ({item.Quantity:N0} {unitName}) تتجاوز الرصيد المتوفر في المستودع ({whProduct.StockQuantity:N0} {unitName}). قد تكون البضاعة بيعت أو تم استهلاكها!");
                            }
                        }
                    }
                    else if (originalItem.AddonId.HasValue && addonsDict.TryGetValue(originalItem.AddonId.Value, out var addon))
                    {
                        if (addon.Quantity < item.Quantity)
                        {
                            throw new InvalidOperationException(
                                $"لا يمكن إرجاع الإضافة '{addon.Name}' للمورد لأن الكمية المطلوبة ({item.Quantity:N0}) تتجاوز الرصيد المتوفر ({addon.Quantity:N0}).");
                        }
                    }
                }

                foreach (var item in purchaseReturn.PurchaseReturnItems)
                {
                    if (!origItemMap.TryGetValue(item.OriginalPurchaseItemId, out var originalItem))
                        continue;

                    if (originalItem.ProductId.HasValue && whProductsDict.TryGetValue(originalItem.ProductId.Value, out var whProduct))
                    {
                        if (PurchaseService.TryDetectCarcass(whProduct.Name, originalItem.Notes, whProduct.PortionUnit, out string animalTypeName, out int portionsPerCarcass))
                        {
                            decimal N = item.Quantity;
                            decimal totalPortions = N * portionsPerCarcass;
                            int targetWhId = whProduct.WarehouseId ?? originalPurchase.WarehouseId ?? 1;

                            var compNames = new List<(string Name, decimal Quantity, decimal UnitCost)>
                            {
                                ($"لحم {animalTypeName} (نفر/جزء)", totalPortions, item.UnitPrice > 0 && portionsPerCarcass > 0 ? item.UnitPrice / portionsPerCarcass : 0m),
                                ($"رأس {animalTypeName}", N * 1m, 0m),
                                ($"كبدة {animalTypeName} (حبة)", N * 1m, 0m),
                                ($"ظهر {animalTypeName} (حبة)", N * 1m, 0m)
                            };

                            foreach (var (compName, compQty, compUnitCost) in compNames)
                            {
                                var target = await context.warehouseFromProducts
                                    .FirstOrDefaultAsync(w => w.WarehouseId == targetWhId && (w.Name == compName || w.Name == compName.Replace("(نفر/جزء)", "(ثمن/جزء)")));

                                if (target != null)
                                {
                                    decimal beforeQty = target.StockQuantity;
                                    target.StockQuantity -= compQty;
                                    decimal afterQty = target.StockQuantity;
                                    target.UpdatedAt = now;
                                    _unitOfWork.warehouseFromProduct.Update(target);

                                    await _unitOfWork.StockMovements.AddAsync(new StockMovement
                                    {
                                        WarehouseFromProductId = target.Id,
                                        WarehouseId = targetWhId,
                                        MovementType = StockMovementType.PurchaseReturn,
                                        Quantity = -compQty,
                                        BalanceBefore = beforeQty,
                                        BalanceAfter = afterQty,
                                        UnitCost = compUnitCost,
                                        TotalCost = compQty * compUnitCost,
                                        MovementDate = now,
                                        CreatedAt = now,
                                        ReferenceNumber = purchaseReturn.ReturnNumber,
                                        PurchaseId = purchaseReturn.OriginalPurchaseId,
                                        UserId = purchaseReturn.UserId,
                                        Notes = $"مرتجع مشتريات للمورد: إرجاع أجزاء ذبيحة #{purchaseReturn.ReturnNumber} ({compName})"
                                    });
                                }
                            }
                        }
                        else if (PurchaseService.TryDetectBagWeight(whProduct.Name, originalItem.Notes, out decimal bagWeightKg))
                        {
                            decimal returnKg = item.Quantity * bagWeightKg;
                            var beforeQty = whProduct.StockQuantity;
                            whProduct.StockQuantity -= returnKg;
                            decimal afterQty = whProduct.StockQuantity;
                            whProduct.UpdatedAt = now;
                            _unitOfWork.warehouseFromProduct.Update(whProduct);

                            await _unitOfWork.StockMovements.AddAsync(new StockMovement
                            {
                                WarehouseFromProductId = whProduct.Id,
                                WarehouseId = whProduct.WarehouseId,
                                MovementType = StockMovementType.PurchaseReturn,
                                Quantity = -returnKg,
                                BalanceBefore = beforeQty,
                                BalanceAfter = afterQty,
                                UnitCost = bagWeightKg > 0 ? (item.UnitPrice / bagWeightKg) : item.UnitPrice,
                                TotalCost = item.TotalAmount,
                                MovementDate = now,
                                CreatedAt = now,
                                ReferenceNumber = purchaseReturn.ReturnNumber,
                                PurchaseId = purchaseReturn.OriginalPurchaseId,
                                UserId = purchaseReturn.UserId,
                                Notes = $"مرتجع مشتريات للمورد ({item.Quantity:0.##} كيس = {returnKg:0.##} كجم): {purchaseReturn.Reason ?? "إرجاع بضاعة"}"
                            });
                        }
                        else
                        {
                            var beforeQty = whProduct.StockQuantity;
                            whProduct.StockQuantity -= item.Quantity;
                            decimal afterQty = whProduct.StockQuantity;
                            whProduct.UpdatedAt = now;
                            _unitOfWork.warehouseFromProduct.Update(whProduct);

                            await _unitOfWork.StockMovements.AddAsync(new StockMovement
                            {
                                WarehouseFromProductId = whProduct.Id,
                                WarehouseId = whProduct.WarehouseId,
                                MovementType = StockMovementType.PurchaseReturn,
                                Quantity = -item.Quantity,
                                BalanceBefore = beforeQty,
                                BalanceAfter = afterQty,
                                UnitCost = item.UnitPrice,
                                TotalCost = item.TotalAmount,
                                MovementDate = now,
                                CreatedAt = now,
                                ReferenceNumber = purchaseReturn.ReturnNumber,
                                PurchaseId = purchaseReturn.OriginalPurchaseId,
                                UserId = purchaseReturn.UserId,
                                Notes = $"مرتجع مشتريات للمورد: {purchaseReturn.Reason ?? "إرجاع بضاعة"}"
                            });
                        }
                    }
                    else if (originalItem.AddonId.HasValue && addonsDict.TryGetValue(originalItem.AddonId.Value, out var addon))
                    {
                        addon.Quantity -= item.Quantity;
                        addon.UpdatedAt = now;
                        _unitOfWork.Addons.Update(addon);
                    }
                }

                bool isCashReceipt = purchaseReturn.SettlementMode == PurchaseReturnSettlementMode.CashReceipt || 
                                     purchaseReturn.ReturnMethod == PaymentMethod.Cash ||
                                     originalPurchase.PaymentMethod != PaymentMethod.OnCredit;

                if (isCashReceipt)
                {
                    string? targetChannel = purchaseReturn.RefundChannel?.Trim();
                    if (string.IsNullOrWhiteSpace(targetChannel))
                    {
                        targetChannel = originalPurchase.DepositInfo?.BankOrWallet;
                    }
                    if (string.IsNullOrWhiteSpace(targetChannel) && !string.IsNullOrWhiteSpace(originalPurchase.Notes))
                    {
                        foreach (var w in POSSystem.WPF.Helpers.FinancialChannelHelper.UnifiedWallets)
                        {
                            if (originalPurchase.Notes.Contains(w, StringComparison.OrdinalIgnoreCase))
                            {
                                targetChannel = w;
                                break;
                            }
                        }
                    }

                    bool isCashDrawerRefund = string.IsNullOrWhiteSpace(targetChannel) ||
                                              targetChannel.Contains("الصندوق") ||
                                              targetChannel.Equals("نقدي", StringComparison.OrdinalIgnoreCase);

                    var vCode = $"RV-PRET-{purchaseReturn.ReturnNumber}-{now:HHmmssfff}-{Guid.NewGuid().ToString("N")[..4]}";
                    if (!isCashDrawerRefund)
                    {
                        string depositTag = POSSystem.WPF.Helpers.OrderDepositHelper.FormatDepositTag(
                            "ريال يمني",
                            purchaseReturn.TotalAmount,
                            targetChannel!,
                            purchaseReturn.ReturnNumber);

                        var receipt = new ReceiptVoucher
                        {
                            VoucherNumber = vCode,
                            Name = $"سند قبض مرتجع مشتريات - {purchaseReturn.ReturnNumber}",
                            Amount = purchaseReturn.TotalAmount,
                            Type = VoucherType.Receipt,
                            PaymentMethod = PaymentMethod.MobilePayment,
                            CustomerName = originalPurchase.SupplierName ?? "المورد",
                            Notes = $"{depositTag} سند قبض - استرداد مبلغ مرتجع مشتريات (#{purchaseReturn.ReturnNumber}) للفاتورة #{originalPurchase.PurchaseNumber} من المورد {originalPurchase.SupplierName ?? "المورد"} إلى ({targetChannel}). السبب: {purchaseReturn.Reason}",
                            UserId = purchaseReturn.UserId,
                            CreatedAt = now,
                            UpdatedAt = now,
                            VoucherDate = now
                        };
                        await _unitOfWork.ReceiptVouchers.AddAsync(receipt);
                    }
                    else
                    {
                        var receipt = new ReceiptVoucher
                        {
                            VoucherNumber = vCode,
                            Name = $"سند قبض مرتجع مشتريات - {purchaseReturn.ReturnNumber}",
                            Amount = purchaseReturn.TotalAmount,
                            Type = VoucherType.Receipt,
                            PaymentMethod = PaymentMethod.Cash,
                            CustomerName = originalPurchase.SupplierName ?? "المورد",
                            Notes = $"[الصندوق الرئيسي] سند قبض - استرداد نقدي لمرتجع مشتريات (#{purchaseReturn.ReturnNumber}) للفاتورة #{originalPurchase.PurchaseNumber} من المورد {originalPurchase.SupplierName ?? "المورد"}. السبب: {purchaseReturn.Reason}",
                            UserId = purchaseReturn.UserId,
                            CreatedAt = now,
                            UpdatedAt = now,
                            VoucherDate = now
                        };
                        await _unitOfWork.ReceiptVouchers.AddAsync(receipt);

                        var targetShift = await context.Shifts.OrderByDescending(sh => sh.IsActive).ThenByDescending(sh => sh.Id).FirstOrDefaultAsync();
                        if (targetShift != null)
                        {
                            await context.CashBoxTransactions.AddAsync(new CashBoxTransaction
                            {
                                ShiftId = targetShift.Id,
                                UserId = purchaseReturn.UserId,
                                Type = CashBoxTransactionType.Income,
                                Amount = purchaseReturn.TotalAmount,
                                Name = $"استرداد مرتجع مشتريات - {purchaseReturn.ReturnNumber}",
                                Description = $"استرداد نقدي لمرتجع مشتريات #{purchaseReturn.ReturnNumber} (فاتورة #{originalPurchase.PurchaseNumber}) من المورد {originalPurchase.SupplierName}",
                                Notes = purchaseReturn.Reason,
                                TransactionDate = now,
                                CreatedAt = now,
                                UpdatedAt = now,
                                IsActive = true
                            });
                        }
                    }
                }
                else
                {
                    Supplier? supplier = null;
                    if (originalPurchase.SupplierId.HasValue)
                    {
                        supplier = await _unitOfWork.Suppliers.GetByIdAsync(originalPurchase.SupplierId.Value);
                    }
                    else if (!string.IsNullOrWhiteSpace(originalPurchase.SupplierName))
                    {
                        supplier = await _unitOfWork.Suppliers.FirstOrDefaultAsync(s => s.Name == originalPurchase.SupplierName);
                    }

                    if (supplier != null)
                    {
                        supplier.TotalBalance -= purchaseReturn.TotalAmount;
                        supplier.UpdatedAt = now;
                        _unitOfWork.Suppliers.Update(supplier);
                    }
                }

                await _unitOfWork.PurchaseReturns.AddAsync(purchaseReturn);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();
                FinancialChannelService.InvalidateCache();
                POSSystem.WPF.Business.ServiceLocator.NotifyCashBoxChanged();
                return purchaseReturn;
            }
            catch
            {
                if (_unitOfWork.HasActiveTransaction)
                    await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
