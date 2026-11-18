using POSSystem.WPF.Models;

namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface IPurchaseItemRepository : IRepository<PurchaseItem>
    {
        Task<IEnumerable<PurchaseItem>> GetByPurchaseIdAsync(int purchaseId);
        Task<IEnumerable<PurchaseItem>> GetByProductIdAsync(int productId);
         Task<IEnumerable<PurchaseItem>> GetAllPurchaseAsync();

    }
}

