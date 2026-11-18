
namespace POSSystem.WPF.Data.Repositories
{
    public class RecipesRepostries : Repository<Recipe>, IRecipesRepostries
    {
        public RecipesRepostries(POSDbContext context):base(context) 
        {
            
        }



        public async Task<IEnumerable<Recipe>> GetRecipesAllProducts()
        {
            // FIX: Was calling synchronous .ToList() inside an async method (sync-over-async anti-pattern).
            return await _context.Recipes
                .AsNoTracking()
                .Include(x => x.Product)
                .ToListAsync();
        }


    }
}
