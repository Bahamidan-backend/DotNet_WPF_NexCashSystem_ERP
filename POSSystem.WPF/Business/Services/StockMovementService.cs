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
    public class StockMovementService : IStockMovementService
    {
        private readonly IUnitOfWork _unitOfWork;

        public StockMovementService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<StockMovement> RecordMovementAsync(
            int warehouseFromProductId,
            int? warehouseId,
            StockMovementType movementType,
            decimal quantity,
            decimal balanceBefore,
            decimal balanceAfter,
            decimal unitCost,
            string? referenceNumber = null,
            int? orderId = null,
            int? purchaseId = null,
            int? userId = null,
            string? notes = null)
        {
            var movement = new StockMovement
            {
                WarehouseFromProductId = warehouseFromProductId,
                WarehouseId = warehouseId,
                MovementType = movementType,
                Quantity = quantity,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                UnitCost = unitCost,
                TotalCost = Math.Abs(quantity) * unitCost,
                ReferenceNumber = referenceNumber,
                OrderId = orderId,
                PurchaseId = purchaseId,
                UserId = userId,
                Notes = notes,
                MovementDate = DateTime.Now,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.StockMovements.AddAsync(movement);
            await _unitOfWork.SaveChangesAsync();

            AppLogger.LogInfo($"StockMovement recorded: ProductId={warehouseFromProductId}, Type={movementType}, Qty={quantity}, Before={balanceBefore}, After={balanceAfter}");
            return movement;
        }

        public async Task<IEnumerable<StockMovement>> GetMovementsByProductAsync(int warehouseFromProductId, int limit = 100)
        {
            var context = _unitOfWork.GetContext();
            return await context.StockMovements.AsNoTracking()
                .Include(m => m.WarehouseFromProduct)
                .Include(m => m.Warehouse)
                .Where(m => m.WarehouseFromProductId == warehouseFromProductId)
                .OrderByDescending(m => m.MovementDate)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<StockMovement>> GetMovementsByWarehouseAsync(int warehouseId, DateTime startDate, DateTime endDate)
        {
            var context = _unitOfWork.GetContext();
            return await context.StockMovements.AsNoTracking()
                .Include(m => m.WarehouseFromProduct)
                .Include(m => m.Warehouse)
                .Where(m => m.WarehouseId == warehouseId && m.MovementDate >= startDate && m.MovementDate <= endDate)
                .OrderByDescending(m => m.MovementDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<StockMovement>> GetMovementsByDateRangeAsync(DateTime startDate, DateTime endDate, StockMovementType? type = null)
        {
            var context = _unitOfWork.GetContext();
            var query = context.StockMovements.AsNoTracking()
                .Include(m => m.WarehouseFromProduct)
                .Include(m => m.Warehouse)
                .Where(m => m.MovementDate >= startDate && m.MovementDate <= endDate);

            if (type.HasValue)
            {
                query = query.Where(m => m.MovementType == type.Value);
            }

            return await query.OrderByDescending(m => m.MovementDate).ToListAsync();
        }
    }
}

