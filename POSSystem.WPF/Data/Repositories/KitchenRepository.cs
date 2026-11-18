using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Data.Repositories.Interfase;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Data.Repositories
{
    public class KitchenRepository : Repository<Kitchen>, IKitchenRepository
    {
        public KitchenRepository(POSDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Kitchen>> GetActiveKitchensAsync()
        {
            return await _context.Kitchens
                .AsNoTracking()
                .Where(k => k.IsActive)
                .OrderBy(k => k.Name)
                .ToListAsync();
        }

        public async Task<Kitchen?> GetKitchenByNameAsync(string name)
        {
            return await _context.Kitchens
                .AsNoTracking()
                .FirstOrDefaultAsync(k => EF.Functions.Like(k.Name, name));
        }
    }
}
