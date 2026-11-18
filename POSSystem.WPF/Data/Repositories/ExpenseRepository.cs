using POSSystem.WPF.Data.Repositories.Interfase;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Data.Repositories
{
    public class ExpenseRepository : Repository<Expense>, IExpenseRepository
    {
        public ExpenseRepository(POSDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Expense>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            // Normalize dates to include the full end date (inclusive)
            var start = startDate.Date;
            var endExclusive = endDate.Date.AddDays(1);

            return await _dbSet
                .AsNoTracking()
                .Where(e => e.ExpenseDate >= start && e.ExpenseDate < endExclusive)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Expense>> GetByCategoryAsync(string category)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(e => e.Category == category)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Expense>> GetByTypeAsync(ExpenseType type)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(e => e.Type == type)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var endExclusive = endDate.Date.AddDays(1);

            var amounts = await _dbSet
                .AsNoTracking()
                .Where(e => e.ExpenseDate >= start && e.ExpenseDate < endExclusive)
                .Select(e => e.Amount)
                .ToListAsync();
            return amounts.Sum();
        }

        public async Task<decimal> GetTotalByCategoryAsync(string category, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _dbSet.Where(e => e.Category == category);
            
            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;
                query = query.Where(e => e.ExpenseDate >= start);
            }
            
            if (endDate.HasValue)
            {
                var endExclusive = endDate.Value.Date.AddDays(1);
                query = query.Where(e => e.ExpenseDate < endExclusive);
            }

            var amounts = await query
                .AsNoTracking()
                .Select(e => e.Amount)
                .ToListAsync();
            return amounts.Sum();
        }
    }
}

