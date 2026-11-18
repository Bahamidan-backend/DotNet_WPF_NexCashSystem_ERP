using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;
using POSSystem.WPF.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace POSSystem.WPF.Business.Services
{
    public class ReturnOrderService : IReturnOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        public event EventHandler ReturnProcessed;

        public ReturnOrderService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<ReturnOrder>> GetAllReturnsAsync()
        {
            return await _unitOfWork.ReturnOrders.GetAllReturnsWithDetailsAsync();
        }

        public async Task<ReturnOrder?> GetReturnByIdAsync(int id)
        {
            return await _unitOfWork.ReturnOrders.GetReturnWithDetailsAsync(id);
        }

        public async Task<bool> ProcessReturnAsync(ReturnOrder returnOrder)
        {
            try
            {
                await _unitOfWork.ExecuteWithConcurrencyRetryAsync(async () =>
                {
                    if (_unitOfWork.HasActiveTransaction)
                        await _unitOfWork.RollbackTransactionAsync();

                    _unitOfWork.ClearEntityTracking();
                    returnOrder.Id = 0;
                    if (returnOrder.ReturnOrderItems != null)
                    {
                        foreach (var ri in returnOrder.ReturnOrderItems) ri.Id = 0;
                    }

                    await _unitOfWork.BeginTransactionAsync();

                    // 1. Validations:
                    var originalOrder = await _unitOfWork.Orders.GetOrderWithItemsAsync(returnOrder.OriginalOrderId);
                    if (originalOrder == null)
                        throw new Exception("الطلب الأصلي غير موجود.");

                    if (originalOrder.Status == OrderStatus.Cancelled || originalOrder.Status == OrderStatus.Returned)
                        throw new Exception("لا يمكن إرجاع طلب ملغي أو تم إرجاعه بالكامل مسبقاً.");

                    var originalItemsMap = (originalOrder.OrderItems ?? new List<OrderItem>())
                        .ToDictionary(oi => oi.Id);

                    // Check already returned quantities
                    var previousReturns = await _unitOfWork.ReturnOrders.GetReturnsByOriginalOrderIdAsync(returnOrder.OriginalOrderId);
                    var returnedItemsSoFar = previousReturns
                        .SelectMany(r => r.ReturnOrderItems)
                        .GroupBy(ri => ri.OriginalOrderItemId)
                        .ToDictionary(g => g.Key, g => g.Sum(ri => ri.Quantity));

                    decimal actualTotalAmount = 0m;

                    foreach (var item in returnOrder.ReturnOrderItems)
                    {
                        if (!originalItemsMap.TryGetValue(item.OriginalOrderItemId, out var originalItem) || originalItem == null)
                            throw new Exception($"الصنف رقم {item.OriginalOrderItemId} غير موجود في الطلب الفعلي.");

                        int returnedBefore = returnedItemsSoFar.ContainsKey(item.OriginalOrderItemId) ? returnedItemsSoFar[item.OriginalOrderItemId] : 0;

                        if (item.Quantity + returnedBefore > originalItem.Quantity)
                            throw new Exception($"الكمية المرتجعة للصنف '{originalItem.Name}' تتجاوز الكمية الأصلية المتبقية.");

                        // FIX ISSUE-FIN-003: Prorate order-level discount across returned items so refund never exceeds net amount paid
                        decimal discountRatio = (originalOrder.SubTotal > 0 && originalOrder.Discount > 0)
                            ? Math.Max(0m, (originalOrder.SubTotal - originalOrder.Discount) / originalOrder.SubTotal)
                            : 1m;
                        decimal effectiveUnitPrice = originalItem.Quantity > 0
                            ? Math.Round((originalItem.TotalPrice / originalItem.Quantity) * discountRatio, 2)
                            : Math.Round(originalItem.UnitPrice * discountRatio, 2);
                        item.UnitPrice = effectiveUnitPrice;
                        item.InitializeTotalAmount();

                        actualTotalAmount += item.TotalAmount;
                    }

                    // If the entire order is returned (quantity matches exactly for all items)
                    bool isFullReturn = true;
                    foreach (var oi in originalOrder.OrderItems)
                    {
                        int returningNow = returnOrder.ReturnOrderItems.Where(ri => ri.OriginalOrderItemId == oi.Id).Sum(ri => ri.Quantity);
                        int returnedBefore = returnedItemsSoFar.ContainsKey(oi.Id) ? returnedItemsSoFar[oi.Id] : 0;
                        if (returningNow + returnedBefore < oi.Quantity)
                        {
                            isFullReturn = false;
                            break;
                        }
                    }

                    originalOrder.Status = isFullReturn ? OrderStatus.Returned : OrderStatus.PartiallyReturned;

                    returnOrder.TotalAmount = actualTotalAmount;
                    returnOrder.ReturnDate = DateTime.Now;
                    returnOrder.ReturnNumber = await _unitOfWork.ReturnOrders.GenerateUniqueReturnNumberAsync();

                    await _unitOfWork.ReturnOrders.AddAsync(returnOrder);
                    _unitOfWork.Orders.Update(originalOrder);

                    // 2. Financial settlement logic:
                    // A) Cash: creates CashBoxTransaction (drawer cash deduction) + ReceiptVoucher (Payment voucher / سند صرف).
                    // B) OnCredit: updates CustomerDebt balance directly (no drawer cash movement).
                    // C) Card / Transfer / Wallet: electronic refund, no drawer cash deduction.
                    if (returnOrder.ReturnMethod != PaymentMethod.Remake)
                    {
                        if (returnOrder.ReturnMethod == PaymentMethod.Cash)
                        {
                            var activeShift = await _unitOfWork.Shifts.FirstOrDefaultAsync(s => s.UserId == returnOrder.CashierId && s.IsActive);

                            if (activeShift != null)
                            {
                                var refundTransaction = new CashBoxTransaction
                                {
                                    ShiftId = activeShift.Id,
                                    Amount = actualTotalAmount,
                                    Name = $"إرجاع طلب #{originalOrder.OrderNumber}",
                                    Description = $"إرجاع نقدي للطلب #{originalOrder.OrderNumber}، إشعار دائن {returnOrder.ReturnNumber}. السبب: {returnOrder.Reason}",
                                    TransactionDate = DateTime.Now,
                                    UserId = returnOrder.CashierId,
                                    Type = CashBoxTransactionType.Expense
                                };
                                await _unitOfWork.CashBoxTransactions.AddAsync(refundTransaction);
                            }

                            // إنشاء سند صرف رسمي (Payment Voucher) في جدول السندات برقم تسلسلي فريد
                            var customerName = originalOrder.CustomerDisplayName ?? originalOrder.Delivery?.DeliveryCustomerName ?? "عميل نقدي";
                            var now = DateTime.Now;
                            var voucherCode = $"PV-RET-{returnOrder.ReturnNumber}-{now:HHmmssfff}-{Guid.NewGuid().ToString("N")[..4]}";
                            var paymentVoucher = new ReceiptVoucher
                            {
                                VoucherNumber = voucherCode,
                                Name = voucherCode,
                                Amount = actualTotalAmount,
                                Type = VoucherType.Payment,
                                CustomerName = customerName,
                                Notes = $"سند صرف لمرتجع مبيعات نقدي (إشعار دائن #{returnOrder.ReturnNumber}) للطلب #{originalOrder.OrderNumber}. السبب: {returnOrder.Reason}",
                                UserId = returnOrder.CashierId,
                                CreatedAt = now,
                                VoucherDate = now
                            };
                            await _unitOfWork.ReceiptVouchers.AddAsync(paymentVoucher);
                        }
                        else if (returnOrder.ReturnMethod == PaymentMethod.OnCredit)
                        {
                            var debt = await _unitOfWork.CustomerDebts.FirstOrDefaultAsync(d => d.OrderId == originalOrder.Id);
                            if (debt != null && !debt.IsPaid)
                            {
                                decimal unpaidBefore = debt.RemainingAmount;
                                decimal excessRefund = Math.Max(0m, actualTotalAmount - unpaidBefore);
                                decimal debtReduction = Math.Min(unpaidBefore, actualTotalAmount);

                                debt.Amount = Math.Max(debt.PaidAmount, debt.Amount - debtReduction);
                                if (debt.RemainingAmount <= 0)
                                {
                                    debt.PaidAmount = debt.Amount;
                                    debt.IsPaid = true;
                                    debt.PaymentDate = DateTime.Now;
                                    debt.Notes = string.IsNullOrWhiteSpace(debt.Notes)
                                        ? $"[تمت تسوية الدين بالكامل عبر مرتجع {returnOrder.ReturnNumber}]"
                                        : $"{debt.Notes}\n+ [تمت تسوية الدين بالكامل عبر مرتجع {returnOrder.ReturnNumber}]";
                                }
                                else
                                {
                                    debt.Notes = string.IsNullOrWhiteSpace(debt.Notes)
                                        ? $"[خصم مرتجع {returnOrder.ReturnNumber}: {debtReduction:N0} ريال]"
                                        : $"{debt.Notes}\n+ [خصم مرتجع {returnOrder.ReturnNumber}: {debtReduction:N0} ريال]";
                                }
                                debt.UpdatedAt = DateTime.Now;
                                _unitOfWork.CustomerDebts.Update(debt);

                                if (excessRefund > 0)
                                {
                                    var activeShift = await _unitOfWork.Shifts.FirstOrDefaultAsync(s => s.UserId == returnOrder.CashierId && s.IsActive);
                                    if (activeShift != null)
                                    {
                                        await _unitOfWork.CashBoxTransactions.AddAsync(new CashBoxTransaction
                                        {
                                            ShiftId = activeShift.Id,
                                            Amount = excessRefund,
                                            Name = $"استرداد فائض مرتجع آجل #{originalOrder.OrderNumber}",
                                            Description = $"استرداد نقدي لفائض مرتجع طلب آجل #{originalOrder.OrderNumber} بعد تصفية المتبقي من الدين ({returnOrder.ReturnNumber})",
                                            TransactionDate = DateTime.Now,
                                            UserId = returnOrder.CashierId,
                                            Type = CashBoxTransactionType.Expense
                                        });
                                    }
                                }
                            }
                        }
                        else
                        {
                            // FIX ISSUE-FIN-004: Record official Payment Voucher (سند صرف) for electronic/wallet/card returns
                            var customerName = originalOrder.CustomerDisplayName ?? originalOrder.Delivery?.DeliveryCustomerName ?? "عميل";
                            var now = DateTime.Now;
                            var walletTag = !string.IsNullOrWhiteSpace(returnOrder.RefundWallet)
                                ? $" [طريقة الدفع: {returnOrder.RefundWallet}]"
                                : "";
                            var voucherCode = $"PV-RET-{returnOrder.ReturnNumber}-{now:HHmmssfff}-{Guid.NewGuid().ToString("N")[..4]}";
                            var paymentVoucher = new ReceiptVoucher
                            {
                                VoucherNumber = voucherCode,
                                Name = $"سند صرف مرتجع مبيعات - {returnOrder.ReturnNumber}",
                                Amount = actualTotalAmount,
                                Type = VoucherType.Payment,
                                PaymentMethod = returnOrder.ReturnMethod,
                                CustomerName = customerName,
                                Notes = $"سند صرف لمرتجع مبيعات إلكتروني (إشعار دائن #{returnOrder.ReturnNumber}) للطلب #{originalOrder.OrderNumber}.{walletTag} السبب: {returnOrder.Reason}",
                                UserId = returnOrder.CashierId,
                                CreatedAt = now,
                                UpdatedAt = now,
                                VoucherDate = now
                            };
                            await _unitOfWork.ReceiptVouchers.AddAsync(paymentVoucher);
                        }
                    }

                    // 3. Return to Stock / Waste logic:
                    // If condition is Waste (or Remake): DO NOT return to stock.
                    // Only Restock items return to inventory.
                    if (returnOrder.ReturnMethod != PaymentMethod.Remake)
                    {
                        var context = _unitOfWork.GetContext();
                        var nonWasteItems = returnOrder.ReturnOrderItems
                            .Where(item => item.Condition != ReturnItemCondition.Waste)
                            .ToList();

                        var returnedProductIds = nonWasteItems
                            .Select(item => originalItemsMap.TryGetValue(item.OriginalOrderItemId, out var oi) && oi.ProductId.HasValue ? oi.ProductId.Value : 0)
                            .Where(id => id > 0)
                            .Distinct()
                            .ToList();

                        var allRecipes = returnedProductIds.Count > 0
                            ? await context.Recipes.AsNoTracking().Where(r => returnedProductIds.Contains(r.ProductId)).ToListAsync()
                            : new List<Recipe>();

                        var recipesByProduct = allRecipes.GroupBy(r => r.ProductId).ToDictionary(g => g.Key, g => g.ToList());

                        var whIds = allRecipes.Where(r => r.WarehouseFromProductId.HasValue).Select(r => r.WarehouseFromProductId!.Value).Distinct().ToList();
                        var whNames = allRecipes.Where(r => !string.IsNullOrWhiteSpace(r.Name)).Select(r => r.Name.Trim()).Distinct().ToList();

                        var warehouseItems = (whIds.Count > 0 || whNames.Count > 0)
                            ? await context.warehouseFromProducts.Where(w => whIds.Contains(w.Id) || (w.Name != null && whNames.Contains(w.Name))).ToListAsync()
                            : new List<WarehouseFromProduct>();

                        var whById = warehouseItems.ToDictionary(w => w.Id);
                        var whByName = warehouseItems
                            .Where(w => !string.IsNullOrWhiteSpace(w.Name))
                            .GroupBy(w => w.Name!.Trim(), StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                        foreach (var item in nonWasteItems)
                        {
                            if (!originalItemsMap.TryGetValue(item.OriginalOrderItemId, out var originalItem) || originalItem == null)
                                continue;

                            if (originalItem.ProductId.HasValue && originalItem.Product != null)
                            {
                                var product = originalItem.Product;
                                decimal addition = PortionSizeHelper.CalculateDeduction(originalItem.Size, item.Quantity);
                                decimal factor = addition > 0 ? addition : item.Quantity;

                                if (!product.IsUntrackedStock) product.TotalValue += addition;

                                if (product.PersonNumberFood > 0)
                                {
                                    product.StockQuantity = product.TotalValue / product.PersonNumberFood;
                                }
                                else
                                {
                                    product.StockQuantity = product.TotalValue;
                                }

                                _unitOfWork.Products.Update(product);

                                // 3A.0: If product has no Recipe but is directly linked to a warehouse item via TypeWarehoseFromProduct, restore warehouse stock
                                if ((!recipesByProduct.TryGetValue(product.Id, out var checkRecipes) || checkRecipes.Count == 0) &&
                                    !string.IsNullOrWhiteSpace(product.TypeWarehoseFromProduct))
                                {
                                    var trimmedLink = product.TypeWarehoseFromProduct.Trim();
                                    WarehouseFromProduct? directWhItem = null;
                                    if (int.TryParse(trimmedLink, out int directWhId))
                                    {
                                        directWhItem = await context.warehouseFromProducts.FirstOrDefaultAsync(w => w.Id == directWhId);
                                    }
                                    else
                                    {
                                        directWhItem = await context.warehouseFromProducts.FirstOrDefaultAsync(w => w.Name == trimmedLink);
                                    }

                                    if (directWhItem != null)
                                    {
                                        decimal restoreQty = product.PersonNumberFood > 0 ? (addition / product.PersonNumberFood) : addition;
                                        decimal beforeQty = directWhItem.StockQuantity;
                                        directWhItem.StockQuantity += restoreQty;
                                        directWhItem.UpdatedAt = DateTime.Now;
                                        _unitOfWork.warehouseFromProduct.Update(directWhItem);

                                        await _unitOfWork.StockMovements.AddAsync(new StockMovement
                                        {
                                            WarehouseFromProductId = directWhItem.Id,
                                            WarehouseId = directWhItem.WarehouseId,
                                            MovementType = StockMovementType.SaleReturn,
                                            Quantity = restoreQty,
                                            BalanceBefore = beforeQty,
                                            BalanceAfter = directWhItem.StockQuantity,
                                            UnitCost = directWhItem.Price,
                                            TotalCost = Math.Round(restoreQty * directWhItem.Price, 2),
                                            MovementDate = DateTime.Now,
                                            CreatedAt = DateTime.Now,
                                            UserId = returnOrder.CashierId,
                                            OrderId = originalOrder.Id,
                                            ReferenceNumber = returnOrder.ReturnNumber,
                                            Notes = $"مرتجع مبيعات مباشر للمخزن #{returnOrder.ReturnNumber} (فاتورة #{originalOrder.OrderNumber})"
                                        });
                                    }
                                }

                                // 3A. Restore recipe raw materials to warehouse ONLY if product was not batch-cooked
                                if (recipesByProduct.TryGetValue(product.Id, out var recipes) && recipes.Count > 0)
                                {
                                    bool isBatchCookedProduct = (recipes.Any(r => r.IsMainIngredient)) || 
                                                               product.YieldRate > 0 || 
                                                               product.AvailableQtyInPOS > 0;

                                    if (!isBatchCookedProduct)
                                    {
                                        foreach (var recipe in recipes)
                                        {
                                            if (recipe.Quantity <= 0) continue;
                                            if (recipe.IsMainIngredient) continue;

                                            WarehouseFromProduct? whIngredient = null;
                                            if (recipe.WarehouseFromProductId.HasValue && whById.TryGetValue(recipe.WarehouseFromProductId.Value, out var foundById))
                                            {
                                                whIngredient = foundById;
                                            }
                                            else if (!string.IsNullOrWhiteSpace(recipe.Name) && whByName.TryGetValue(recipe.Name.Trim(), out var foundByName))
                                            {
                                                whIngredient = foundByName;
                                            }

                                            if (whIngredient != null)
                                            {
                                                decimal convertedQty = UnitConversionHelper.ConvertQuantity(recipe.Quantity * factor, recipe.portionUnit, whIngredient.PortionUnit);
                                                var beforeQty = whIngredient.StockQuantity;
                                                whIngredient.StockQuantity += convertedQty;
                                                whIngredient.UpdatedAt = DateTime.Now;
                                                _unitOfWork.warehouseFromProduct.Update(whIngredient);

                                                var movement = new StockMovement
                                                {
                                                    WarehouseFromProductId = whIngredient.Id,
                                                    WarehouseId = whIngredient.WarehouseId,
                                                    MovementType = StockMovementType.SaleReturn,
                                                    Quantity = convertedQty,
                                                    BalanceBefore = beforeQty,
                                                    BalanceAfter = whIngredient.StockQuantity,
                                                    UnitCost = whIngredient.Price,
                                                    TotalCost = Math.Round(convertedQty * whIngredient.Price, 2),
                                                    MovementDate = DateTime.Now,
                                                    CreatedAt = DateTime.Now,
                                                    UserId = returnOrder.CashierId,
                                                    OrderId = originalOrder.Id,
                                                    ReferenceNumber = returnOrder.ReturnNumber,
                                                    Notes = $"مرتجع مبيعات إشعار #{returnOrder.ReturnNumber} (فاتورة #{originalOrder.OrderNumber})"
                                                };
                                                await _unitOfWork.StockMovements.AddAsync(movement);
                                            }
                                        }
                                    }
                                }
                            }
                            else if (originalItem.AddonId.HasValue && originalItem.Addon != null)
                            {
                                var addon = originalItem.Addon;
                                addon.Quantity += item.Quantity;
                                _unitOfWork.Addons.Update(addon);
                            }

                            // 3B. Restore linked addons (addonAndOrderItems)
                            if (originalItem.addonAndOrderItems != null && originalItem.addonAndOrderItems.Any())
                            {
                                foreach (var aoi in originalItem.addonAndOrderItems)
                                {
                                    if (aoi.Addon != null)
                                    {
                                        aoi.Addon.Quantity += item.Quantity;
                                        _unitOfWork.Addons.Update(aoi.Addon);
                                    }
                                    else if (aoi.AddonId > 0)
                                    {
                                        var linkedAddon = await _unitOfWork.Addons.GetByIdAsync(aoi.AddonId);
                                        if (linkedAddon != null)
                                        {
                                            linkedAddon.Quantity += item.Quantity;
                                            _unitOfWork.Addons.Update(linkedAddon);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();
                });

                ReturnProcessed?.Invoke(this, EventArgs.Empty);
                ServiceLocator.NotifyInventoryChanged();
                ServiceLocator.NotifyProductsChanged();

                return true;
            }
            catch (Exception ex)
            {
                if (_unitOfWork.HasActiveTransaction)
                    await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<IEnumerable<ReturnOrder>> GetReturnsByOriginalOrderIdAsync(int orderId)
        {
            return await _unitOfWork.ReturnOrders.GetReturnsByOriginalOrderIdAsync(orderId);
        }

        public async Task<IEnumerable<Order>> GetOrdersAvailableForReturnAsync()
        {
            var recentDate = DateTime.Now.Date.AddDays(-7);
            var endDate = DateTime.Now.Date.AddDays(1);

            return await _unitOfWork.GetContext().Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Addon)
                .Where(o => o.CreatedAt >= recentDate && o.CreatedAt < endDate &&
                           (o.Status == OrderStatus.Pending ||
                            o.Status == OrderStatus.Completed ||
                            o.Status == OrderStatus.Served ||
                            o.Status == OrderStatus.PartiallyReturned))
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }
    }
}
