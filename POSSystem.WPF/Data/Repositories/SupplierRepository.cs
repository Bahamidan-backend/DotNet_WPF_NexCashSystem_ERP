using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Models;
using POSSystem.WPF.Data.Repositories.Interfase;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace POSSystem.WPF.Data.Repositories
{
    public class SupplierRepository : Repository<Supplier>, ISupplierRepository
    {
        public SupplierRepository(POSDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Supplier>> GetSuppliersWithHistoryAsync()
        {
            return await _context.Suppliers
                .AsNoTracking()
                .Include(s => s.Purchases)
                .Include(s => s.SupplierPayments)
                .AsSplitQuery() // يمنع انفجار الصفوف الكارثي (Cartesian Explosion) في SQLite!
                .ToListAsync();
        }

        public async Task<Supplier?> GetSupplierWithHistoryAsync(int id)
        {
            return await _context.Suppliers
                .AsNoTracking()
                .Include(s => s.Purchases)
                .Include(s => s.SupplierPayments)
                .AsSplitQuery()
                .FirstOrDefaultAsync(s => s.Id == id);
        }
    }
}
