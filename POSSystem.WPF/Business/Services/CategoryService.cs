

using Microsoft.VisualBasic;

namespace POSSystem.WPF.Business.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await _unitOfWork.Categories.GetAllAsync();
        }


         
        public async Task<IEnumerable<Category>> GetActiveCategoriesAsync()
        {
            return await _unitOfWork.Categories.GetActiveCategoriesAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _unitOfWork.Categories.GetByIdAsync(id);
        }

        public async Task<Category?> GetCategoryByNameAsync(string name)
        {
            return await _unitOfWork.Categories.GetCategoryByNameAsync(name);
        }

        public async Task<Category> CreateCategoryAsync(Category category)
        {
            if (!await ValidateCategoryDataAsync(category))
                throw new ArgumentException("Invalid category data");

            category.Name = category.Name.Trim();

            // Check if category name already exists
            if (await _unitOfWork.Categories.CategoryNameExistsAsync(category.Name))
                throw new InvalidOperationException("Category with this name already exists");
            
            category.CreatedAt = DateTime.Now;
            category.UpdatedAt = DateTime.Now;

            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();
            ServiceLocator.NotifyCategoriesChanged();
            ServiceLocator.NotifyProductsChanged();

            return category;
        }

        public async Task<Category> UpdateCategoryAsync(Category category)
        {
            if (!await ValidateCategoryDataAsync(category))
                throw new ArgumentException("Invalid category data");

            var existingCategory = await _unitOfWork.Categories.GetByIdAsync(category.Id);
            if (existingCategory == null)
                throw new ArgumentException("Category not found");

            category.Name = category.Name.Trim();

            // Check if category name already exists (excluding current category)
            if (await _unitOfWork.Categories.CategoryNameExistsAsync(category.Name, category.Id))
                throw new InvalidOperationException("Category with this name already exists");

            existingCategory.Name = category.Name;
            existingCategory.Description = category.Description;
            existingCategory.portionSize = category.portionSize;
            existingCategory.UpdatedAt = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();
            ServiceLocator.NotifyCategoriesChanged();
            ServiceLocator.NotifyProductsChanged();

            return existingCategory;
        }

      



        public async Task<int> DeleteCategoryAsync(Category category)
        {
            int deleted = await _unitOfWork.GetContext().Categories
                .Where(c => c.Id == category.Id)
                .ExecuteDeleteAsync();
            _unitOfWork.ClearEntityTracking();
            ServiceLocator.NotifyCategoriesChanged();
            ServiceLocator.NotifyProductsChanged();
            return deleted;
        }



        public async Task<bool> CategoryNameExistsAsync(string name, int? excludeId = null)
        {
            return await _unitOfWork.Categories.CategoryNameExistsAsync(name, excludeId);
        }

        public Task<bool> ValidateCategoryDataAsync(Category category)
        {
            if (category == null) return Task.FromResult(false);
            if (string.IsNullOrWhiteSpace(category.Name)) return Task.FromResult(false);

            return Task.FromResult(true);
        }

       
    }
}
