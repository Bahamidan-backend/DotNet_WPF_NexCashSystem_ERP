using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Data.Repositories.Interfase;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Data.Repositories
{
    // NOTE: Filename has a typo ("Repostry") kept for backward compatibility with existing references.
    public class WarehouseFromProductRepostry : Repository<WarehouseFromProduct>, IWarehouseFromProductRepostry
    {
        public WarehouseFromProductRepostry(POSDbContext context) : base(context)
        {
        }

        // FIX: Was synchronous — converted to async to avoid blocking the UI thread.
        public async Task<IEnumerable<WarehouseFromProduct>> GetWarehouseFromProductsAllProductsAsync()
        {
            return await _context.warehouseFromProducts
                .Include(x => x.Warehouse)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Synchronous version kept for backward compatibility with callers that cannot be awaited.
        /// Prefer the async version wherever possible.
        /// </summary>
        public IEnumerable<WarehouseFromProduct> GetWarehouseFromProductsAllProducts()
        {
            return _context.warehouseFromProducts
                .Include(x => x.Warehouse)
                .AsNoTracking()
                .ToList();
        }

        public void UpdateWarehouseFromProducts(WarehouseFromProduct fromProduct)
        {
            Update(fromProduct);
        }

        // FIX: Atomic direct update in database without loading full entity into memory
        public async Task UpdateQuntityWarehouseFromProductAsync(int id, decimal quantity)
        {
            await _context.warehouseFromProducts
                .Where(x => x.Id == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.StockQuantity, quantity)
                    .SetProperty(x => x.UpdatedAt, DateTime.Now));
        }

        /// <summary>Sync overload for legacy callers.</summary>
        public void UpdateQuntityWarehouseFromProduct(int id, decimal WarehousefromP)
        {
            _context.warehouseFromProducts
                .Where(x => x.Id == id)
                .ExecuteUpdate(s => s
                    .SetProperty(x => x.StockQuantity, WarehousefromP)
                    .SetProperty(x => x.UpdatedAt, DateTime.Now));
        }

        // FIX: Added AsNoTracking for read query
        public async Task<WarehouseFromProduct?> GetFindByIdAsync(int id)
        {
            return await _context.warehouseFromProducts
                .AsNoTracking()
                .Include(x => x.Warehouse)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        /// <summary>Sync overload for legacy callers.</summary>
        public WarehouseFromProduct GetFindById(int id)
        {
            return _context.warehouseFromProducts
                .AsNoTracking()
                .Include(x => x.Warehouse)
                .FirstOrDefault(x => x.Id == id);
        }

        // FIX: Removed unnecessary Include(x => x.Warehouse) from Count query
        public async Task<int> GetCountWarehouseFromProductAsync()
        {
            return await _context.warehouseFromProducts.CountAsync();
        }

        /// <summary>Sync overload for legacy callers.</summary>
        public int GetCountWarehouseFromProduct()
        {
            return _context.warehouseFromProducts.Count();
        }

        public int GetCountforProductInsaedWarehouseFromProduct()
        {
            return _context.warehouseFromProducts.Count(x => x.WarehouseId != null);
        }

        // FIX: Projects numeric product calculation directly from database, sums in memory to handle SQLite decimal aggregate
        public async Task<decimal> GetWarehouseFromProductTotalValueAsync()
        {
            var values = await _context.warehouseFromProducts
                .AsNoTracking()
                .Select(x => x.StockQuantity * x.Price)
                .ToListAsync();
            return values.Sum();
        }

        /// <summary>Sync overload for legacy callers.</summary>
        public string GetWarehouseFromProductTotalValue()
        {
            var total = _context.warehouseFromProducts
                .AsNoTracking()
                .Select(x => (decimal?)(x.StockQuantity * x.Price))
                .ToList()
                .Sum() ?? 0m;
            return total.ToString("N0");
        }
    }
}
