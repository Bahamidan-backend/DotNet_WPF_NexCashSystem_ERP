
namespace POSSystem.WPF.Business.Services.interfase
{
    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetAllCategoriesAsync();
        Task<IEnumerable<Category>> GetActiveCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<Category?> GetCategoryByNameAsync(string name);
        Task<Category> CreateCategoryAsync(Category category);
        Task<Category> UpdateCategoryAsync(Category category);
        Task<int> DeleteCategoryAsync(Category category);
        Task<bool> CategoryNameExistsAsync(string name, int? excludeId = null);
        Task<bool> ValidateCategoryDataAsync(Category category);
    }
}
