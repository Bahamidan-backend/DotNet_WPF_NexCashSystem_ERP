using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;
using POSSystem.WPF.Services.Logging;

namespace POSSystem.WPF.Business.Services
{
    public class StockWasteService : IStockWasteService
    {
        private readonly IUnitOfWork _unitOfWork;

        public StockWasteService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<StockWaste> RecordWasteAsync(
            int warehouseFromProductId,
            decimal quantity,
            WasteReason reason,
            int? userId = null,
            string? notes = null)
        {
            if (warehouseFromProductId <= 0)
                throw new ArgumentException("معرف المادة غير صالح.", nameof(warehouseFromProductId));

            if (quantity <= 0)
                throw new ArgumentException("يجب أن تكون كمية الهدر أكبر من الصفر.", nameof(quantity));

            var whProduct = await _unitOfWork.warehouseFromProduct.GetByIdAsync(warehouseFromProductId);
            if (whProduct == null)
                throw new InvalidOperationException($"المادة برقم {warehouseFromProductId} غير موجودة في المستودع.");

            // التحقق الصارم من توفر الرصيد: يمنع تسجيل هدر أكبر من الرصيد الفعلي
            if (whProduct.StockQuantity < quantity)
            {
                var unitName = whProduct.PortionUnit.HasValue
                    ? PortionUnitHelper.GetDisplayName(whProduct.PortionUnit.Value)
                    : string.Empty;

                throw new InvalidOperationException(
                    $"لا يمكن تسجيل هدر أكبر من الرصيد المتوفر في المستودع للمادة '{whProduct.Name}'. " +
                    $"الرصيد المتاح: {whProduct.StockQuantity:N0} {unitName}، والكمية المراد إتلافها: {quantity:N0} {unitName}.".Trim());
            }

            var now = DateTime.Now;
            var beforeQty = whProduct.StockQuantity;
            var unitCost = whProduct.Price;
            var totalCost = Math.Round(quantity * unitCost, 2);

            StockWaste waste = null!;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // الخصم الدقيق دون استخدام Math.Max للتصفير القسري
                whProduct.StockQuantity -= quantity;
                var afterQty = whProduct.StockQuantity;
                whProduct.UpdatedAt = now;
                _unitOfWork.warehouseFromProduct.Update(whProduct);

                // 1. Create StockWaste record
                waste = new StockWaste
                {
                    WarehouseFromProductId = warehouseFromProductId,
                    WarehouseId = whProduct.WarehouseId,
                    Quantity = quantity,
                    PortionUnit = whProduct.PortionUnit,
                    UnitCost = unitCost,
                    TotalCost = totalCost,
                    Reason = reason,
                    WasteDate = now,
                    ReportedByUserId = userId,
                    Notes = notes,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _unitOfWork.StockWastes.AddAsync(waste);

                // 2. Audit Trail Movement
                var movement = new StockMovement
                {
                    WarehouseFromProductId = warehouseFromProductId,
                    WarehouseId = whProduct.WarehouseId,
                    MovementType = StockMovementType.Waste,
                    Quantity = -quantity,
                    BalanceBefore = beforeQty,
                    BalanceAfter = afterQty,
                    UnitCost = unitCost,
                    TotalCost = totalCost,
                    MovementDate = now,
                    CreatedAt = now,
                    UserId = userId,
                    ReferenceNumber = $"WST-{now:yyyyMMddHHmmss}-{warehouseFromProductId}",
                    Notes = $"تسجيل هدر/تالف: {WasteReasonHelper.GetArabicText(reason)}" +
                            (!string.IsNullOrWhiteSpace(notes) ? $" | {notes}" : "")
                };

                await _unitOfWork.StockMovements.AddAsync(movement);

                await _unitOfWork.SaveChangesAsync();
            });

            AppLogger.LogInfo($"[STOCK WASTE] Recorded waste of {quantity} {whProduct.PortionUnit} of '{whProduct.Name}'. Cost: {totalCost:N0} SAR. Reason: {reason}");

            return waste;
        }

        public async Task<IEnumerable<StockWaste>> GetWasteByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1);
            return await _unitOfWork.GetContext().StockWastes
                .AsNoTracking()
                .Include(w => w.WarehouseFromProduct)
                .Include(w => w.Warehouse)
                .Include(w => w.ReportedByUser)
                .Where(w => w.WasteDate >= start && w.WasteDate < end)
                .OrderByDescending(w => w.WasteDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<StockWaste>> GetWasteByWarehouseAsync(int warehouseId, DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1);
            return await _unitOfWork.GetContext().StockWastes
                .AsNoTracking()
                .Include(w => w.WarehouseFromProduct)
                .Include(w => w.ReportedByUser)
                .Where(w => w.WarehouseId == warehouseId && w.WasteDate >= start && w.WasteDate < end)
                .OrderByDescending(w => w.WasteDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalWasteCostAsync(DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1);
            var costs = await _unitOfWork.GetContext().StockWastes
                .AsNoTracking()
                .Where(w => w.WasteDate >= start && w.WasteDate < end)
                .Select(w => w.TotalCost)
                .ToListAsync();
            return costs.Sum();
        }
    }
}

