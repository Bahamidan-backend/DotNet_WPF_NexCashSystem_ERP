
namespace POSSystem.WPF.Data.Repositories
{
    public class AddonRepository : Repository<Addon>, IAddonRepository
    {
        public AddonRepository(POSDbContext context) : base(context)
        {
        }

        private IQueryable<Addon> GetBaseQuery(bool asNoTracking = true)
        {
            var query = asNoTracking ? _dbSet.AsNoTracking() : _dbSet;
            // Removed Include(a => a.OrderItems) to prevent pulling historical sales for every addon!
            return query.Include(a => a.Category);
        }

        public async Task<IEnumerable<Addon>> GetAddonsByCategoryAsync(int categoryId)
        {
            return await GetBaseQuery()
                .Where(a => a.CategoryId == categoryId && a.IsActive)
                .OrderBy(a => a.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Addon>> GetActiveAddonsAsync()
        {
            return await GetBaseQuery()
                .Where(a => a.IsActive)
                .OrderBy(a => a.Category.Name)
                .ThenBy(a => a.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Addon>> GetLowStockAddonsAsync()
        {
            return await GetBaseQuery()
                .Where(a => a.IsActive && a.Quantity <= 5)
                .OrderBy(a => (double)a.Quantity)
                .ToListAsync();
        }

        public async Task<Addon?> GetAddonByNameAsync(string name)
        {
            return await _context.Addons
                .AsNoTracking()
                .FirstOrDefaultAsync(a => EF.Functions.Like(a.Name, name));
        }

        public async Task<bool> AddonNameExistsAsync(string name, int? excludeId = null)
        {
            var query = _context.Addons.AsNoTracking().Where(a => EF.Functions.Like(a.Name, name));
            
            if (excludeId.HasValue)
            {
                query = query.Where(a => a.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task UpdateAddonQuantityAsync(int addonId, decimal newQuantity)
        {
            // تحديث مباشر في SQLite بدون سحب الكائن إلى الذاكرة
            await _dbSet
                .Where(a => a.Id == addonId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.Quantity, newQuantity)
                    .SetProperty(a => a.UpdatedAt, DateTime.Now));
        }

        public override async Task<Addon?> GetByIdAsync(int id)
        {
            // جلب الإضافة مع القسم في استعلام واحد فقط بدلاً من استعلامين
            return await _dbSet
                .Include(a => a.Category)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public  async Task<List<Addon?>> GetByIdsAsync(List<int> id)
        {
            return await _context.Addons
          .AsNoTracking()
          .Include(a => a.Category)
          .Where(a => id.Contains(a.Id))
          .ToListAsync();
        }
        public override async Task<IEnumerable<Addon>> GetAllAsync()
        {
            return await GetBaseQuery()
                .OrderBy(a => a.Category.Name)
                .ThenBy(a => a.Name)
                .ToListAsync();
        }



       


    }
}
