
namespace POSSystem.WPF.Business.Services.interfase
{
    public interface IAddonService
    {
        Task<IEnumerable<Addon>> GetAllAddonsAsync();
        Task<IEnumerable<Addon>> GetActiveAddonsAsync();
        Task<IEnumerable<Addon>> GetAddonsByCategoryAsync(int categoryId);
        Task<IEnumerable<Addon>> GetLowStockAddonsAsync();
        Task<Addon?> GetAddonByIdAsync(int id);
        Task<Addon?> GetAddonByNameAsync(string name);
        Task<Addon> CreateAddonAsync(Addon addon);
        Task<Addon> UpdateAddonAsync(int id,Addon addon);
        Task DeleteAddonAsync(int addonId);
        Task UpdateAddonQuantityAsync(int addonId, decimal newQuantity);
        Task<bool> AddonNameExistsAsync(string name, int? excludeId = null);
        Task<bool> ValidateAddonDataAsync(Addon addon);
        Task FilterAddons(ObservableCollection<Addon> Addons, Category? SelectedCategory,  bool ShowLowStockOnly);
        //Task LoadData(ObservableCollection<Category> Categories, ObservableCollection<Addon> Addons, OrderItem orderitem = null);
        Task RefreshCategories(ObservableCollection<Category> Categories);
        Task<IEnumerable<Addon>> GetAddonsForOrderAsync(OrderItem orderItem);
        Task<List<Addon>?> GetAddonByIdsAsync(List<int> id);
        Task<POSSystem.WPF.Models.AddonStatistics> GetAddonStatisticsAsync();
        Task<int> RefillAllOutOfStockAsync();
    }
}
