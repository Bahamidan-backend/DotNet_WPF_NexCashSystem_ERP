
namespace POSSystem.WPF.Business.Services.interfase
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<IEnumerable<Product>> GetActiveProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);
        Task<Product> CreateProductAsync(Product product);
        Task<Product> UpdateProductAsync(Product product);
        Task DeleteProductAsync(int productId);
        Task UpdateStockAsync(int productId, int newStock);
        Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold = 10);
        Task<bool> ValidateProductAsync(Product product);


        Task<int> UpdateProduct(Product product);
        Task<int> DeleteProduct(Product product);
        Task ApplyBulkDiscountAsync(decimal percentage);
        Task RevertBulkDiscountAsync();

        Task<ServiceResult<int>> RefillAllOutOfStockAsync();
        Task<bool> DeductStockAsync(int productId, string size, decimal quantity);
        Task UpdateProductRecipeStock(POSSystem.WPF.Models.DTOs.UpdateRecipeDto dto);
        Task<Product?> IncreaseProductQuantityAsync(int productId, decimal addedQuantity, string note);
    }
}
