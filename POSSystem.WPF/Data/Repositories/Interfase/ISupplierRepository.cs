using POSSystem.WPF.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface ISupplierRepository : IRepository<Supplier>
    {
        Task<IEnumerable<Supplier>> GetSuppliersWithHistoryAsync();
        Task<Supplier?> GetSupplierWithHistoryAsync(int id);
    }
}
