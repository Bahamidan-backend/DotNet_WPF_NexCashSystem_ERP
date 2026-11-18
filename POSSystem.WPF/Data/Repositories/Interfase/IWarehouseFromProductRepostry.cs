
namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface IWarehouseFromProductRepostry : IRepository<WarehouseFromProduct>
    {
        // Sync (legacy) versions kept for backward compatibility
        IEnumerable<WarehouseFromProduct> GetWarehouseFromProductsAllProducts();
        void UpdateWarehouseFromProducts(WarehouseFromProduct fromProduct);
        void UpdateQuntityWarehouseFromProduct(int id, decimal WarehousefromP);
        int GetCountWarehouseFromProduct();
        string GetWarehouseFromProductTotalValue();
        int GetCountforProductInsaedWarehouseFromProduct();
        WarehouseFromProduct GetFindById(int id);

        // Async versions (preferred)
        Task<IEnumerable<WarehouseFromProduct>> GetWarehouseFromProductsAllProductsAsync();
        Task UpdateQuntityWarehouseFromProductAsync(int id, decimal quantity);
        Task<WarehouseFromProduct?> GetFindByIdAsync(int id);
        Task<int> GetCountWarehouseFromProductAsync();
        Task<decimal> GetWarehouseFromProductTotalValueAsync();
    }
}
