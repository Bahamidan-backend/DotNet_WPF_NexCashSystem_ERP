using System.Collections.Generic;
using System.Threading.Tasks;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface IPurchaseReturnItemRepository : IRepository<PurchaseReturnItem>
    {
        Task<IEnumerable<PurchaseReturnItem>> GetByReturnIdAsync(int returnId);
    }
}
