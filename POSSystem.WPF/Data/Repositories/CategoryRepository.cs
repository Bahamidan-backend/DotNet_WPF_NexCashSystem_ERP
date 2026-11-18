

using Microsoft.EntityFrameworkCore;

namespace POSSystem.WPF.Data.Repositories
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        public CategoryRepository(POSDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Category>> GetActiveCategoriesAsync()
        {
            return await _context.Categories
                .AsNoTracking()
                .Include(x => x.Kitchen)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Category?> GetCategoryByNameAsync(string name)
        {
            return await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => EF.Functions.Like(c.Name, name));
        }

        public async Task<bool> CategoryNameExistsAsync(string name, int? excludeId = null)
        {
            var query = _context.Categories.AsNoTracking().Where(c => EF.Functions.Like(c.Name, name));
            
            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }
    }
}
