

namespace POSSystem.WPF.Data.Repositories
{
    public class SaleRepository : Repository<Sale>, ISaleRepository
    {
        public SaleRepository(POSDbContext context) : base(context)
        {
        }

        private IQueryable<Sale> GetBaseSaleQuery(bool asNoTracking = true)
        {
            var query = asNoTracking ? _dbSet.AsNoTracking() : _dbSet;
            return query
                .Include(s => s.User)
                .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                .AsSplitQuery();
        }

        public async Task<IEnumerable<Sale>> GetSalesWithItemsAsync()
        {
            return await GetBaseSaleQuery(asNoTracking: true)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Sale>> GetSalesByUserAsync(int userId)
        {
            return await GetBaseSaleQuery(asNoTracking: true)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Sale>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var endExclusive = endDate.Date.AddDays(1);

            return await GetBaseSaleQuery(asNoTracking: true)
                .Where(s => s.CreatedAt >= start && s.CreatedAt < endExclusive)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Sale>> GetSalesByStatusAsync(SaleStatus status)
        {
            return await GetBaseSaleQuery(asNoTracking: true)
                .Where(s => s.Status == status)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<Sale?> GetSaleWithItemsAsync(int saleId)
        {
            return await GetBaseSaleQuery(asNoTracking: false)
                .FirstOrDefaultAsync(s => s.Id == saleId);
        }

        private static readonly object _invoiceNumberLock = new object();
        private static string _lastGeneratedInvoiceNumber = string.Empty;

        public async Task<string> GenerateUniqueInvoiceNumberAsync()
        {
            var today = DateTime.Now;
            var datePrefix = today.ToString("yyyyMMdd");
            var baseNumber = $"INV-{datePrefix}-";

            var existingNumbers = await _dbSet
                .AsNoTracking()
                .Where(s => s.InvoiceNumber != null && s.InvoiceNumber.StartsWith(baseNumber))
                .Select(s => s.InvoiceNumber)
                .ToListAsync();

            var existingSet = new HashSet<string>(existingNumbers, StringComparer.OrdinalIgnoreCase);

            lock (_invoiceNumberLock)
            {
                if (!string.IsNullOrWhiteSpace(_lastGeneratedInvoiceNumber) && _lastGeneratedInvoiceNumber.StartsWith(baseNumber))
                {
                    existingSet.Add(_lastGeneratedInvoiceNumber);
                }

                int maxSeq = 0;
                foreach (var num in existingSet)
                {
                    if (num.Length > baseNumber.Length)
                    {
                        var suffix = num.Substring(baseNumber.Length);
                        var dashIdx = suffix.IndexOf('-');
                        if (dashIdx > 0) suffix = suffix.Substring(0, dashIdx);
                        if (int.TryParse(suffix, out var lastNumber) && lastNumber > maxSeq)
                        {
                            maxSeq = lastNumber;
                        }
                    }
                }

                int nextNumber = maxSeq + 1;
                string candidate = $"{baseNumber}{nextNumber:D3}";
                while (existingSet.Contains(candidate))
                {
                    nextNumber++;
                    candidate = $"{baseNumber}{nextNumber:D3}";
                }

                _lastGeneratedInvoiceNumber = candidate;
                return _lastGeneratedInvoiceNumber;
            }
        }

        public async Task<bool> InvoiceNumberExistsAsync(string invoiceNumber)
        {
            return await _dbSet.AnyAsync(s => s.InvoiceNumber == invoiceNumber);
        }

        public async Task<IEnumerable<Sale>> GetTodaySalesAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            
            return await GetBaseSaleQuery(asNoTracking: true)
                .Where(s => s.CreatedAt >= today && s.CreatedAt < tomorrow)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalSalesByDateAsync(DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);
            
            var amounts = await _dbSet
                .Where(s => s.CreatedAt >= startDate && s.CreatedAt < endDate)
                .Select(s => (decimal?)s.FinalAmount)
                .ToListAsync();
            return amounts.Sum() ?? 0m;
        }

        public async Task<decimal> GetTotalSalesByMonthAsync(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);
            
            var amounts = await _dbSet
                .Where(s => s.CreatedAt >= startDate && s.CreatedAt < endDate)
                .Select(s => (decimal?)s.FinalAmount)
                .ToListAsync();
            return amounts.Sum() ?? 0m;
        }
    }
}
