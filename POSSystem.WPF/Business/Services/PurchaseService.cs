using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;
using static POSSystem.WPF.Models.Recipe;

namespace POSSystem.WPF.Business.Services
{
    public class PurchaseService : IPurchaseService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PurchaseService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<PurchaseItem>> GetAllPurchaseAsync1()
        {
            return await _unitOfWork.PurchaseItems.GetAllPurchaseAsync();
        }

        public async Task<IEnumerable<Purchase>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _unitOfWork.Purchases.GetByDateRangeAsync(startDate, endDate);
        }

        public async Task<IEnumerable<Purchase>> GetBySupplierAsync(string supplierName)
        {
            return await _unitOfWork.Purchases.GetBySupplierAsync(supplierName);
        }

        public async Task<Purchase?> GetPurchaseByIdAsync(int id)
        {
            return await _unitOfWork.Purchases.GetByIdAsync(id);
        }

        public async Task<Purchase?> GetPurchaseWithItemsAsync(int id)
        {
            return await _unitOfWork.Purchases.GetWithItemsAsync(id);
        }

        public async Task<Purchase> CreatePurchaseAsync(Purchase purchase)
        {
            if (string.IsNullOrWhiteSpace(purchase.SupplierName))
                throw new ArgumentException("اسم المورد مطلوب");

            if (purchase.PurchaseItems == null || !purchase.PurchaseItems.Any())
                throw new ArgumentException("يجب إضافة منتجات على الأقل");

            _unitOfWork.ClearEntityTracking();

            purchase.TotalAmount = purchase.PurchaseItems.Sum(pi => pi.TotalPrice);

            var now = DateTime.Now;
            purchase.CreatedAt = now;
            purchase.UpdatedAt = now;
            purchase.PurchaseDate = purchase.PurchaseDate == default ? now : purchase.PurchaseDate;

            if (string.IsNullOrWhiteSpace(purchase.PurchaseNumber) ||
                await _unitOfWork.GetContext().Purchases.AsNoTracking().AnyAsync(p => p.PurchaseNumber == purchase.PurchaseNumber))
            {
                purchase.PurchaseNumber = await GeneratePurchaseNumberAsync();
            }

            if (string.IsNullOrWhiteSpace(purchase.Name))
            {
                purchase.Name = !string.IsNullOrWhiteSpace(purchase.SupplierName) 
                    ? purchase.SupplierName 
                    : (!string.IsNullOrWhiteSpace(purchase.PurchaseNumber) ? purchase.PurchaseNumber : "فاتورة مشتريات");
            }

            foreach (var item in purchase.PurchaseItems)
            {
                item.Product = null;
                item.Addon = null;
                item.Purchase = null;
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _unitOfWork.Purchases.AddAsync(purchase);
            
                bool isCredit = purchase.PaymentMethod == PaymentMethod.OnCredit;
                if (isCredit && purchase.SupplierId.HasValue)
                {
                    var supplier = await _unitOfWork.Suppliers.GetByIdAsync(purchase.SupplierId.Value);
                    if (supplier != null)
                    {
                        supplier.TotalBalance += purchase.TotalAmount;
                        supplier.UpdatedAt = now;
                        _unitOfWork.Suppliers.Update(supplier);
                    }
                }
                else if (!isCredit)
                {
                    var voucherNumber = $"PV-{purchase.PurchaseNumber}";
                    if (voucherNumber.Length > 100)
                        voucherNumber = voucherNumber.Substring(0, 100);
                    int pvSuffix = 1;
                    while (await _unitOfWork.GetContext().ReceiptVouchers.AsNoTracking().AnyAsync(v => v.VoucherNumber == voucherNumber))
                    {
                        voucherNumber = $"PV-{purchase.PurchaseNumber}-{pvSuffix++}";
                    }

                    string voucherNotes = $"سند صرف مشتريات مقابل فاتورة رقم: {purchase.PurchaseNumber}";
                    if (!string.IsNullOrWhiteSpace(purchase.Notes))
                    {
                        voucherNotes += $" | {purchase.Notes}";
                    }
                    if (voucherNotes.Length > 500)
                    {
                        voucherNotes = voucherNotes.Substring(0, 500);
                    }

                    var voucher = new ReceiptVoucher
                    {
                        VoucherNumber = voucherNumber,
                        Name = $"سند صرف مشتريات - {purchase.PurchaseNumber}",
                        Type = VoucherType.Payment,
                        PaymentMethod = purchase.PaymentMethod,
                        Amount = purchase.TotalAmount,
                        CustomerName = !string.IsNullOrWhiteSpace(purchase.SupplierName) ? purchase.SupplierName : "مورد مشتريات",
                        VoucherDate = purchase.PurchaseDate,
                        Notes = voucherNotes,
                        UserId = purchase.UserId,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    await _unitOfWork.ReceiptVouchers.AddAsync(voucher);

                    string voucherTag = $"[سند صرف: {voucherNumber}]";
                    if (string.IsNullOrWhiteSpace(purchase.Notes))
                    {
                        purchase.Notes = voucherTag;
                    }
                    else if (!purchase.Notes.Contains("[سند صرف:"))
                    {
                        var updatedNotes = $"{purchase.Notes} {voucherTag}".Trim();
                        purchase.Notes = updatedNotes.Length > 500 ? updatedNotes.Substring(0, 500) : updatedNotes;
                    }
                }

                var context = _unitOfWork.GetContext();
                var productIds = purchase.PurchaseItems.Where(i => i.ProductId.HasValue).Select(i => i.ProductId!.Value).Distinct().ToList();
                var addonIds = purchase.PurchaseItems.Where(i => i.AddonId.HasValue).Select(i => i.AddonId!.Value).Distinct().ToList();

                var allWhProductsDict = productIds.Count > 0
                    ? await context.warehouseFromProducts.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id)
                    : new Dictionary<int, WarehouseFromProduct>();

                var allAddonsDict = addonIds.Count > 0
                    ? await context.Addons.Where(a => addonIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id)
                    : new Dictionary<int, Addon>();

                foreach (var item in purchase.PurchaseItems)
                {
                    if (item.ProductId.HasValue)
                    {
                        if (allWhProductsDict.TryGetValue(item.ProductId.Value, out var whProduct))
                        {
                            int targetWarehouseId = whProduct.WarehouseId ?? purchase.WarehouseId ?? 1;

                            if (TryDetectCarcass(whProduct.Name, item.Notes, whProduct.PortionUnit, out string animalTypeName, out int portionsPerCarcass))
                            {
                                decimal N = item.Quantity;
                                decimal totalPortions = N * portionsPerCarcass;
                                decimal totalLineCost = item.Quantity * item.UnitPrice;
                                decimal meatUnitCost = totalPortions > 0 ? (totalLineCost / totalPortions) : 0m;

                                whProduct.Price = item.UnitPrice;
                                whProduct.UpdatedAt = now;
                                _unitOfWork.warehouseFromProduct.Update(whProduct);

                                var components = new List<(string Name, decimal Quantity, decimal UnitCost, PortionUnit PortionUnit)>
                                {
                                    ($"لحم {animalTypeName} (نفر/جزء)", totalPortions, meatUnitCost, PortionUnit.Portion),
                                    ($"رأس {animalTypeName}", N * 1m, 0m, PortionUnit.Piece),
                                    ($"كبدة {animalTypeName} (حبة)", N * 1m, 0m, PortionUnit.Piece),
                                    ($"ظهر {animalTypeName} (حبة)", N * 1m, 0m, PortionUnit.Piece)
                                };

                                foreach (var comp in components)
                                {
                                    var targetProduct = await context.warehouseFromProducts
                                        .FirstOrDefaultAsync(w => w.WarehouseId == targetWarehouseId && w.Name == comp.Name);

                                    decimal prevQty = 0m;
                                    if (targetProduct == null)
                                    {
                                        targetProduct = new WarehouseFromProduct
                                        {
                                            WarehouseId = targetWarehouseId,
                                            Name = comp.Name,
                                            StockQuantity = comp.Quantity,
                                            Price = comp.UnitCost,
                                            PortionUnit = comp.PortionUnit,
                                            IsActive = true,
                                            CreatedAt = now,
                                            UpdatedAt = now
                                        };
                                        await context.warehouseFromProducts.AddAsync(targetProduct);
                                        await _unitOfWork.SaveChangesAsync();
                                    }
                                    else
                                    {
                                        prevQty = targetProduct.StockQuantity;
                                        var prevVal = prevQty * targetProduct.Price;
                                        var newVal = comp.Quantity * comp.UnitCost;

                                        targetProduct.StockQuantity += comp.Quantity;
                                        if (targetProduct.StockQuantity > 0)
                                        {
                                            targetProduct.Price = (prevVal + newVal) / targetProduct.StockQuantity;
                                        }
                                        targetProduct.PortionUnit = comp.PortionUnit;
                                        targetProduct.UpdatedAt = now;
                                        _unitOfWork.warehouseFromProduct.Update(targetProduct);
                                    }

                                    await _unitOfWork.StockMovements.AddAsync(new StockMovement
                                    {
                                        WarehouseFromProductId = targetProduct.Id,
                                        WarehouseId = targetWarehouseId,
                                        MovementType = StockMovementType.Purchase,
                                        Quantity = comp.Quantity,
                                        BalanceBefore = prevQty,
                                        BalanceAfter = targetProduct.StockQuantity,
                                        UnitCost = comp.UnitCost,
                                        TotalCost = comp.Quantity * comp.UnitCost,
                                        PurchaseId = purchase.Id > 0 ? purchase.Id : null,
                                        ReferenceNumber = purchase.PurchaseNumber,
                                        UserId = purchase.UserId,
                                        Notes = $"تفكيك آلي لشراء ذبيحة #{purchase.InvoiceNumber ?? purchase.PurchaseNumber} ({whProduct.Name})",
                                        MovementDate = now,
                                        CreatedAt = now
                                    });
                                }

                                AnimalType animalTypeEnum = animalTypeName switch
                                {
                                    "بقرة" => AnimalType.Cow,
                                    "جمل" => AnimalType.Camel,
                                    _ => AnimalType.Sheep
                                };

                                var animalBatch = new AnimalBatch
                                {
                                    Name = $"{animalTypeName} - شراء #{purchase.InvoiceNumber ?? purchase.PurchaseNumber}",
                                    Type = animalTypeEnum,
                                    BatchDate = now,
                                    WarehouseId = targetWarehouseId,
                                    SupplierId = purchase.SupplierId,
                                    HeadCount = (int)Math.Max(1, N),
                                    PurchaseCost = totalLineCost,
                                    IsWeightMode = false,
                                    RemainingWeight = totalPortions,
                                    TotalWeight = totalPortions,
                                    CreatedAt = now,
                                    UpdatedAt = now
                                };
                                await context.AnimalBatches.AddAsync(animalBatch);
                            }
                            else if (TryDetectBagWeight(whProduct.Name, item.Notes, out decimal bagWeightKg))
                            {
                                // تحويل الأكياس إلى كيلو وتخزينها بالكيلو في المخزون (مثال: 5 أكياس × 40 كجم = 200 كجم)
                                decimal totalKg = item.Quantity * bagWeightKg;
                                decimal totalLineCost = item.Quantity * item.UnitPrice;
                                decimal unitCostPerKg = totalKg > 0 ? (totalLineCost / totalKg) : item.UnitPrice;

                                decimal previousQty = (whProduct.PortionUnit.HasValue && whProduct.PortionUnit.Value != PortionUnit.Kg && whProduct.StockQuantity > 0)
                                    ? (whProduct.StockQuantity * bagWeightKg)
                                    : whProduct.StockQuantity;
                                decimal previousUnitPrice = (whProduct.PortionUnit.HasValue && whProduct.PortionUnit.Value != PortionUnit.Kg && bagWeightKg > 0 && whProduct.Price > 0)
                                    ? (whProduct.Price / bagWeightKg)
                                    : whProduct.Price;

                                decimal previousValue = previousQty * previousUnitPrice;

                                whProduct.StockQuantity = previousQty + totalKg;
                                whProduct.PortionUnit = PortionUnit.Kg;
                                if (whProduct.StockQuantity > 0)
                                {
                                    whProduct.Price = (previousValue + totalLineCost) / whProduct.StockQuantity;
                                }
                                whProduct.Description = $"[شراء: كيس={bagWeightKg:0.##}]";
                                whProduct.UpdatedAt = now;
                                _unitOfWork.warehouseFromProduct.Update(whProduct);

                                await _unitOfWork.StockMovements.AddAsync(new StockMovement
                                {
                                    WarehouseFromProductId = whProduct.Id,
                                    WarehouseId = whProduct.WarehouseId,
                                    MovementType = StockMovementType.Purchase,
                                    Quantity = totalKg,
                                    BalanceBefore = previousQty,
                                    BalanceAfter = whProduct.StockQuantity,
                                    UnitCost = unitCostPerKg,
                                    TotalCost = totalLineCost,
                                    PurchaseId = purchase.Id > 0 ? purchase.Id : null,
                                    ReferenceNumber = purchase.PurchaseNumber,
                                    UserId = purchase.UserId,
                                    Notes = $"شراء {item.Quantity} كيس × {bagWeightKg:0.##} كجم = {totalKg:0.##} كجم بموجب فاتورة #{purchase.InvoiceNumber ?? purchase.PurchaseNumber}",
                                    MovementDate = now,
                                    CreatedAt = now
                                });
                            }
                            else
                            {
                                var previousQty = whProduct.StockQuantity;
                                var previousValue = previousQty * whProduct.Price;
                                var newValue = item.Quantity * item.UnitPrice;

                                whProduct.StockQuantity += item.Quantity;
                                
                                if (whProduct.StockQuantity > 0)
                                {
                                    whProduct.Price = (previousValue + newValue) / whProduct.StockQuantity;
                                }

                                whProduct.UpdatedAt = now;
                                _unitOfWork.warehouseFromProduct.Update(whProduct);

                                await _unitOfWork.StockMovements.AddAsync(new StockMovement
                                {
                                    WarehouseFromProductId = whProduct.Id,
                                    WarehouseId = whProduct.WarehouseId,
                                    MovementType = StockMovementType.Purchase,
                                    Quantity = item.Quantity,
                                    BalanceBefore = previousQty,
                                    BalanceAfter = whProduct.StockQuantity,
                                    UnitCost = item.UnitPrice,
                                    TotalCost = item.Quantity * item.UnitPrice,
                                    PurchaseId = purchase.Id > 0 ? purchase.Id : null,
                                    ReferenceNumber = purchase.PurchaseNumber,
                                    UserId = purchase.UserId,
                                    Notes = $"شراء مخزون بموجب فاتورة شراء #{purchase.InvoiceNumber ?? purchase.PurchaseNumber}",
                                    MovementDate = now,
                                    CreatedAt = now
                                });
                            }
                        }
                    }
                    else if (item.AddonId.HasValue)
                    {
                        if (allAddonsDict.TryGetValue(item.AddonId.Value, out var addon))
                        {
                            var previousQty = addon.Quantity;
                            var previousValue = previousQty * addon.BuyingPrice;
                            var newValue = item.Quantity * item.UnitPrice;

                            addon.Quantity += item.Quantity;

                            if (addon.Quantity > 0)
                            {
                                addon.BuyingPrice = (previousValue + newValue) / addon.Quantity;
                            }

                            addon.UpdatedAt = now;
                            _unitOfWork.Addons.Update(addon);
                        }
                    }
                }

                await _unitOfWork.SaveChangesAsync();
            });
            return purchase;
        }

        public async Task<PurchaseItem> CreatePurchaseitemAsync(PurchaseItem purchaseitem)
        {
            await _unitOfWork.PurchaseItems.AddAsync(purchaseitem);
            await _unitOfWork.SaveChangesAsync();
            return purchaseitem;
        }

        public async Task<Purchase> UpdatePurchaseAsync(Purchase purchase)
        {
            if (string.IsNullOrWhiteSpace(purchase.SupplierName))
                throw new ArgumentException("اسم المورد مطلوب");

            var context = _unitOfWork.GetContext();
            var existingPurchase = await context.Purchases
                .Include(p => p.PurchaseItems)
                .FirstOrDefaultAsync(p => p.Id == purchase.Id);

            if (existingPurchase == null)
                throw new ArgumentException($"فاتورة المشتريات برقم {purchase.Id} غير موجودة");

            var now = DateTime.Now;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var oldItems = existingPurchase.PurchaseItems.ToList();
                var newItems = (purchase.PurchaseItems ?? Enumerable.Empty<PurchaseItem>()).ToList();

                var handledOldItemIds = new HashSet<int>();
                var adjustments = new List<(int? ProductId, int? AddonId, decimal QtyDiff, decimal EffectiveKgDiff, bool IsBagConverted, decimal BagWeightKg, decimal UnitPrice, string? Notes)>();

                foreach (var newItem in newItems)
                {
                    PurchaseItem? matchedOld = null;
                    if (newItem.Id > 0)
                    {
                        matchedOld = oldItems.FirstOrDefault(o => o.Id == newItem.Id);
                    }
                    else
                    {
                        matchedOld = oldItems.FirstOrDefault(o => !handledOldItemIds.Contains(o.Id) &&
                            ((newItem.ProductId.HasValue && o.ProductId == newItem.ProductId) ||
                             (newItem.AddonId.HasValue && o.AddonId == newItem.AddonId)));
                    }

                    bool newIsBag = TryDetectBagWeight(null, newItem.Notes, out decimal newBagW);
                    decimal newEffQty = newIsBag ? (newItem.Quantity * newBagW) : newItem.Quantity;

                    if (matchedOld != null)
                    {
                        handledOldItemIds.Add(matchedOld.Id);
                        bool oldIsBag = TryDetectBagWeight(null, matchedOld.Notes, out decimal oldBagW);
                        decimal oldEffQty = oldIsBag ? (matchedOld.Quantity * oldBagW) : matchedOld.Quantity;

                        decimal rawDiff = newItem.Quantity - matchedOld.Quantity;
                        decimal effDiff = newEffQty - oldEffQty;
                        if (rawDiff != 0 || effDiff != 0)
                        {
                            adjustments.Add((newItem.ProductId, newItem.AddonId, rawDiff, effDiff, newIsBag || oldIsBag, newIsBag ? newBagW : oldBagW, newItem.UnitPrice, newItem.Notes));
                        }
                    }
                    else
                    {
                        adjustments.Add((newItem.ProductId, newItem.AddonId, newItem.Quantity, newEffQty, newIsBag, newBagW, newItem.UnitPrice, newItem.Notes));
                    }
                }

                foreach (var oldItem in oldItems.Where(o => !handledOldItemIds.Contains(o.Id)))
                {
                    bool oldIsBag = TryDetectBagWeight(null, oldItem.Notes, out decimal oldBagW);
                    decimal oldEffQty = oldIsBag ? (oldItem.Quantity * oldBagW) : oldItem.Quantity;
                    adjustments.Add((oldItem.ProductId, oldItem.AddonId, -oldItem.Quantity, -oldEffQty, oldIsBag, oldBagW, oldItem.UnitPrice, oldItem.Notes));
                }

                var productIdsToFetch = adjustments.Where(a => a.ProductId.HasValue).Select(a => a.ProductId!.Value).Distinct().ToList();
                var addonIdsToFetch = adjustments.Where(a => a.AddonId.HasValue).Select(a => a.AddonId!.Value).Distinct().ToList();

                var productsDict = productIdsToFetch.Count > 0
                    ? await context.warehouseFromProducts.Where(p => productIdsToFetch.Contains(p.Id)).ToDictionaryAsync(p => p.Id)
                    : new Dictionary<int, WarehouseFromProduct>();

                var addonsDict = addonIdsToFetch.Count > 0
                    ? await context.Addons.Where(a => addonIdsToFetch.Contains(a.Id)).ToDictionaryAsync(a => a.Id)
                    : new Dictionary<int, Addon>();

                foreach (var adj in adjustments.Where(a => a.EffectiveKgDiff < 0 || a.QtyDiff < 0))
                {
                    if (adj.ProductId.HasValue && productsDict.TryGetValue(adj.ProductId.Value, out var prod))
                    {
                        if (TryDetectCarcass(prod.Name, adj.Notes, prod.PortionUnit, out string animalTypeName, out int portionsPerCarcass))
                        {
                            if (adj.QtyDiff < 0)
                            {
                                decimal carcassReduction = Math.Abs(adj.QtyDiff);
                                decimal portionsReduction = carcassReduction * portionsPerCarcass;
                                int targetWhId = prod.WarehouseId ?? existingPurchase.WarehouseId ?? 1;
                                string meatName = $"لحم {animalTypeName} (نفر/جزء)";
                                string legacyMeatName = $"لحم {animalTypeName} (ثمن/جزء)";
                                var meatTarget = await context.warehouseFromProducts
                                    .FirstOrDefaultAsync(w => w.WarehouseId == targetWhId && (w.Name == meatName || w.Name == legacyMeatName));

                                if (meatTarget != null && meatTarget.StockQuantity < portionsReduction)
                                {
                                    throw new InvalidOperationException(
                                        $"لا يمكن تقليل كمية شراء ذبيحة '{prod.Name}' بمقدار {carcassReduction}، لأن رصيد أجزاء اللحم المتبقي بالمستودع ({meatTarget.StockQuantity:N0} جزء) أقل من المطلوب استرجاعه ({portionsReduction:N0} جزء). تم استهلاك اللحم في المبيعات!");
                                }
                            }
                        }
                        else if (adj.EffectiveKgDiff < 0)
                        {
                            decimal neededReduction = Math.Abs(adj.EffectiveKgDiff);
                            if (prod.StockQuantity < neededReduction)
                            {
                                var unitName = prod.PortionUnit.HasValue ? PortionUnitHelper.GetDisplayName(prod.PortionUnit.Value) : string.Empty;
                                throw new InvalidOperationException(
                                    $"لا يمكن تقليل كمية الشراء للمادة '{prod.Name}' بمقدار {neededReduction:N0} {unitName}، لأن الرصيد المتبقي في المستودع ({prod.StockQuantity:N0} {unitName}) غير كافٍ.");
                            }
                        }
                    }
                    else if (adj.AddonId.HasValue && addonsDict.TryGetValue(adj.AddonId.Value, out var addon) && adj.QtyDiff < 0)
                    {
                        decimal neededReduction = Math.Abs(adj.QtyDiff);
                        if (addon.Quantity < neededReduction)
                        {
                            throw new InvalidOperationException(
                                $"لا يمكن تقليل كمية الإضافة '{addon.Name}' بمقدار {neededReduction:N0}، لأن الرصيد المتبقي ({addon.Quantity:N0}) غير كافٍ.");
                        }
                    }
                }

                foreach (var adj in adjustments)
                {
                    if (adj.ProductId.HasValue && productsDict.TryGetValue(adj.ProductId.Value, out var prod))
                    {
                        if (TryDetectCarcass(prod.Name, adj.Notes, prod.PortionUnit, out string animalTypeName, out int portionsPerCarcass))
                        {
                            decimal N = adj.QtyDiff;
                            decimal totalPortions = N * portionsPerCarcass;
                            int targetWhId = prod.WarehouseId ?? existingPurchase.WarehouseId ?? 1;

                            var compNames = new List<(string Name, decimal Quantity, decimal UnitCost)>
                            {
                                ($"لحم {animalTypeName} (نفر/جزء)", totalPortions, adj.UnitPrice > 0 && portionsPerCarcass > 0 ? adj.UnitPrice / portionsPerCarcass : 0m),
                                ($"رأس {animalTypeName}", N * 1m, 0m),
                                ($"كبدة {animalTypeName} (حبة)", N * 1m, 0m),
                                ($"ظهر {animalTypeName} (حبة)", N * 1m, 0m)
                            };

                            foreach (var (compName, compQty, compUnitCost) in compNames)
                            {
                                string altName = compName.Replace("(نفر/جزء)", "(ثمن/جزء)");
                                var target = await context.warehouseFromProducts
                                    .FirstOrDefaultAsync(w => w.WarehouseId == targetWhId && (w.Name == compName || w.Name == altName));
                                if (target != null)
                                {
                                    decimal beforeQty = target.StockQuantity;
                                    target.StockQuantity += compQty;
                                    decimal afterQty = target.StockQuantity;
                                    target.UpdatedAt = now;
                                    _unitOfWork.warehouseFromProduct.Update(target);

                                    await _unitOfWork.StockMovements.AddAsync(new StockMovement
                                    {
                                        WarehouseFromProductId = target.Id,
                                        WarehouseId = target.WarehouseId,
                                        MovementType = compQty > 0 ? StockMovementType.Purchase : StockMovementType.Adjustment,
                                        Quantity = compQty,
                                        BalanceBefore = beforeQty,
                                        BalanceAfter = afterQty,
                                        UnitCost = compUnitCost,
                                        TotalCost = Math.Abs(compQty) * compUnitCost,
                                        PurchaseId = existingPurchase.Id,
                                        ReferenceNumber = existingPurchase.PurchaseNumber,
                                        UserId = purchase.UserId ?? existingPurchase.UserId,
                                        Notes = $"تعديل فاتورة شراء ذبيحة #{existingPurchase.InvoiceNumber ?? existingPurchase.PurchaseNumber} ({prod.Name})",
                                        MovementDate = now,
                                        CreatedAt = now
                                    });
                                }
                            }
                        }
                        else
                        {
                            decimal stockDelta = adj.IsBagConverted ? adj.EffectiveKgDiff : adj.QtyDiff;
                            decimal beforeQty = prod.StockQuantity;
                            prod.StockQuantity += stockDelta;
                            decimal afterQty = prod.StockQuantity;

                            if (adj.IsBagConverted)
                            {
                                prod.PortionUnit = PortionUnit.Kg;
                                if (adj.BagWeightKg > 0)
                                {
                                    prod.Description = $"[شراء: كيس={adj.BagWeightKg:0.##}]";
                                }
                            }

                            decimal effectiveUnitCost = (adj.IsBagConverted && adj.BagWeightKg > 0)
                                ? (adj.UnitPrice / adj.BagWeightKg)
                                : adj.UnitPrice;

                            if (stockDelta > 0)
                            {
                                var previousValue = beforeQty * prod.Price;
                                var newValue = stockDelta * effectiveUnitCost;
                                if (prod.StockQuantity > 0)
                                {
                                    prod.Price = (previousValue + newValue) / prod.StockQuantity;
                                }
                            }

                            prod.UpdatedAt = now;
                            _unitOfWork.warehouseFromProduct.Update(prod);

                            await _unitOfWork.StockMovements.AddAsync(new StockMovement
                            {
                                WarehouseFromProductId = prod.Id,
                                WarehouseId = prod.WarehouseId,
                                MovementType = stockDelta > 0 ? StockMovementType.Purchase : StockMovementType.Adjustment,
                                Quantity = stockDelta,
                                BalanceBefore = beforeQty,
                                BalanceAfter = afterQty,
                                UnitCost = effectiveUnitCost,
                                TotalCost = Math.Abs(stockDelta) * effectiveUnitCost,
                                PurchaseId = existingPurchase.Id,
                                ReferenceNumber = existingPurchase.PurchaseNumber,
                                UserId = purchase.UserId ?? existingPurchase.UserId,
                                Notes = $"تعديل فاتورة شراء #{existingPurchase.InvoiceNumber ?? existingPurchase.PurchaseNumber} (فرق كمية المخزون: {(stockDelta > 0 ? "+" : "")}{stockDelta:0.##})",
                                MovementDate = now,
                                CreatedAt = now
                            });
                        }
                    }
                    else if (adj.AddonId.HasValue && addonsDict.TryGetValue(adj.AddonId.Value, out var addon))
                    {
                        addon.Quantity += adj.QtyDiff;
                        addon.UpdatedAt = now;
                        _unitOfWork.Addons.Update(addon);
                    }
                }

                decimal oldTotal = existingPurchase.TotalAmount;
                decimal newTotal = purchase.PurchaseItems?.Sum(pi => pi.TotalPrice) ?? 0;

                var oldMethod = existingPurchase.PaymentMethod;
                var newMethod = purchase.PaymentMethod;

                int? oldSupplierId = existingPurchase.SupplierId;
                int? newSupplierId = purchase.SupplierId;

                if (oldMethod == PaymentMethod.OnCredit && oldSupplierId.HasValue)
                {
                    var oldSup = await _unitOfWork.Suppliers.GetByIdAsync(oldSupplierId.Value);
                    if (oldSup != null)
                    {
                        oldSup.TotalBalance -= oldTotal;
                        oldSup.UpdatedAt = now;
                        _unitOfWork.Suppliers.Update(oldSup);
                    }
                }

                if (newMethod == PaymentMethod.OnCredit && newSupplierId.HasValue)
                {
                    var newSup = await _unitOfWork.Suppliers.GetByIdAsync(newSupplierId.Value);
                    if (newSup != null)
                    {
                        newSup.TotalBalance += newTotal;
                        newSup.UpdatedAt = now;
                        _unitOfWork.Suppliers.Update(newSup);
                    }
                }

                if (!string.IsNullOrWhiteSpace(existingPurchase.PurchaseNumber))
                {
                    var vouchers = await context.ReceiptVouchers
                        .Where(v => (v.VoucherNumber != null && v.VoucherNumber.Contains(existingPurchase.PurchaseNumber)) ||
                                    (v.Notes != null && v.Notes.Contains(existingPurchase.PurchaseNumber)))
                        .ToListAsync();

                    if (newMethod == PaymentMethod.OnCredit)
                    {
                        if (vouchers.Any())
                        {
                            context.ReceiptVouchers.RemoveRange(vouchers);
                        }
                    }
                    else
                    {
                        foreach (var v in vouchers)
                        {
                            v.Amount = newTotal;
                            v.CustomerName = purchase.SupplierName;
                            v.VoucherDate = purchase.PurchaseDate;
                            v.UpdatedAt = now;
                            _unitOfWork.ReceiptVouchers.Update(v);
                        }
                    }
                }

                existingPurchase.SupplierId = purchase.SupplierId;
                existingPurchase.SupplierName = purchase.SupplierName;
                existingPurchase.WarehouseId = purchase.WarehouseId;
                existingPurchase.InvoiceNumber = purchase.InvoiceNumber;
                existingPurchase.PurchaseDate = purchase.PurchaseDate == default ? now : purchase.PurchaseDate;
                existingPurchase.PaymentMethod = purchase.PaymentMethod;
                existingPurchase.TotalAmount = newTotal;
                existingPurchase.Notes = purchase.Notes;
                existingPurchase.UpdatedAt = now;
                if (!string.IsNullOrWhiteSpace(purchase.Name))
                    existingPurchase.Name = purchase.Name;

                context.PurchaseItems.RemoveRange(existingPurchase.PurchaseItems);
                existingPurchase.PurchaseItems.Clear();

                if (purchase.PurchaseItems != null)
                {
                    foreach (var item in purchase.PurchaseItems)
                    {
                        existingPurchase.PurchaseItems.Add(new PurchaseItem
                        {
                            PurchaseId = existingPurchase.Id,
                            ProductId = item.ProductId,
                            AddonId = item.AddonId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            TotalPrice = item.TotalPrice,
                            Notes = item.Notes
                        });
                    }
                }

                _unitOfWork.Purchases.Update(existingPurchase);
                await _unitOfWork.SaveChangesAsync();
            });

            return existingPurchase;
        }

        public async Task DeletePurchaseAsync(int id)
        {
            _unitOfWork.ClearEntityTracking();

            var purchase = await GetPurchaseWithItemsAsync(id);
            if (purchase == null)
                throw new ArgumentException("المشتري غير موجود");

            var now = DateTime.Now;
            var context = _unitOfWork.GetContext();

            var productIds = purchase.PurchaseItems.Where(i => i.ProductId.HasValue).Select(i => i.ProductId!.Value).Distinct().ToList();
            var addonIds = purchase.PurchaseItems.Where(i => i.AddonId.HasValue).Select(i => i.AddonId!.Value).Distinct().ToList();

            var allWhProductsDict = productIds.Count > 0
                ? await context.warehouseFromProducts.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id)
                : new Dictionary<int, WarehouseFromProduct>();

            var allAddonsDict = addonIds.Count > 0
                ? await context.Addons.Where(a => addonIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id)
                : new Dictionary<int, Addon>();

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                foreach (var item in purchase.PurchaseItems)
                {
                    if (item.ProductId.HasValue && allWhProductsDict.TryGetValue(item.ProductId.Value, out var product))
                    {
                        if (TryDetectCarcass(product.Name, item.Notes, product.PortionUnit, out string animalTypeName, out int portionsPerCarcass))
                        {
                            decimal totalPortions = item.Quantity * portionsPerCarcass;
                            int targetWhId = product.WarehouseId ?? purchase.WarehouseId ?? 1;
                            string meatName = $"لحم {animalTypeName} (نفر/جزء)";
                            string legacyMeatName = $"لحم {animalTypeName} (ثمن/جزء)";
                            var targetMeat = await context.warehouseFromProducts
                                .FirstOrDefaultAsync(w => w.WarehouseId == targetWhId && (w.Name == meatName || w.Name == legacyMeatName));

                            if (targetMeat != null && targetMeat.StockQuantity < totalPortions)
                            {
                                throw new InvalidOperationException(
                                    $"لا يمكن حذف فاتورة الشراء للذبيحة '{product.Name}' لأن أجزاء اللحم تم بيعها أو استهلاكها بالفعل! " +
                                    $"الرصيد المتبقي في المستودع: {targetMeat.StockQuantity:N0} جزء، والمطلوب للتراجع: {totalPortions:N0} جزء.");
                            }
                        }
                        else if (TryDetectBagWeight(product.Name, item.Notes, out decimal bagWeightKg))
                        {
                            decimal requiredKg = item.Quantity * bagWeightKg;
                            if (product.StockQuantity < requiredKg)
                            {
                                throw new InvalidOperationException(
                                    $"لا يمكن حذف فاتورة الشراء لأن كميات المادة '{product.Name}' تم استهلاكها بالفعل! " +
                                    $"الكمية المطلوب التراجع عنها: {requiredKg:N0} كجم ({item.Quantity} كيس)، والرصيد المتوفر حالياً بالمستودع: {product.StockQuantity:N0} كجم.");
                            }
                        }
                        else
                        {
                            if (product.StockQuantity < item.Quantity)
                            {
                                var unitName = product.PortionUnit.HasValue ? PortionUnitHelper.GetDisplayName(product.PortionUnit.Value) : string.Empty;
                                throw new InvalidOperationException(
                                    $"لا يمكن حذف فاتورة الشراء لأن كميات المادة '{product.Name}' تم استهلاكها أو بيعها بالفعل! " +
                                    $"الكمية المطلوب التراجع عنها: {item.Quantity:N0} {unitName}، والرصيد المتوفر حالياً بالمستودع: {product.StockQuantity:N0} {unitName}.");
                            }
                        }
                    }
                    else if (item.AddonId.HasValue && allAddonsDict.TryGetValue(item.AddonId.Value, out var addon))
                    {
                        if (addon.Quantity < item.Quantity)
                        {
                            throw new InvalidOperationException(
                                $"لا يمكن حذف فاتورة الشراء لأن الإضافة '{addon.Name}' تم بيعها أو استهلاكها! " +
                                $"الكمية المطلوب التراجع عنها: {item.Quantity:N0}، والرصيد المتوفر: {addon.Quantity:N0}.");
                        }
                    }
                }

                foreach (var item in purchase.PurchaseItems)
                {
                    if (item.ProductId.HasValue && allWhProductsDict.TryGetValue(item.ProductId.Value, out var product))
                    {
                        if (TryDetectCarcass(product.Name, item.Notes, product.PortionUnit, out string animalTypeName, out int portionsPerCarcass))
                        {
                            decimal N = item.Quantity;
                            decimal totalPortions = N * portionsPerCarcass;
                            int targetWhId = product.WarehouseId ?? purchase.WarehouseId ?? 1;

                            var compNames = new List<(string Name, decimal Quantity, decimal UnitCost)>
                            {
                                ($"لحم {animalTypeName} (نفر/جزء)", totalPortions, item.UnitPrice > 0 && portionsPerCarcass > 0 ? item.UnitPrice / portionsPerCarcass : 0m),
                                ($"رأس {animalTypeName}", N * 1m, 0m),
                                ($"كبدة {animalTypeName} (حبة)", N * 1m, 0m),
                                ($"ظهر {animalTypeName} (حبة)", N * 1m, 0m)
                            };

                            foreach (var (compName, compQty, compUnitCost) in compNames)
                            {
                                string altName = compName.Replace("(نفر/جزء)", "(ثمن/جزء)");
                                var target = await context.warehouseFromProducts
                                    .FirstOrDefaultAsync(w => w.WarehouseId == targetWhId && (w.Name == compName || w.Name == altName));
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
                                        WarehouseId = target.WarehouseId,
                                        MovementType = StockMovementType.Adjustment,
                                        Quantity = -compQty,
                                        BalanceBefore = beforeQty,
                                        BalanceAfter = afterQty,
                                        UnitCost = compUnitCost,
                                        TotalCost = Math.Abs(compQty) * compUnitCost,
                                        PurchaseId = purchase.Id,
                                        ReferenceNumber = purchase.PurchaseNumber,
                                        UserId = purchase.UserId,
                                        Notes = $"حذف/إلغاء فاتورة شراء ذبيحة #{purchase.InvoiceNumber ?? purchase.PurchaseNumber} (عكس استلام أجزاء {animalTypeName})",
                                        MovementDate = now,
                                        CreatedAt = now
                                    });
                                }
                            }
                        }
                        else if (TryDetectBagWeight(product.Name, item.Notes, out decimal bagWeightKg))
                        {
                            decimal reverseKg = item.Quantity * bagWeightKg;
                            decimal beforeQty = product.StockQuantity;
                            product.StockQuantity -= reverseKg;
                            decimal afterQty = product.StockQuantity;
                            product.UpdatedAt = now;
                            _unitOfWork.warehouseFromProduct.Update(product);

                            await _unitOfWork.StockMovements.AddAsync(new StockMovement
                            {
                                WarehouseFromProductId = product.Id,
                                WarehouseId = product.WarehouseId,
                                MovementType = StockMovementType.Adjustment,
                                Quantity = -reverseKg,
                                BalanceBefore = beforeQty,
                                BalanceAfter = afterQty,
                                UnitCost = bagWeightKg > 0 ? (item.UnitPrice / bagWeightKg) : item.UnitPrice,
                                TotalCost = item.Quantity * item.UnitPrice,
                                PurchaseId = purchase.Id,
                                ReferenceNumber = purchase.PurchaseNumber,
                                UserId = purchase.UserId,
                                Notes = $"حذف/إلغاء فاتورة مشتريات #{purchase.InvoiceNumber ?? purchase.PurchaseNumber} (عكس {reverseKg:0.##} كجم)",
                                MovementDate = now,
                                CreatedAt = now
                            });
                        }
                        else
                        {
                            decimal beforeQty = product.StockQuantity;
                            product.StockQuantity -= item.Quantity;
                            decimal afterQty = product.StockQuantity;
                            product.UpdatedAt = now;
                            _unitOfWork.warehouseFromProduct.Update(product);

                            await _unitOfWork.StockMovements.AddAsync(new StockMovement
                            {
                                WarehouseFromProductId = product.Id,
                                WarehouseId = product.WarehouseId,
                                MovementType = StockMovementType.Adjustment,
                                Quantity = -item.Quantity,
                                BalanceBefore = beforeQty,
                                BalanceAfter = afterQty,
                                UnitCost = item.UnitPrice,
                                TotalCost = item.Quantity * item.UnitPrice,
                                PurchaseId = purchase.Id,
                                ReferenceNumber = purchase.PurchaseNumber,
                                UserId = purchase.UserId,
                                Notes = $"حذف/إلغاء فاتورة مشتريات #{purchase.InvoiceNumber ?? purchase.PurchaseNumber} (عكس استلام المخزون)",
                                MovementDate = now,
                                CreatedAt = now
                            });
                        }
                    }
                    else if (item.AddonId.HasValue && allAddonsDict.TryGetValue(item.AddonId.Value, out var addon))
                    {
                        addon.Quantity -= item.Quantity;
                        addon.UpdatedAt = now;
                        _unitOfWork.Addons.Update(addon);
                    }
                }

                if (purchase.PaymentMethod == PaymentMethod.OnCredit && purchase.SupplierId.HasValue)
                {
                    var supplier = await _unitOfWork.Suppliers.GetByIdAsync(purchase.SupplierId.Value);
                    if (supplier != null)
                    {
                        supplier.TotalBalance -= purchase.TotalAmount;
                        supplier.UpdatedAt = now;
                        _unitOfWork.Suppliers.Update(supplier);
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(purchase.PurchaseNumber))
                    {
                        var vouchers = await context.ReceiptVouchers
                            .Where(v => (v.VoucherNumber != null && v.VoucherNumber.Contains(purchase.PurchaseNumber)) ||
                                        (v.Notes != null && v.Notes.Contains(purchase.PurchaseNumber)))
                            .ToListAsync();

                        if (vouchers.Any())
                        {
                            context.ReceiptVouchers.RemoveRange(vouchers);
                        }
                    }
                }

                _unitOfWork.Purchases.Remove(purchase);
                await _unitOfWork.SaveChangesAsync();
            });
        }

        public async Task<decimal> GetTotalByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _unitOfWork.Purchases.GetTotalByDateRangeAsync(startDate, endDate);
        }

        public async Task<string> GeneratePurchaseNumberAsync()
        {
            var today = DateTime.Now.Date;
            var prefix = $"PUR-{today:yyyyMMdd}-";
            var context = _unitOfWork.GetContext();

            var existingNumbers = await context.Purchases
                .AsNoTracking()
                .Where(p => p.PurchaseNumber != null && p.PurchaseNumber.StartsWith(prefix))
                .Select(p => p.PurchaseNumber!)
                .ToListAsync();

            var existingSet = new HashSet<string>(existingNumbers, StringComparer.OrdinalIgnoreCase);
            int maxSequence = 0;
            foreach (var num in existingNumbers)
            {
                if (num.Length > prefix.Length)
                {
                    var suffix = num.Substring(prefix.Length);
                    var dashIdx = suffix.IndexOf('-');
                    if (dashIdx > 0)
                        suffix = suffix.Substring(0, dashIdx);

                    if (int.TryParse(suffix, out int seq) && seq > maxSequence)
                    {
                        maxSequence = seq;
                    }
                }
            }

            int nextSeq = maxSequence + 1;
            string candidate = $"{prefix}{nextSeq:D4}";
            while (existingSet.Contains(candidate) ||
                   await context.Purchases.AsNoTracking().AnyAsync(p => p.PurchaseNumber == candidate))
            {
                nextSeq++;
                candidate = $"{prefix}{nextSeq:D4}";
            }

            return candidate;
        }

        public static bool TryDetectBagWeight(string? productName, string? itemNotes, out decimal bagWeightKg)
        {
            bagWeightKg = 0m;
            if (string.IsNullOrWhiteSpace(itemNotes))
                return false;

            var match = Regex.Match(
                itemNotes,
                @"(?:حجم\s*الكيس|حجم\s*العبوة|وزن\s*الكيس)\s*[:=\-]?\s*(\d+(?:[\.,]\d+)?)");

            if (match.Success)
            {
                string raw = match.Groups[1].Value.Replace(',', '.');
                if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed) && parsed > 0)
                {
                    bagWeightKg = parsed;
                    return true;
                }
            }

            return false;
        }

        public static bool TryDetectCarcass(string? productName, string? itemNotes, PortionUnit? portionUnit, out string animalTypeName, out int portionsPerCarcass)
        {
            animalTypeName = string.Empty;
            portionsPerCarcass = 0;

            if (string.IsNullOrWhiteSpace(productName))
                return false;

            string prod = productName.Trim().ToLowerInvariant();
            string notes = (itemNotes ?? "").Trim().ToLowerInvariant();
            string combined = $"{prod} {notes}";

            bool isPart = prod.Contains("لحم") || 
                          prod.Contains("رأس") || 
                          prod.Contains("راس") || 
                          prod.Contains("كبد") || 
                          prod.Contains("ظهر") ||
                          prod.Contains("ثمن") ||
                          prod.Contains("نفر");

            if (isPart)
                return false;

            int defaultPortions = 0;

            if (combined.Contains("خروف") || combined.Contains("غنم") || combined.Contains("ضأن") || 
                combined.Contains("sheep") || combined.Contains("lamb"))
            {
                animalTypeName = "خروف";
                defaultPortions = 16;
            }
            else if (combined.Contains("تيس"))
            {
                animalTypeName = "تيس";
                defaultPortions = 16;
            }
            else if (combined.Contains("عجل"))
            {
                animalTypeName = "عجل";
                defaultPortions = 50;
            }
            else if (combined.Contains("بقر") || combined.Contains("ثور") || combined.Contains("cow") || combined.Contains("beef"))
            {
                animalTypeName = "بقر";
                defaultPortions = 50;
            }
            else if (combined.Contains("حاشي"))
            {
                animalTypeName = "حاشي";
                defaultPortions = 60;
            }
            else if (combined.Contains("جمل") || combined.Contains("قعود") || combined.Contains("camel"))
            {
                animalTypeName = "جمل";
                defaultPortions = 60;
            }
            else if (portionUnit == PortionUnit.Carcass || combined.Contains("ذبيحة") || combined.Contains("ذبيحه"))
            {
                animalTypeName = "خروف";
                defaultPortions = 16;
            }
            else
            {
                return false;
            }

            portionsPerCarcass = defaultPortions;

            if (!string.IsNullOrWhiteSpace(itemNotes))
            {
                var kwMatch = Regex.Match(
                    itemNotes, 
                    @"(?:أنفار|انفار|نفر|أجزاء|اجزاء|حصص|حصة)\s*[:=\-]?\s*(\d+)|(\d+)\s*(?:أنفار|انفار|نفر|أجزاء|اجزاء)");

                if (kwMatch.Success)
                {
                    string val = !string.IsNullOrEmpty(kwMatch.Groups[1].Value) ? kwMatch.Groups[1].Value : kwMatch.Groups[2].Value;
                    if (int.TryParse(val, out int parsed) && parsed > 0)
                    {
                        portionsPerCarcass = parsed;
                    }
                }
                else
                {
                    var numMatch = Regex.Match(itemNotes, @"\b(\d+)\b");
                    if (numMatch.Success && int.TryParse(numMatch.Value, out int parsedNum) && parsedNum > 0 && parsedNum < 1000)
                    {
                        portionsPerCarcass = parsedNum;
                    }
                }
            }

            return true;
        }
    }
}
