using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Data.Repositories.Interfase;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Data.Repositories
{
    public class PurchaseRepository : Repository<Purchase>, IPurchaseRepository
    {
        public PurchaseRepository(POSDbContext context) : base(context)
        {
        }

        public static string NormalizeArabicText(string? input, bool stripInternalAlef = false)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var s = input.Trim().ToLowerInvariant()
                .Replace("ـ", "")
                .Replace("أ", "ا")
                .Replace("إ", "ا")
                .Replace("آ", "ا")
                .Replace("ٱ", "ا")
                .Replace("ة", "ه")
                .Replace("ى", "ي")
                .Replace("ؤ", "و")
                .Replace("ئ", "ي");

            if (stripInternalAlef)
            {
                // توحيد الفروقات الإملائية الشائعة مثل (العامودي / العمودي) أو (الحامدي / الحمدي)
                s = s.Replace("ا", "");
            }
            return s;
        }

        public static bool IsArabicSupplierMatch(string? candidateName, string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(candidateName) || string.IsNullOrWhiteSpace(searchTerm))
                return false;

            if (candidateName.Contains(searchTerm.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;

            var normCandidate = NormalizeArabicText(candidateName, false);
            var normSearch = NormalizeArabicText(searchTerm, false);
            if (normCandidate.Contains(normSearch, StringComparison.OrdinalIgnoreCase) ||
                normSearch.Contains(normCandidate, StringComparison.OrdinalIgnoreCase))
                return true;

            var skeletonCandidate = NormalizeArabicText(candidateName, true);
            var skeletonSearch = NormalizeArabicText(searchTerm, true);
            if (!string.IsNullOrWhiteSpace(skeletonCandidate) &&
                !string.IsNullOrWhiteSpace(skeletonSearch) &&
                skeletonSearch.Length >= 3 &&
                (skeletonCandidate.Contains(skeletonSearch, StringComparison.OrdinalIgnoreCase) ||
                 skeletonSearch.Contains(skeletonCandidate, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        private async Task AutoSyncSupplierNamesInPurchasesAsync()
        {
            try
            {
                var suppliers = await _context.Suppliers.AsNoTracking().Where(s => !s.IsDeleted).ToListAsync();
                if (suppliers.Count == 0) return;

                var supplierById = suppliers.ToDictionary(s => s.Id);
                var allPurchases = await _context.Purchases
                    .Where(p => p.SupplierId != null || (p.SupplierName != null && p.SupplierName != ""))
                    .ToListAsync();
                bool anyUpdated = false;

                foreach (var p in allPurchases)
                {
                    if (p.SupplierId.HasValue && supplierById.TryGetValue(p.SupplierId.Value, out var linkedSup))
                    {
                        if (!string.Equals(p.SupplierName, linkedSup.Name, StringComparison.Ordinal))
                        {
                            p.SupplierName = linkedSup.Name;
                            p.Name = linkedSup.Name;
                            anyUpdated = true;
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(p.SupplierName))
                    {
                        var exactOrSkeleton = suppliers.FirstOrDefault(s =>
                            string.Equals(NormalizeArabicText(s.Name, true), NormalizeArabicText(p.SupplierName, true), StringComparison.OrdinalIgnoreCase));

                        if (exactOrSkeleton != null &&
                            (!string.Equals(p.SupplierName, exactOrSkeleton.Name, StringComparison.Ordinal) || p.SupplierId != exactOrSkeleton.Id))
                        {
                            p.SupplierId = exactOrSkeleton.Id;
                            p.SupplierName = exactOrSkeleton.Name;
                            p.Name = exactOrSkeleton.Name;
                            anyUpdated = true;
                        }
                    }
                }

                if (anyUpdated)
                {
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex) { POSSystem.WPF.Services.Logging.AppLogger.LogWarning($"AutoSyncSupplierNames warning: {ex.Message}"); }
        }

        private IQueryable<Purchase> GetBaseQuery(bool asNoTracking = true)
        {
            var query = asNoTracking ? _dbSet.AsNoTracking() : _dbSet;
            return query
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseItems)
                    .ThenInclude(pi => pi.Product)
                .Include(p => p.PurchaseItems)
                    .ThenInclude(pi => pi.Addon)
                .AsSplitQuery();
        }

        public async Task<IEnumerable<Purchase>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            await AutoSyncSupplierNamesInPurchasesAsync();
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1).AddTicks(-1);

            var list = await GetBaseQuery()
                .Where(p => p.PurchaseDate >= start && p.PurchaseDate <= end)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();

            foreach (var p in list)
            {
                if (p.Supplier != null && !string.IsNullOrWhiteSpace(p.Supplier.Name))
                {
                    p.SupplierName = p.Supplier.Name;
                }
            }

            return list;
        }

        public async Task<IEnumerable<Purchase>> GetBySupplierAsync(string supplierName)
        {
            await AutoSyncSupplierNamesInPurchasesAsync();
            var all = await GetBaseQuery()
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();

            foreach (var p in all)
            {
                if (p.Supplier != null && !string.IsNullOrWhiteSpace(p.Supplier.Name))
                {
                    p.SupplierName = p.Supplier.Name;
                }
            }

            if (string.IsNullOrWhiteSpace(supplierName))
                return all;

            return all.Where(p =>
                IsArabicSupplierMatch(p.SupplierName, supplierName) ||
                IsArabicSupplierMatch(p.Supplier?.Name, supplierName) ||
                IsArabicSupplierMatch(p.Name, supplierName)).ToList();
        }

        public async Task<decimal> GetTotalByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var end = endDate.Date.AddDays(1).AddTicks(-1);
            var totals = await _dbSet
                .AsNoTracking()
                .Where(p => p.PurchaseDate >= start && p.PurchaseDate <= end)
                .Select(p => p.TotalAmount)
                .ToListAsync();
            return totals.Sum();
        }

        public async Task<Purchase?> GetWithItemsAsync(int id)
        {
            return await GetBaseQuery(asNoTracking: false)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}
