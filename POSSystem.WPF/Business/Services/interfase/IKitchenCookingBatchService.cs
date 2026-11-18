using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using POSSystem.WPF.Models;
using POSSystem.WPF.Models.DTOs;

namespace POSSystem.WPF.Business.Services.interfase
{
    public class LeftoverDisposalItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal QuantityToDispose { get; set; }
        public string? SizeName { get; set; }
    }

    public interface IKitchenCookingBatchService
    {
        Task<KitchenCookingBatch> CreateBatchAsync(CreateCookingBatchDto dto);
        Task<KitchenCookingBatch> SettleCookingSurplusAsync(int batchId, decimal surplusPortions, string reason, int? userId = null, string? userName = null);
        Task<int> DisposeLeftoverPosStockAsync(List<LeftoverDisposalItemDto> items, WasteReason reason, string disposalCategoryLabel, string? notes = null, int? userId = null, string? supervisorName = null);
        Task<IEnumerable<KitchenCookingBatch>> GetBatchesByDateAsync(DateTime fromDate, DateTime toDate);
        Task<IEnumerable<KitchenCookingBatch>> GetRecentSettleableBatchesAsync(int daysBack = 3);
        Task<KitchenCookingBatch?> GetBatchByIdAsync(int id);
        Task<IEnumerable<Product>> GetBatchCookableProductsAsync();
    }
}
