using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Business.Services.interfase
{
    public interface IStockWasteService
    {
        Task<StockWaste> RecordWasteAsync(
            int warehouseFromProductId,
            decimal quantity,
            WasteReason reason,
            int? userId = null,
            string? notes = null);

        Task<IEnumerable<StockWaste>> GetWasteByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<StockWaste>> GetWasteByWarehouseAsync(int warehouseId, DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalWasteCostAsync(DateTime startDate, DateTime endDate);
    }
}

