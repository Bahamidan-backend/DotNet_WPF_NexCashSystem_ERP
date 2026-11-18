using System.Collections.Generic;
using System.Threading.Tasks;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface IPurchaseReturnRepository : IRepository<PurchaseReturn>
    {
        Task UpdateAsync(PurchaseReturn purchaseReturn);
        Task DeleteAsync(int id);
        Task<string> GenerateReturnNumberAsync();
        Task<IEnumerable<PurchaseReturn>> SearchAsync(string searchTerm, DateTime? fromDate, DateTime? toDate);
    }
}
