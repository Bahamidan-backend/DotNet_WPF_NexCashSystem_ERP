using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Data.Repositories.Interfase;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Data.Repositories
{
    public class CashBoxTransactionRepository : Repository<CashBoxTransaction>, ICashBoxTransactionRepository
    {
        public CashBoxTransactionRepository(POSDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<CashBoxTransaction>> GetByShiftIdAsync(int shiftId)
        {
            // FIX: Added AsNoTracking for read-only list query.
            return await _dbSet
                .AsNoTracking()
                .Include(t => t.Shift)
                    .ThenInclude(s => s.User)
                .Include(t => t.User)
                .Where(t => t.ShiftId == shiftId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<CashBoxTransaction>> GetByUserIdAsync(int userId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalByShiftIdAsync(int shiftId)
        {
            var items = await _dbSet
                .AsNoTracking()
                .Where(t => t.ShiftId == shiftId)
                .Select(t => new { t.Type, t.Amount })
                .ToListAsync();
            return items.Sum(t => t.Type == CashBoxTransactionType.Income ? t.Amount : -t.Amount);
        }

        public async Task<decimal> GetTotalByTypeAsync(int shiftId, CashBoxTransactionType type)
        {
            var amounts = await _dbSet
                .AsNoTracking()
                .Where(t => t.ShiftId == shiftId && t.Type == type)
                .Select(t => t.Amount)
                .ToListAsync();
            return amounts.Sum();
        }

        public async Task<IEnumerable<CashBoxTransaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(t => t.Shift)
                    .ThenInclude(s => s.User)
                .Include(t => t.User)
                .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }
    }
}

