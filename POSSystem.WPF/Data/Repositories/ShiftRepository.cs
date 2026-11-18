using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Data.Repositories.Interfase;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Data.Repositories
{
    public class ShiftRepository : Repository<Shift>, IShiftRepository
    {
        public ShiftRepository(POSDbContext context) : base(context)
        {
        }

        public async Task<Shift?> GetActiveShiftByUserIdAsync(int userId)
        {
            // جلب بيانات الوردية والمستخدم فقط بدون سحب مئات حركات الصندوق في كل استدعاء!
            return await _dbSet
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);
        }

        public async Task<IEnumerable<Shift>> GetShiftsByUserIdAsync(int userId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Shift>> GetShiftsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(s => s.StartTime >= startDate && s.StartTime <= endDate)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<Shift?> GetShiftByShiftNumberAsync(string shiftNumber)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ShiftNumber == shiftNumber);
        }

        public async Task<string> GenerateShiftNumberAsync()
        {
            var today = DateTime.Now;
            var prefix = $"SHIFT-{today:yyyyMMdd}-";

            var existingNumbers = await _dbSet
                .AsNoTracking()
                .Where(s => s.ShiftNumber != null && s.ShiftNumber.StartsWith(prefix))
                .Select(s => s.ShiftNumber)
                .ToListAsync();

            var existingSet = new HashSet<string>(existingNumbers, StringComparer.OrdinalIgnoreCase);
            int maxSeq = 0;
            foreach (var num in existingNumbers)
            {
                if (num.Length > prefix.Length)
                {
                    var suffix = num.Substring(prefix.Length);
                    var dashIdx = suffix.IndexOf('-');
                    if (dashIdx > 0) suffix = suffix.Substring(0, dashIdx);
                    if (int.TryParse(suffix, out var lastNumber) && lastNumber > maxSeq)
                        maxSeq = lastNumber;
                }
            }

            int nextNumber = maxSeq + 1;
            string candidate = $"{prefix}{nextNumber:D4}";
            while (existingSet.Contains(candidate) ||
                   await _dbSet.AsNoTracking().AnyAsync(s => s.ShiftNumber == candidate))
            {
                nextNumber++;
                candidate = $"{prefix}{nextNumber:D4}";
            }

            return candidate;
        }
    }
}

