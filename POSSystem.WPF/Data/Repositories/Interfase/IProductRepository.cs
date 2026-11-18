
namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<PagedResult<Product>> GetProductsPagedAsync(int pageNumber, int pageSize);
        Task<Product> GetProductsByIDForRecipeAsync(int id);

        Task<IEnumerable<Product>> GetActiveProductsAsync();
 
        Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);
        Task<PagedResult<Product>> SearchProductsPagedAsync(string searchTerm, int pageNumber, int pageSize);
        Task UpdateStockAsync(int productId, int newStock);
        Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold = 10);
        public Task<int> GetTotalProductForHederAsync();

        Task ApplyBulkDiscountAsync(decimal percentage);
        Task RevertBulkDiscountAsync();
    }
}
