
namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<IEnumerable<Category>> GetActiveCategoriesAsync();
        Task<Category?> GetCategoryByNameAsync(string name);
        Task<bool> CategoryNameExistsAsync(string name, int? excludeId = null);
    }
}
