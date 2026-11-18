using POSSystem.WPF.Models;

namespace POSSystem.WPF.Business.Services
{
    public interface IAnimalBatchService
    {
        Task<IEnumerable<AnimalBatch>> GetAllBatchesAsync();
        Task<IEnumerable<AnimalBatch>> GetBatchesByTypeAsync(AnimalType type);
        Task<IEnumerable<AnimalBatch>> GetFullAnimalsOnlyAsync();
        Task<AnimalBatch> GetBatchByIdAsync(int id);
        Task<AnimalBatch> CreateBatchAsync(AnimalBatch batch);
        Task UpdateBatchComponentsAsync(BatchComponent component);
        Task<bool> ConsumeFromBatchAsync(int batchId, ComponentType componentType, decimal quantity);
        Task<bool> ConsumeFromAnyBatchAsync(ComponentType componentType, decimal quantity);
        Task<bool> RestoreToAnyBatchAsync(ComponentType componentType, decimal quantity);
        Task<bool> SellFullAnimalAsync(int batchId);
        Task<bool> BreakdownCarcassesFromInventoryAsync(int sourceWarehouseId, int warehouseFromProductId, int targetWarehouseId, int count, bool isWeightMode, int portionsCount, decimal meatWeight, decimal liverWeight, decimal backWeight, int categoryIdToUse, int? supplierId = null);
        Task<bool> AddPartialCarcassAsync(int targetWarehouseId, AnimalType animalType, decimal headCount, decimal liverQty, decimal backQty, decimal meatQty, bool isWeightMode, int? supplierId = null);
        Task<bool> DeleteBatchAsync(int batchId);
        Task<int> GetFullAnimalsCountAsync();
        Task<Dictionary<AnimalType, int>> GetFullAnimalsCountByTypeAsync();
    }
}
