using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Data;
using POSSystem.WPF.Models;
using POSSystem.WPF.Data.Repositories.Interfase;

namespace POSSystem.WPF.Data.Repositories
{
    public class PurchaseReturnRepository : Repository<PurchaseReturn>, IPurchaseReturnRepository
    {
        public PurchaseReturnRepository(POSDbContext context) : base(context)
        {
        }

        public override async Task<PurchaseReturn?> GetByIdAsync(int id)
        {
            return await _dbSet.AsNoTracking()
                .Include(r => r.PurchaseReturnItems)
                    .ThenInclude(ri => ri.OriginalPurchaseItem)
                        .ThenInclude(pi => pi.Product)
                .Include(r => r.PurchaseReturnItems)
                    .ThenInclude(ri => ri.OriginalPurchaseItem)
                        .ThenInclude(pi => pi.Addon)
                .Include(r => r.OriginalPurchase)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public override async Task<IEnumerable<PurchaseReturn>> GetAllAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.OriginalPurchase)
                .OrderByDescending(r => r.ReturnDate)
                .ToListAsync();
        }

        public async Task UpdateAsync(PurchaseReturn purchaseReturn)
        {
            // FIX: SaveChanges removed — this is the UnitOfWork's responsibility.
            Update(purchaseReturn);
        }

        public async Task DeleteAsync(int id)
        {
            // FIX: SaveChanges removed — this is the UnitOfWork's responsibility.
            await RemoveByIdAsync(id);
        }

        private static readonly object _returnNumberLock = new object();
        private static string _lastGeneratedReturnNumber = string.Empty;

        public async Task<string> GenerateReturnNumberAsync()
        {
            var dateStr = DateTime.Now.ToString("yyyyMMdd");
            var baseNumber = $"PR-{dateStr}-";

            var existingNumbers = await _context.PurchaseReturns
                .AsNoTracking()
                .Where(r => r.ReturnNumber != null && r.ReturnNumber.StartsWith(baseNumber))
                .Select(r => r.ReturnNumber)
                .ToListAsync();

            var existingSet = new HashSet<string>(existingNumbers, StringComparer.OrdinalIgnoreCase);

            lock (_returnNumberLock)
            {
                if (!string.IsNullOrWhiteSpace(_lastGeneratedReturnNumber) && _lastGeneratedReturnNumber.StartsWith(baseNumber))
                {
                    existingSet.Add(_lastGeneratedReturnNumber);
                }

                int maxSeq = 0;
                foreach (var num in existingSet)
                {
                    if (num.Length > baseNumber.Length)
                    {
                        var suffix = num.Substring(baseNumber.Length);
                        var dashIdx = suffix.IndexOf('-');
                        if (dashIdx > 0) suffix = suffix.Substring(0, dashIdx);
                        if (int.TryParse(suffix, out int parsed) && parsed > maxSeq)
                        {
                            maxSeq = parsed;
                        }
                    }
                }

                int nextNum = maxSeq + 1;
                string candidate = $"{baseNumber}{nextNum:D4}";
                while (existingSet.Contains(candidate))
                {
                    nextNum++;
                    candidate = $"{baseNumber}{nextNum:D4}";
                }

                _lastGeneratedReturnNumber = candidate;
                return _lastGeneratedReturnNumber;
            }
        }

        public async Task<IEnumerable<PurchaseReturn>> SearchAsync(string searchTerm, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.PurchaseReturns
                .AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.OriginalPurchase)
                .AsQueryable();

            // Filter by date first in SQL, then apply Arabic-aware supplier/return search
            string? term = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();

            // مقارنة مباشرة تدعم الـ Index في SQLite
            if (fromDate.HasValue)
            {
                var start = fromDate.Value.Date;
                query = query.Where(r => r.ReturnDate >= start);
            }

            if (toDate.HasValue)
            {
                var endExclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(r => r.ReturnDate < endExclusive);
            }

            var results = await query
                .Include(r => r.OriginalPurchase)
                    .ThenInclude(p => p!.Supplier)
                .Include(r => r.PurchaseReturnItems)
                    .ThenInclude(ri => ri.OriginalPurchaseItem)
                        .ThenInclude(pi => pi.Product)
                .Include(r => r.PurchaseReturnItems)
                    .ThenInclude(ri => ri.OriginalPurchaseItem)
                        .ThenInclude(pi => pi.Addon)
                .AsSplitQuery()
                .OrderByDescending(r => r.ReturnDate)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(term))
            {
                results = results.Where(r =>
                    (r.ReturnNumber != null && r.ReturnNumber.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (r.Reason != null && r.Reason.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (r.OriginalPurchase != null && r.OriginalPurchase.PurchaseNumber != null && r.OriginalPurchase.PurchaseNumber.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (r.OriginalPurchase != null && PurchaseRepository.IsArabicSupplierMatch(r.OriginalPurchase.SupplierName, term)) ||
                    (r.OriginalPurchase?.Supplier != null && PurchaseRepository.IsArabicSupplierMatch(r.OriginalPurchase.Supplier.Name, term))
                ).ToList();
            }

            return results;
        }
    }
}
