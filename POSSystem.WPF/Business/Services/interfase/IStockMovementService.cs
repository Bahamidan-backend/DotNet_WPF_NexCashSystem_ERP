using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Business.Services.interfase
{
    public interface IStockMovementService
    {
        Task<StockMovement> RecordMovementAsync(
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
            string? notes = null);

        Task<IEnumerable<StockMovement>> GetMovementsByProductAsync(int warehouseFromProductId, int limit = 100);
        Task<IEnumerable<StockMovement>> GetMovementsByWarehouseAsync(int warehouseId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<StockMovement>> GetMovementsByDateRangeAsync(DateTime startDate, DateTime endDate, StockMovementType? type = null);
    }
}

