using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace POSSystem.WPF.Data.Repositories
{
    public class ReturnOrderRepository : Repository<ReturnOrder>, IReturnOrderRepository
    {
        public ReturnOrderRepository(POSDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ReturnOrder>> GetAllReturnsWithDetailsAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Include(r => r.Cashier)
                .Include(r => r.OriginalOrder)
                .Include(r => r.ReturnOrderItems)
                    .ThenInclude(ri => ri.OriginalOrderItem)
                        .ThenInclude(oi => oi.Product)
                .AsSplitQuery()
                .OrderByDescending(r => r.ReturnDate)
                .ToListAsync();
        }

        public async Task<ReturnOrder?> GetReturnWithDetailsAsync(int returnId)
        {
            return await _dbSet
                .Include(r => r.Cashier)
                .Include(r => r.OriginalOrder)
                .Include(r => r.ReturnOrderItems)
                    .ThenInclude(ri => ri.OriginalOrderItem)
                        .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(r => r.Id == returnId);
        }

        public async Task<IEnumerable<ReturnOrder>> GetReturnsByOriginalOrderIdAsync(int originalOrderId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(r => r.ReturnOrderItems)
                .Where(r => r.OriginalOrderId == originalOrderId)
                .ToListAsync();
        }

        public async Task<string> GenerateUniqueReturnNumberAsync()
        {
            var baseNumber = $"R-{DateTime.Now:yyyyMMdd}-";

            var existingNumbers = await _dbSet
                .AsNoTracking()
                .Where(r => r.ReturnNumber != null && r.ReturnNumber.StartsWith(baseNumber))
                .Select(r => r.ReturnNumber)
                .ToListAsync();

            var existingSet = new HashSet<string>(existingNumbers, StringComparer.OrdinalIgnoreCase);
            int maxSeq = 0;
            foreach (var num in existingNumbers)
            {
                if (num.Length > baseNumber.Length)
                {
                    var suffix = num.Substring(baseNumber.Length);
                    var dashIdx = suffix.IndexOf('-');
                    if (dashIdx > 0) suffix = suffix.Substring(0, dashIdx);
                    if (int.TryParse(suffix, out int lastNum) && lastNum > maxSeq)
                    {
                        maxSeq = lastNum;
                    }
                }
            }

            int nextNumber = maxSeq + 1;
            string candidate = $"{baseNumber}{nextNumber:D3}";
            while (existingSet.Contains(candidate) ||
                   await _dbSet.AsNoTracking().AnyAsync(r => r.ReturnNumber == candidate))
            {
                nextNumber++;
                candidate = $"{baseNumber}{nextNumber:D3}";
            }

            return candidate;
        }
    }
}
