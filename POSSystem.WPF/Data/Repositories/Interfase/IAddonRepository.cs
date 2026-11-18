
namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface IAddonRepository : IRepository<Addon>
    {
        Task<IEnumerable<Addon>> GetAddonsByCategoryAsync(int categoryId);
        Task<IEnumerable<Addon>> GetActiveAddonsAsync();
        Task<IEnumerable<Addon>> GetLowStockAddonsAsync();
        Task<Addon?> GetAddonByNameAsync(string name);
        Task<bool> AddonNameExistsAsync(string name, int? excludeId = null);
        Task UpdateAddonQuantityAsync(int addonId, decimal newQuantity);
        Task<List<Addon?>> GetByIdsAsync(List<int> id);

    }
}
