using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Data.Repositories.Interfase;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Data.Repositories
{
    public class WarehouseRepository : Repository<Warehouse>, IWarehouseRepository
    {
        public WarehouseRepository(POSDbContext context) : base(context)
        {
        }

        // FIX: Added AsNoTracking for read query
        public override async Task<IEnumerable<Warehouse>> GetAllAsync()
        {
            return await _context.Warehouses
                .AsNoTracking()
                .Include(w => w.warehouseFromProducts)
                .OrderBy(w => w.Name)
                .ToListAsync();
        }

        // FIX: Replaced obsolete Products navigation with warehouseFromProducts
        public override async Task<Warehouse?> GetByIdAsync(int id)
        {
            return await _context.Warehouses
                .Include(w => w.warehouseFromProducts)
                .FirstOrDefaultAsync(w => w.Id == id && w.IsActive);
        }

        // FIX: Added AsNoTracking for read query
        public async Task<IEnumerable<Warehouse>> GetActiveWarehousesAsync()
        {
            return await _context.Warehouses
                .AsNoTracking()
                .Where(w => w.IsActive)
                .OrderBy(w => w.Name)
                .ToListAsync();
        }

        // FIX: Query warehouseFromProducts directly with AsNoTracking and filter
        public async Task<IEnumerable<Warehouse>> GetWarehousesWithLowStockAsync()
        {
            return await _context.Warehouses
                .AsNoTracking()
                .Include(w => w.warehouseFromProducts.Where(p => p.IsActive && p.StockQuantity <= 10))
                .Where(w => w.IsActive && w.warehouseFromProducts.Any(p => p.IsActive && p.StockQuantity <= 10))
                .ToListAsync();
        }
    }
}
