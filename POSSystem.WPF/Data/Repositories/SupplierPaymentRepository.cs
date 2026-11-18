using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Models;
using POSSystem.WPF.Data.Repositories.Interfase;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace POSSystem.WPF.Data.Repositories
{
    public class SupplierPaymentRepository : Repository<SupplierPayment>, ISupplierPaymentRepository
    {
        public SupplierPaymentRepository(POSDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<SupplierPayment>> GetPaymentsBySupplierAsync(int supplierId)
        {
            return await _context.SupplierPayments
                .AsNoTracking()
                .Where(p => p.SupplierId == supplierId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }
    }
}
