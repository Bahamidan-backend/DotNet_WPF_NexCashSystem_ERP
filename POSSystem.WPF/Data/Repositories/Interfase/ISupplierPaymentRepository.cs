using POSSystem.WPF.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface ISupplierPaymentRepository : IRepository<SupplierPayment>
    {
        Task<IEnumerable<SupplierPayment>> GetPaymentsBySupplierAsync(int supplierId);
    }
}
