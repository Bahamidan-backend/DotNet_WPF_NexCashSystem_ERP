

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Models;
using POSSystem.WPF.Services.Logging;

namespace POSSystem.WPF.Business.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IUnitOfWork _unitOfWork;

        public WarehouseService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Warehouse>> GetAllWarehousesAsync()
        {
            _unitOfWork.ClearEntityTracking();
            return await _unitOfWork.Warehouses.GetAllAsync();
        }

        public async Task<Warehouse?> GetWarehouseByIdAsync(int id)
        {
            return await _unitOfWork.Warehouses.GetByIdAsync(id);
        }

        public async Task<Warehouse> CreateWarehouseAsync(Warehouse warehouse)
        {
            if (string.IsNullOrWhiteSpace(warehouse.Name))
                throw new ArgumentException("اسم المستودع مطلوب.");

            warehouse.Name = warehouse.Name.Trim();
            bool exists = await _unitOfWork.GetContext().Warehouses
                .AsNoTracking()
                .AnyAsync(w => w.Name.ToLower() == warehouse.Name.ToLower());
            if (exists)
                throw new InvalidOperationException($"يوجد مستودع مسجل بنفس الاسم ({warehouse.Name}) مسبقاً.");

            warehouse.CreatedAt = DateTime.Now;
            warehouse.IsActive = true;
            
            var createdWarehouse = await _unitOfWork.Warehouses.AddAsync(warehouse);
            await _unitOfWork.SaveChangesAsync();
            
            return createdWarehouse;
        }

        public async Task<Warehouse> UpdateWarehouseAsync(Warehouse warehouse)
        {
            if (string.IsNullOrWhiteSpace(warehouse.Name))
                throw new ArgumentException("اسم المستودع مطلوب.");

            warehouse.Name = warehouse.Name.Trim();
            bool exists = await _unitOfWork.GetContext().Warehouses
                .AsNoTracking()
                .AnyAsync(w => w.Id != warehouse.Id && w.Name.ToLower() == warehouse.Name.ToLower());
            if (exists)
                throw new InvalidOperationException($"يوجد مستودع آخر مسجل بنفس الاسم ({warehouse.Name}).");

            warehouse.UpdatedAt = DateTime.Now;
            
            _unitOfWork.ClearEntityTracking();
            _unitOfWork.Warehouses.Update(warehouse);
            await _unitOfWork.SaveChangesAsync();
            
            return warehouse;
        }

      

        public async Task<IEnumerable<Warehouse>> GetWarehousesWithLowStockAsync()
        {
            return await _unitOfWork.Warehouses.GetWarehousesWithLowStockAsync();
        }

     

        public async Task<bool> AssignProductToWarehouseAsync(int productId, int warehouseId)
        {
            return await _unitOfWork.GetContext().Products
                .AsNoTracking()
                .AnyAsync(p => p.Id == productId);
        }

        public async Task<bool> RemoveProductFromWarehouseAsync(int productId)
        {
            return await _unitOfWork.GetContext().Products
                .AsNoTracking()
                .AnyAsync(p => p.Id == productId);
        }















        // 1. إنشاء المادة وتسجيل الحركة الافتتاحية في اتصال واحد (Single Round-Trip)
        public async Task<WarehouseFromProduct> CreateProductFromWarehouse(WarehouseFromProduct warehouseFromProduct)
        {
            var now = DateTime.Now;
            await _unitOfWork.warehouseFromProduct.AddAsync(warehouseFromProduct);

            if (warehouseFromProduct.StockQuantity > 0)
            {
                await _unitOfWork.StockMovements.AddAsync(new StockMovement
                {
                    WarehouseFromProduct = warehouseFromProduct,
                    WarehouseId = warehouseFromProduct.WarehouseId,
                    MovementType = StockMovementType.InitialStock,
                    Quantity = warehouseFromProduct.StockQuantity,
                    BalanceBefore = 0,
                    BalanceAfter = warehouseFromProduct.StockQuantity,
                    UnitCost = warehouseFromProduct.Price,
                    TotalCost = warehouseFromProduct.Price * warehouseFromProduct.StockQuantity,
                    MovementDate = now,
                    CreatedAt = now,
                    ReferenceNumber = $"INIT-{warehouseFromProduct.Id}",
                    Notes = "رصيد افتتاحي عند إنشاء المادة"
                });
            }

            await _unitOfWork.SaveChangesAsync();
            return warehouseFromProduct;
        }

        public async Task UpdateProductFromWarehouse(WarehouseFromProduct warehouseFromProduct)
        {
            _unitOfWork.warehouseFromProduct.UpdateWarehouseFromProducts(warehouseFromProduct);
            await _unitOfWork.SaveChangesAsync();
        }

        // 2. تحديث الكمية وتسجيل حركة التسوية في حفظ واحد فقط
        public async Task UpdateQuntityWarehouseFromProduct(int id, decimal newQuantity, int? userId = null, string? referenceNumber = null, string? notes = null)
        {
            if (newQuantity < 0)
                throw new InvalidOperationException("لا يمكن أن يكون رصيد المستودع سالباً.");

            var existing = await _unitOfWork.warehouseFromProduct.GetByIdAsync(id);
            if (existing == null) return;

            decimal beforeQty = existing.StockQuantity;
            if (beforeQty == newQuantity) return;

            var now = DateTime.Now;
            existing.StockQuantity = newQuantity;
            existing.UpdatedAt = now;
            _unitOfWork.warehouseFromProduct.UpdateWarehouseFromProducts(existing);

            await _unitOfWork.StockMovements.AddAsync(new StockMovement
            {
                WarehouseFromProductId = id,
                WarehouseId = existing.WarehouseId,
                MovementType = StockMovementType.Adjustment,
                Quantity = newQuantity - beforeQty,
                BalanceBefore = beforeQty,
                BalanceAfter = newQuantity,
                UnitCost = existing.Price,
                TotalCost = existing.Price * Math.Abs(newQuantity - beforeQty),
                MovementDate = now,
                CreatedAt = now,
                UserId = userId,
                ReferenceNumber = referenceNumber ?? $"ADJ-{id}-{now:yyyyMMddHHmmss}",
                Notes = notes ?? "تعديل رصيد يدوي / تسوية جردية"
            });

            await _unitOfWork.SaveChangesAsync();
        }

        // 3. حذف مادة من المستودع باستعلامات SQL مباشرة فائقة السرعة بدون حلقات foreach
        public async Task DeleteProductFromWarehouse(WarehouseFromProduct warehouseFromProduct)
        {
            var context = _unitOfWork.GetContext();
            var productId = warehouseFromProduct.Id;

            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                await context.PurchaseItems
                    .Where(pi => pi.ProductId == productId)
                    .ExecuteUpdateAsync(s => s.SetProperty(pi => pi.ProductId, (int?)null));

                await context.warehouseFromProducts
                    .Where(wp => wp.Id == productId)
                    .ExecuteDeleteAsync();

                await transaction.CommitAsync();
                _unitOfWork.ClearEntityTracking();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<AnimalBatch> CreateAnimalBatchAsync(AnimalBatch batch)
        {
            return await ServiceLocator.AnimalBatchService.CreateBatchAsync(batch);
        }

        public async Task<IEnumerable<AnimalBatch>> GetAllBatchesAsync()
        {
            return await ServiceLocator.AnimalBatchService.GetAllBatchesAsync();
        }

        public async Task<IEnumerable<AnimalBatch>> GetFullAnimalsAsync()
        {
            return await ServiceLocator.AnimalBatchService.GetFullAnimalsOnlyAsync();
        }


        public IEnumerable<WarehouseFromProduct> GetWarehouseFromProductAll()
        {
            var list = _unitOfWork.warehouseFromProduct.GetWarehouseFromProductsAllProducts().ToList();
            bool anyFixed = false;
            var context = _unitOfWork.GetContext();
            foreach (var item in list)
            {
                if (item.Name != null && item.Name.Contains("لحم") &&
                    (item.Name.Contains("نفر") || item.Name.Contains("جزء") || item.Name.Contains("ثمن")) &&
                    (item.PortionUnit == Recipe.PortionUnit.Eighth || item.PortionUnit == null))
                {
                    item.PortionUnit = Recipe.PortionUnit.Portion;
                    if (item.Name.Contains("(ثمن/جزء)"))
                        item.Name = item.Name.Replace("(ثمن/جزء)", "(نفر/جزء)");

                    var tracked = context.warehouseFromProducts.FirstOrDefault(w => w.Id == item.Id);
                    if (tracked != null)
                    {
                        tracked.PortionUnit = Recipe.PortionUnit.Portion;
                        if (tracked.Name != null && tracked.Name.Contains("(ثمن/جزء)"))
                            tracked.Name = tracked.Name.Replace("(ثمن/جزء)", "(نفر/جزء)");
                        anyFixed = true;
                    }
                }
            }
            if (anyFixed)
            {
                try { context.SaveChanges(); } catch { }
            }
            return list;
        }

        // 4. حذف المستودع بسرعة فائقة عبر AnyAsync و ExecuteUpdateAsync
        public async Task<int> DeleteWarehouseAsync(Warehouse warehouse)
        {
            var context = _unitOfWork.GetContext();

            // فحص وجود مواد باستعلام خفيف جداً (SELECT 1 ... LIMIT 1) بدلاً من سحب كل المواد للرام!
            bool hasProducts = await context.warehouseFromProducts
                .AsNoTracking()
                .AnyAsync(wp => wp.WarehouseId == warehouse.Id);

            if (hasProducts)
                return -1; // Indicates warehouse has products and cannot be deleted

            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                // فك ارتباط المشتريات في SQLite مباشرة بدون سحبها للذاكرة
                await context.Purchases
                    .Where(p => p.WarehouseId == warehouse.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.WarehouseId, (int?)null));

                // حذف المستودع مباشرة
                int result = await context.Warehouses
                    .Where(w => w.Id == warehouse.Id)
                    .ExecuteDeleteAsync();

                await transaction.CommitAsync();
                _unitOfWork.ClearEntityTracking();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // 5. تحويل المخزون بدون سحب جدول المستودعات بالكامل للرام
        public async Task<ServiceResult> TransferStockAsync(
            int sourceWarehouseId,
            int destinationWarehouseId,
            int warehouseProductId,
            decimal quantity,
            string? notes = null,
            int? userId = null)
        {
            if (sourceWarehouseId <= 0 || destinationWarehouseId <= 0)
                return ServiceResult.Fail("يرجى تحديد المستودع المصدر والمستودع الهدف بشكل صحيح.");

            if (sourceWarehouseId == destinationWarehouseId)
                return ServiceResult.Fail("لا يمكن التحويل إلى نفس المستودع.");

            if (quantity <= 0)
                return ServiceResult.Fail("يجب أن تكون كمية التحويل أكبر من الصفر.");

            var context = _unitOfWork.GetContext();

            // جلب المستودعين في استعلام واحد خفيف بدون Tracking
            var warehouses = await context.Warehouses
                .AsNoTracking()
                .Where(w => w.Id == sourceWarehouseId || w.Id == destinationWarehouseId)
                .ToDictionaryAsync(w => w.Id, w => w.Name);

            if (!warehouses.TryGetValue(sourceWarehouseId, out var sourceWhName) ||
                !warehouses.TryGetValue(destinationWarehouseId, out var destWhName))
            {
                return ServiceResult.Fail("أحد المستودعات المحددة غير موجود.");
            }

            var sourceProduct = await _unitOfWork.warehouseFromProduct.GetFindByIdAsync(warehouseProductId);
            if (sourceProduct == null || sourceProduct.WarehouseId != sourceWarehouseId)
                return ServiceResult.Fail("المادة المحددة غير موجودة في المستودع المصدر.");

            if (sourceProduct.StockQuantity < quantity)
                return ServiceResult.Fail($"الكمية المطلوبة ({quantity:N0}) تتجاوز الرصيد المتوفر في {sourceWhName} وهو ({sourceProduct.StockQuantity:N0}).");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var now = DateTime.Now;

                // 1. خصم من المستودع المصدر
                decimal sourceBefore = sourceProduct.StockQuantity;
                sourceProduct.StockQuantity -= quantity;
                decimal sourceAfter = sourceProduct.StockQuantity;
                sourceProduct.UpdatedAt = now;
                _unitOfWork.warehouseFromProduct.UpdateWarehouseFromProducts(sourceProduct);

                // 2. البحث عن المادة المطابقة في المستودع الهدف مباشرة داخل SQLite!
                var trimmedSourceName = (sourceProduct.Name ?? string.Empty).Trim().ToLower();
                var destProduct = await context.warehouseFromProducts
                    .FirstOrDefaultAsync(p =>
                        p.WarehouseId == destinationWarehouseId &&
                        p.PortionUnit == sourceProduct.PortionUnit &&
                        p.Name != null &&
                        p.Name.Trim().ToLower() == trimmedSourceName);

                decimal destBefore = destProduct?.StockQuantity ?? 0m;
                if (destProduct != null)
                {
                    destProduct.StockQuantity += quantity;
                    destProduct.UpdatedAt = now;
                    _unitOfWork.warehouseFromProduct.UpdateWarehouseFromProducts(destProduct);
                }
                else
                {
                    destProduct = new WarehouseFromProduct
                    {
                        Name = sourceProduct.Name?.Trim(),
                        WarehouseId = destinationWarehouseId,
                        Price = sourceProduct.Price,
                        StockQuantity = quantity,
                        PortionUnit = sourceProduct.PortionUnit,
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    await _unitOfWork.warehouseFromProduct.AddAsync(destProduct);
                }

                // 3. تسجيل حركات المخزون
                string transferRef = $"TRF-{now:yyyyMMddHHmmss}-{sourceProduct.Id}";

                await _unitOfWork.StockMovements.AddAsync(new StockMovement
                {
                    WarehouseFromProductId = sourceProduct.Id,
                    WarehouseId = sourceWarehouseId,
                    MovementType = StockMovementType.TransferOut,
                    Quantity = -quantity,
                    BalanceBefore = sourceBefore,
                    BalanceAfter = sourceAfter,
                    UnitCost = sourceProduct.Price,
                    TotalCost = quantity * sourceProduct.Price,
                    UserId = userId,
                    ReferenceNumber = transferRef,
                    Notes = $"تحويل صادر إلى مستودع {destWhName}. {notes ?? ""}".Trim(),
                    MovementDate = now,
                    CreatedAt = now
                });

                var destMovement = new StockMovement
                {
                    WarehouseId = destinationWarehouseId,
                    MovementType = StockMovementType.TransferIn,
                    Quantity = quantity,
                    BalanceBefore = destBefore,
                    BalanceAfter = destProduct.StockQuantity,
                    UnitCost = destProduct.Price,
                    TotalCost = quantity * destProduct.Price,
                    UserId = userId,
                    ReferenceNumber = transferRef,
                    Notes = $"تحويل وارد من مستودع {sourceWhName}. {notes ?? ""}".Trim(),
                    MovementDate = now,
                    CreatedAt = now
                };

                if (destProduct.Id > 0)
                {
                    destMovement.WarehouseFromProductId = destProduct.Id;
                }
                else
                {
                    destMovement.WarehouseFromProduct = destProduct;
                }

                await _unitOfWork.StockMovements.AddAsync(destMovement);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                AppLogger.LogInfo($"[STOCK TRANSFER] Transferred {quantity} {sourceProduct.PortionUnit} of '{sourceProduct.Name}' from '{sourceWhName}' to '{destWhName}'.");
                ServiceLocator.NotifyInventoryChanged();

                return ServiceResult.Ok($"تم تحويل {quantity:N0} من '{sourceProduct.Name}' من {sourceWhName} إلى {destWhName} بنجاح.");
            }
            catch (Exception ex)
            {
                if (_unitOfWork.HasActiveTransaction)
                    await _unitOfWork.RollbackTransactionAsync();
                AppLogger.LogError($"Error transferring stock for product #{warehouseProductId}", ex);
                var fullErr = ex.InnerException != null ? $"{ex.Message} --> {ex.InnerException.Message}" : ex.Message;
                return ServiceResult.Fail($"حدث خطأ أثناء تحويل المخزون: {fullErr}");
            }
        }
    }
}
