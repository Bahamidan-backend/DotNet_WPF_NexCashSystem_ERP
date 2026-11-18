using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Data;
using POSSystem.WPF.Models;
using POSSystem.WPF.Data.Repositories.Interfase;

namespace POSSystem.WPF.Data.Repositories
{
    public class PurchaseReturnItemRepository : Repository<PurchaseReturnItem>, IPurchaseReturnItemRepository
    {
        public PurchaseReturnItemRepository(POSDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<PurchaseReturnItem>> GetByReturnIdAsync(int returnId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(i => i.OriginalPurchaseItem)
                    .ThenInclude(oi => oi.Product)
                .Include(i => i.OriginalPurchaseItem)
                    .ThenInclude(oi => oi.Addon)
                .Where(i => i.PurchaseReturnId == returnId)
                .ToListAsync();
        }


    }
}
