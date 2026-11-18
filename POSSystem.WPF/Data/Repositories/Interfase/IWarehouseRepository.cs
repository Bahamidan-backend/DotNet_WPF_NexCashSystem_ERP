
namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface IWarehouseRepository : IRepository<Warehouse>
    {
        Task<IEnumerable<Warehouse>> GetActiveWarehousesAsync();
        Task<IEnumerable<Warehouse>> GetWarehousesWithLowStockAsync();
    }
}
