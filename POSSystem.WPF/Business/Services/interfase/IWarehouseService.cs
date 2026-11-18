
namespace POSSystem.WPF.Business.Services.interfase
{
    public interface IWarehouseService
    {
        Task<IEnumerable<Warehouse>> GetAllWarehousesAsync();
        Task<Warehouse?> GetWarehouseByIdAsync(int id);
        Task<Warehouse> CreateWarehouseAsync(Warehouse warehouse);
        Task<Warehouse> UpdateWarehouseAsync(Warehouse warehouse);
        Task<int> DeleteWarehouseAsync(Warehouse warehouse);
        Task UpdateQuntityWarehouseFromProduct(int id, decimal newQuantity, int? userId = null, string? referenceNumber = null, string? notes = null);

        Task<IEnumerable<Warehouse>> GetWarehousesWithLowStockAsync();
        Task<bool> AssignProductToWarehouseAsync(int productId, int warehouseId);
        Task<bool> RemoveProductFromWarehouseAsync(int productId);
        Task<WarehouseFromProduct> CreateProductFromWarehouse(WarehouseFromProduct WarehousefromProduct);
        IEnumerable<WarehouseFromProduct> GetWarehouseFromProductAll();

         Task UpdateProductFromWarehouse(WarehouseFromProduct WarehousefromProduct);
        Task DeleteProductFromWarehouse(WarehouseFromProduct WarehousefromProduct);
        Task<ServiceResult> TransferStockAsync(int sourceWarehouseId, int destinationWarehouseId, int warehouseProductId, decimal quantity, string? notes = null, int? userId = null);

        // Animal Batch methods
        Task<AnimalBatch> CreateAnimalBatchAsync(AnimalBatch batch);
        Task<IEnumerable<AnimalBatch>> GetAllBatchesAsync();
        Task<IEnumerable<AnimalBatch>> GetFullAnimalsAsync();

    }
}
