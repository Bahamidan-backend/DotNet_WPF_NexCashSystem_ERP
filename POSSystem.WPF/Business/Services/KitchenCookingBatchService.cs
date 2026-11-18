using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;
using POSSystem.WPF.Models.DTOs;
using POSSystem.WPF.Services.Logging;

namespace POSSystem.WPF.Business.Services
{
    public class KitchenCookingBatchService : IKitchenCookingBatchService
    {
        private readonly IUnitOfWork _unitOfWork;

        public KitchenCookingBatchService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Product>> GetBatchCookableProductsAsync()
        {
            var context = _unitOfWork.GetContext();
            var products = await context.Products
                .AsNoTracking()
                .Include(p => p.Recipe)
                .Include(p => p.Category)
                .Where(p => p.IsActive && !p.IsAnimalComponent &&
                            ((p.Recipe != null && p.Recipe.Any()) ||
                             (p.TypeWarehoseFromProduct != null && p.TypeWarehoseFromProduct != "") ||
                             p.YieldRate > 0))
                .OrderBy(p => p.Name)
                .ToListAsync();

            var allRecipes = await context.Recipes
                .AsNoTracking()
                .ToListAsync();

            var recipesByProduct = allRecipes
                .GroupBy(r => r.ProductId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var p in products)
            {
                if (recipesByProduct.TryGetValue(p.Id, out var rList) && rList.Count > 0)
                {
                    p.Recipe = rList;
                }
                else
                {
                    p.Recipe ??= new List<Recipe>();
                }
            }

            return products;
        }

        public async Task<IEnumerable<KitchenCookingBatch>> GetBatchesByDateAsync(DateTime fromDate, DateTime toDate)
        {
            var start = fromDate.Date;
            var end = toDate.Date.AddDays(1).AddTicks(-1);

            return await _unitOfWork.KitchenCookingBatches
                .Include(b => b.Product)
                .Include(b => b.Warehouse)
                .Include(b => b.Items)
                .Include(b => b.CookedByUser)
                .Where(b => b.CookedAt >= start && b.CookedAt <= end)
                .OrderByDescending(b => b.CookedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<KitchenCookingBatch>> GetRecentSettleableBatchesAsync(int daysBack = 3)
        {
            var cutoff = DateTime.Today.AddDays(-Math.Max(1, daysBack));
            return await _unitOfWork.KitchenCookingBatches
                .Include(b => b.Product)
                .Include(b => b.Warehouse)
                .Include(b => b.Items)
                .Include(b => b.CookedByUser)
                .Where(b => b.CookedAt >= cutoff && b.Status == "Completed")
                .OrderByDescending(b => b.CookedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<KitchenCookingBatch?> GetBatchByIdAsync(int id)
        {
            return await _unitOfWork.KitchenCookingBatches
                .Include(b => b.Product)
                .Include(b => b.Warehouse)
                .Include(b => b.Items)
                .Include(b => b.CookedByUser)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<KitchenCookingBatch> CreateBatchAsync(CreateCookingBatchDto dto)
        {
            if (dto.ProductId <= 0)
                throw new ArgumentException("يجب اختيار الوجبة المراد طبخها.");

            KitchenCookingBatch createdBatch = null!;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var context = _unitOfWork.GetContext();

                var product = await context.Products
                    .Include(p => p.Recipe)
                    .FirstOrDefaultAsync(p => p.Id == dto.ProductId);

                if (product == null)
                    throw new InvalidOperationException("الوجبة المختارة غير موجودة في النظام.");

                if (product.Recipe == null || !product.Recipe.Any())
                {
                    var dbRecipes = await context.Recipes.Where(r => r.ProductId == product.Id).ToListAsync();
                    if (dbRecipes.Count > 0)
                    {
                        product.Recipe = dbRecipes;
                    }
                    else
                    {
                        var fallbackWh = await context.warehouseFromProducts
                            .FirstOrDefaultAsync(w => w.IsActive &&
                                ((!string.IsNullOrEmpty(product.TypeWarehoseFromProduct) && (w.Name == product.TypeWarehoseFromProduct || w.Id.ToString() == product.TypeWarehoseFromProduct)) ||
                                 (w.Name != null && w.Name == product.Name)));
                        if (fallbackWh != null)
                        {
                            product.Recipe = new List<Recipe>
                            {
                                new Recipe
                                {
                                    ProductId = product.Id,
                                    WarehouseFromProductId = fallbackWh.Id,
                                    Name = fallbackWh.Name ?? product.Name,
                                    Quantity = 1m,
                                    portionUnit = product.portionUnit != 0 ? product.portionUnit : (fallbackWh.PortionUnit ?? Recipe.PortionUnit.Bag),
                                    IsMainIngredient = true
                                }
                            };
                        }
                        else
                        {
                            throw new InvalidOperationException($"الوجبة '{product.Name}' ليس لها مقادير أو وصفة مسجلة. يرجى إضافة وصفة للمنتج من شاشة إدارة المنتجات (زر وصفة) أولاً.");
                        }
                    }
                }

                var mainRecipe = product.Recipe.FirstOrDefault(r => r.IsMainIngredient) ?? product.Recipe.First();

                // حساب معامل التكبير/التصغير (Scaling Ratio)
                decimal scalingRatio;
                if (dto.MainIngredientQuantity > 0 && mainRecipe.Quantity > 0)
                {
                    scalingRatio = dto.MainIngredientQuantity / mainRecipe.Quantity;
                }
                else
                {
                    scalingRatio = dto.BatchMultiplier > 0 ? dto.BatchMultiplier : 1.0m;
                }

                if (scalingRatio <= 0)
                    scalingRatio = 1.0m;

                // جمع معرفات وأسماء المكونات للتحقق من المخزن
                var recipeWhIds = product.Recipe
                    .Where(r => r.WarehouseFromProductId.HasValue)
                    .Select(r => r.WarehouseFromProductId!.Value)
                    .Distinct()
                    .ToList();

                var recipeNames = product.Recipe
                    .Where(r => !string.IsNullOrWhiteSpace(r.Name))
                    .Select(r => r.Name.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var whItems = await context.warehouseFromProducts
                    .Where(w => w.IsActive && (recipeWhIds.Contains(w.Id) || (w.Name != null && recipeNames.Contains(w.Name))))
                    .ToListAsync();

                var whItemsById = whItems.ToDictionary(w => w.Id);
                var whItemsByName = new Dictionary<string, WarehouseFromProduct>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in whItems)
                {
                    if (!string.IsNullOrWhiteSpace(item.Name) && !whItemsByName.ContainsKey(item.Name.Trim()))
                        whItemsByName[item.Name.Trim()] = item;
                }

                // 1. التحقق من كفاية المخزون لجميع المكونات قبل البدء
                foreach (var recipe in product.Recipe)
                {
                    if (recipe.Quantity <= 0) continue;

                    WarehouseFromProduct? whItem = null;
                    if (recipe.WarehouseFromProductId.HasValue && whItemsById.TryGetValue(recipe.WarehouseFromProductId.Value, out var foundById))
                        whItem = foundById;
                    else if (!string.IsNullOrWhiteSpace(recipe.Name) && whItemsByName.TryGetValue(recipe.Name.Trim(), out var foundByName))
                        whItem = foundByName;

                    if (whItem == null)
                        throw new InvalidOperationException($"المادة الخام '{recipe.Name}' غير موجودة في المستودع.");

                    decimal rawNeeded = recipe.Quantity * scalingRatio;
                    var targetUnit = whItem.PortionUnit ?? recipe.portionUnit;
                    decimal convertedNeeded = UnitConversionHelper.ConvertQuantity(rawNeeded, recipe.portionUnit, targetUnit);

                    if (whItem.StockQuantity < convertedNeeded)
                    {
                        var unitName = PortionUnitHelper.GetDisplayName(targetUnit);
                        throw new InvalidOperationException($"المخزون غير كافٍ للمادة '{whItem.Name}'. المتوفر بالمستودع: {whItem.StockQuantity:N0} {unitName}، المطلوب للطبخة: {convertedNeeded:N0} {unitName}");
                    }
                }

                // 2. إنشاء كائن الطبخة
                var now = DateTime.Now;
                string batchNumber = $"COOK-{now:yyyyMMddHHmmss}-{product.Id}";

                decimal expectedPortions = product.YieldRate > 0 
                    ? (decimal)product.YieldRate * scalingRatio 
                    : (dto.ActualPortions > 0 ? dto.ActualPortions : 100m);

                decimal finalActualPortions = dto.ActualPortions > 0 ? dto.ActualPortions : expectedPortions;

                var batch = new KitchenCookingBatch
                {
                    BatchNumber = batchNumber,
                    ProductId = product.Id,
                    WarehouseId = dto.WarehouseId > 0 ? dto.WarehouseId : (whItems.FirstOrDefault()?.WarehouseId ?? 1),
                    MainIngredientId = mainRecipe.WarehouseFromProductId,
                    BatchMultiplier = scalingRatio,
                    MainIngredientQuantity = dto.MainIngredientQuantity > 0 ? dto.MainIngredientQuantity : (mainRecipe.Quantity * scalingRatio),
                    PortionUnit = mainRecipe.portionUnit,
                    ExpectedPortions = expectedPortions,
                    ActualPortions = finalActualPortions,
                    CookedByUserId = dto.CookedByUserId,
                    ShiftId = dto.ShiftId,
                    Notes = dto.Notes,
                    CookedAt = now,
                    Status = "Completed"
                };

                decimal totalRawCost = 0m;

                // 3. خصم المكونات من المستودع وتسجيل حركات المخزون والبنود
                foreach (var recipe in product.Recipe)
                {
                    if (recipe.Quantity <= 0) continue;

                    WarehouseFromProduct? whItem = null;
                    if (recipe.WarehouseFromProductId.HasValue && whItemsById.TryGetValue(recipe.WarehouseFromProductId.Value, out var foundById))
                        whItem = foundById;
                    else if (!string.IsNullOrWhiteSpace(recipe.Name) && whItemsByName.TryGetValue(recipe.Name.Trim(), out var foundByName))
                        whItem = foundByName;

                    if (whItem == null) continue;

                    decimal rawNeeded = recipe.Quantity * scalingRatio;
                    var targetUnit = whItem.PortionUnit ?? recipe.portionUnit;
                    decimal convertedQty = UnitConversionHelper.ConvertQuantity(rawNeeded, recipe.portionUnit, targetUnit);

                    decimal beforeStock = whItem.StockQuantity;
                    whItem.StockQuantity -= convertedQty;
                    decimal afterStock = whItem.StockQuantity;
                    whItem.UpdatedAt = now;

                    decimal itemCost = convertedQty * whItem.Price;
                    totalRawCost += itemCost;

                    batch.Items.Add(new KitchenCookingBatchItem
                    {
                        WarehouseFromProductId = whItem.Id,
                        ItemName = whItem.Name ?? recipe.Name,
                        Quantity = convertedQty,
                        PortionUnit = targetUnit,
                        UnitCost = whItem.Price,
                        TotalCost = itemCost
                    });

                    await context.StockMovements.AddAsync(new StockMovement
                    {
                        WarehouseFromProductId = whItem.Id,
                        WarehouseId = whItem.WarehouseId,
                        MovementType = StockMovementType.RecipeCooking,
                        Quantity = -convertedQty,
                        BalanceBefore = beforeStock,
                        BalanceAfter = afterStock,
                        UnitCost = whItem.Price,
                        TotalCost = itemCost,
                        UserId = dto.CookedByUserId,
                        ReferenceNumber = batchNumber,
                        Notes = $"استهلاك طهي للطبخة #{batchNumber} - وجبة '{product.Name}'",
                        MovementDate = now,
                        CreatedAt = now
                    });
                }

                batch.TotalRawCost = totalRawCost;

                // 4. تحديث رصيد الكاشير (زيادة الأنفار المتاحة للبيع فوراً)
                product.AvailableQtyInPOS += finalActualPortions;
                product.LastCookedQty = batch.MainIngredientQuantity;
                if (batch.MainIngredientQuantity > 0)
                {
                    product.YieldRate = (int)Math.Round(finalActualPortions / batch.MainIngredientQuantity);
                }
                product.UpdatedAt = now;

                // 5. إذا كان هناك عجز في الطبخة، تسجيل قيد هدر في StockWastes للمخزون والتقارير المالية
                if (finalActualPortions < expectedPortions)
                {
                    decimal deficit = expectedPortions - finalActualPortions;
                    decimal portionCost = expectedPortions > 0 ? (totalRawCost / expectedPortions) : 0m;
                    decimal wasteCost = Math.Round(deficit * portionCost, 2);

                    var waste = new StockWaste
                    {
                        WarehouseFromProductId = batch.MainIngredientId ?? (whItems.FirstOrDefault()?.Id ?? 1),
                        WarehouseId = batch.WarehouseId,
                        Quantity = deficit,
                        PortionUnit = Recipe.PortionUnit.Piece,
                        UnitCost = portionCost,
                        TotalCost = wasteCost,
                        Reason = WasteReason.CookingBurn,
                        WasteDate = now,
                        ReportedByUserId = dto.CookedByUserId,
                        Notes = $"عجز طبخة قدر #{batchNumber} - وجبة '{product.Name}': نقص معتمد {deficit:N0} نفر. {dto.Notes}",
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    await context.StockWastes.AddAsync(waste);
                }

                await context.KitchenCookingBatches.AddAsync(batch);
                await context.SaveChangesAsync();

                createdBatch = batch;
            });

            ServiceLocator.NotifyProductsChanged();
            ServiceLocator.NotifyInventoryChanged();
            AppLogger.LogInfo($"[COOKING BATCH] #{createdBatch.BatchNumber} created for ProductId {dto.ProductId}. Portions added to POS: {createdBatch.ActualPortions:N0}, Total raw cost: {createdBatch.TotalRawCost:N0}");

            return createdBatch;
        }

        /// <summary>
        /// تسوية وفر وزيادة في القدر بعد انتهاء الطبخ:
        /// تزيد رصيد الوجبة الجاهزة للبيع في الكاشير (AvailableQtyInPOS) وتحدث الإنتاج الفعلي للطبخة (ActualPortions)
        /// دون سحب أي مواد خام جديدة من المستودع (WarehouseFromProduct.StockQuantity لا يتغير).
        /// </summary>
        public async Task<KitchenCookingBatch> SettleCookingSurplusAsync(
            int batchId,
            decimal surplusPortions,
            string reason,
            int? userId = null,
            string? userName = null)
        {
            if (batchId <= 0)
                throw new ArgumentException("يجب اختيار دفعة الطهي / القدر المراد تسوية الزيادة له.");

            if (surplusPortions <= 0)
                throw new ArgumentException("يجب أن تكون كمية الزيادة (عدد الأنفار الإضافية) أكبر من صفر.");

            KitchenCookingBatch updatedBatch = null!;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var context = _unitOfWork.GetContext();

                var batch = await context.KitchenCookingBatches
                    .Include(b => b.Product)
                    .FirstOrDefaultAsync(b => b.Id == batchId);

                if (batch == null)
                    throw new InvalidOperationException("دفعة الطهي المختارة غير موجودة.");

                if (!string.IsNullOrWhiteSpace(batch.Notes) && batch.Notes.Contains("[تسوية وفر معتمدة:"))
                    throw new InvalidOperationException($"تم تسجيل تسوية زيادة سابقة لهذا القدر ({batch.BatchNumber}) بالفعل ولا يمكن تكرار تسوية نفس الدفعة مرتين.");

                var product = await context.Products.FirstOrDefaultAsync(p => p.Id == batch.ProductId);
                if (product == null)
                    throw new InvalidOperationException("الوجبة المرتبطة بدفعة الطهي غير موجودة.");

                var now = DateTime.Now;
                decimal previousActual = batch.ActualPortions;
                decimal newActual = previousActual + surplusPortions;

                decimal posBefore = product.AvailableQtyInPOS;
                product.AvailableQtyInPOS += surplusPortions;
                product.UpdatedAt = now;

                batch.ActualPortions = newActual;
                string actor = !string.IsNullOrWhiteSpace(userName)
                    ? userName
                    : (userId.HasValue ? $"مستخدم #{userId.Value}" : "النظام");
                string cleanReason = string.IsNullOrWhiteSpace(reason) ? "وفر فعلي في إنتاج القدر بعد الغرف" : reason.Trim();
                string settlementTag = $"[تسوية وفر معتمدة: +{surplusPortions:G29} نفر (من {previousActual:G29} إلى {newActual:G29} نفر) بدون سحب مواد خام | السبب: {cleanReason} | بواسطة: {actor} | التاريخ: {now:yyyy/MM/dd HH:mm}]";

                batch.Notes = string.IsNullOrWhiteSpace(batch.Notes)
                    ? settlementTag
                    : $"{batch.Notes} | {settlementTag}";

                await context.SaveChangesAsync();

                AppLogger.LogAudit(
                    userId?.ToString() ?? "SYSTEM",
                    "COOKING_SURPLUS_SETTLEMENT",
                    $"Batch #{batch.BatchNumber} ({product.Name}): Added +{surplusPortions:G29} surplus portions to POS (Before: {posBefore:G29}, After: {product.AvailableQtyInPOS:G29}) WITHOUT raw material deduction. Reason: {cleanReason}");

                updatedBatch = batch;
            });

            ServiceLocator.NotifyProductsChanged();
            ServiceLocator.NotifyInventoryChanged();

            return updatedBatch;
        }

                /// <summary>
        /// صرف الأكل الزايد / المتبقي من شاشة الكاشير (إطعام العمال / صدقة / هدر وتالف نهاية اليوم)
        /// باعتماد الرقم السري للمدير، مع خصم الكمية فوراً من رصيد الكاشير (AvailableQtyInPOS) وتوثيقها في سجلات الصرف والهدر.
        /// </summary>
        public async Task<int> DisposeLeftoverPosStockAsync(
            List<LeftoverDisposalItemDto> items,
            WasteReason reason,
            string disposalCategoryLabel,
            string? notes = null,
            int? userId = null,
            string? supervisorName = null)
        {
            if (items == null || items.Count == 0)
                throw new ArgumentException("يرجى اختيار صنف واحد على الأقل لصرفه من الكاشير.");

            var validItems = items.Where(i => i.ProductId > 0 && i.QuantityToDispose > 0).ToList();
            if (validItems.Count == 0)
                throw new ArgumentException("يرجى إدخال كمية صحيحة أكبر من الصفر للصرف.");

            int processedCount = 0;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var context = _unitOfWork.GetContext();
                var now = DateTime.Now;
                string cleanNotes = string.IsNullOrWhiteSpace(notes) ? "" : $" | ملاحظة: {notes.Trim()}";
                string approver = string.IsNullOrWhiteSpace(supervisorName) ? "المدير" : supervisorName.Trim();

                // Find a fallback WarehouseFromProduct ID in case a product has no linked recipe ingredient
                var defaultWhItem = await context.warehouseFromProducts.AsNoTracking().FirstOrDefaultAsync(w => w.IsActive)
                                 ?? await context.warehouseFromProducts.AsNoTracking().FirstOrDefaultAsync();

                foreach (var req in validItems)
                {
                    var product = await context.Products
                        .Include(p => p.Recipe)
                        .FirstOrDefaultAsync(p => p.Id == req.ProductId);

                    if (product == null)
                        continue;

                    if (product.AvailableQtyInPOS < req.QuantityToDispose)
                    {
                        throw new InvalidOperationException(
                            $"الكمية المراد صرفها من '{product.Name}' ({req.QuantityToDispose:0.##}) أكبر من المتبقي حالياً في الكاشير ({product.AvailableQtyInPOS:0.##}).");
                    }

                    decimal posBefore = product.AvailableQtyInPOS;
                    product.AvailableQtyInPOS = Math.Max(0m, product.AvailableQtyInPOS - req.QuantityToDispose);
                    product.UpdatedAt = now;

                    // Estimate unit cost from recipe or BasePrice
                    decimal unitCost = product.BasePrice > 0 ? product.BasePrice : 0m;
                    int? matchedWhProductId = null;
                    int? matchedWarehouseId = 1;

                    var mainRecipe = product.Recipe?.FirstOrDefault(r => r.IsMainIngredient) ?? product.Recipe?.FirstOrDefault();
                    if (mainRecipe != null && mainRecipe.WarehouseFromProductId.HasValue)
                    {
                        matchedWhProductId = mainRecipe.WarehouseFromProductId.Value;
                        var whProd = await context.warehouseFromProducts.AsNoTracking().FirstOrDefaultAsync(w => w.Id == matchedWhProductId.Value);
                        if (whProd != null)
                        {
                            matchedWarehouseId = whProd.WarehouseId;
                            if (unitCost <= 0 && product.YieldRate > 0)
                            {
                                unitCost = Math.Round((whProd.Price * mainRecipe.Quantity) / product.YieldRate, 2);
                            }
                        }
                    }

                    if (!matchedWhProductId.HasValue && defaultWhItem != null)
                    {
                        matchedWhProductId = defaultWhItem.Id;
                        matchedWarehouseId = defaultWhItem.WarehouseId;
                    }

                    decimal totalWasteCost = Math.Round(unitCost * req.QuantityToDispose, 2);
                    string sizeTag = !string.IsNullOrWhiteSpace(req.SizeName) ? $" ({req.SizeName})" : "";

                    if (matchedWhProductId.HasValue)
                    {
                        var wasteRecord = new StockWaste
                        {
                            WarehouseFromProductId = matchedWhProductId.Value,
                            WarehouseId = matchedWarehouseId,
                            Quantity = req.QuantityToDispose,
                            PortionUnit = Recipe.PortionUnit.Piece,
                            UnitCost = unitCost,
                            TotalCost = totalWasteCost,
                            Reason = reason,
                            WasteDate = now,
                            ReportedByUserId = userId,
                            Notes = $"[صرف متبقي الكاشير: {disposalCategoryLabel}] الصنف: {product.Name}{sizeTag} | الكمية المصروفة: {req.QuantityToDispose:0.##} (قبل: {posBefore:0.##} -> بعد: {product.AvailableQtyInPOS:0.##}) | باعتماد: {approver}{cleanNotes}",
                            CreatedAt = now,
                            UpdatedAt = now
                        };
                        await context.StockWastes.AddAsync(wasteRecord);
                    }

                    AppLogger.LogAudit(
                        userId?.ToString() ?? "SYSTEM",
                        "CASHIER_LEFTOVER_DISPOSAL",
                        $"Disposed {req.QuantityToDispose:0.##} of '{product.Name}' as [{disposalCategoryLabel}] (POS Before: {posBefore:0.##}, After: {product.AvailableQtyInPOS:0.##}). Approved by: {approver}{cleanNotes}");

                    processedCount++;
                }

                await context.SaveChangesAsync();
            });

            ServiceLocator.NotifyProductsChanged();
            ServiceLocator.NotifyInventoryChanged();

            return processedCount;
        }
    }
}
