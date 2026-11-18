using POSSystem.WPF.Data.Repositories.Interfase;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Data.Repositories
{
    public class PurchaseItemRepository : Repository<PurchaseItem>, IPurchaseItemRepository
    {
        public PurchaseItemRepository(POSDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<PurchaseItem>> GetByPurchaseIdAsync(int purchaseId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(pi => pi.PurchaseId == purchaseId)
                .Include(pi => pi.Product)
                .ToListAsync();
        }
        public async Task<IEnumerable<PurchaseItem>> GetAllPurchaseAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();

        }

        public async Task<IEnumerable<PurchaseItem>> GetByProductIdAsync(int productId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(pi => pi.ProductId == productId)
                .Include(pi => pi.Purchase)
                .OrderByDescending(pi => pi.Purchase!.PurchaseDate)
                .ToListAsync();
        }
    }
}

