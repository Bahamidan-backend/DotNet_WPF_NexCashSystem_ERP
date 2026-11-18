
namespace POSSystem.WPF.Data.Repositories
{
    public class CustomerDebtRepository : Repository<CustomerDebt>, ICustomerDebtRepository
    {
        public CustomerDebtRepository(POSDbContext context) : base(context)
        {
        }

        // 1. استعلام خفيف وسريع جداً للقوائم والجداول (بدون سحب تفاصيل عناصر الفواتير وبدون Tracking)
        private IQueryable<CustomerDebt> GetListQuery()
        {
            return _dbSet
                .AsNoTracking()
                .Include(d => d.Customer)
                .Include(d => d.Order);
        }

        // 2. استعلام تفصيلي يستخدم فقط عند فتح تفاصيل دين واحد محدد (GetByIdAsync)
        private IQueryable<CustomerDebt> GetDetailedQuery(bool asNoTracking = false)
        {
            IQueryable<CustomerDebt> query = _dbSet;
            if (asNoTracking) query = query.AsNoTracking();

            return query
                .AsSplitQuery()
                .Include(d => d.Customer)
                .Include(d => d.Order)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                .Include(d => d.Order)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Addon);
        }

        public override async Task<CustomerDebt?> GetByIdAsync(int id)
        {
            return await GetDetailedQuery().FirstOrDefaultAsync(d => d.Id == id);
        }

        public override async Task<IEnumerable<CustomerDebt>> GetAllAsync()
        {
            return await GetListQuery()
                .OrderByDescending(d => d.DebtDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<CustomerDebt>> GetUnpaidDebtsAsync()
        {
            return await GetListQuery()
                .Where(d => !d.IsPaid)
                .OrderByDescending(d => d.DebtDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<CustomerDebt>> GetPaidDebtsAsync()
        {
            return await GetListQuery()
                .Where(d => d.IsPaid)
                .OrderByDescending(d => d.PaymentDate)
                .ToListAsync();
        }

        /// <summary>
        /// Returns paid debts filtered by payment date range.
        /// Preferred over GetPaidDebtsAsync() for report generation to avoid loading all historical data.
        /// </summary>
        public async Task<IEnumerable<CustomerDebt>> GetPaidDebtsInRangeAsync(DateTime startDate, DateTime endDate)
        {
            var startDay = startDate.Date;
            var endExclusive = endDate.Date.AddDays(1);
            return await GetListQuery()
                .Where(d => (d.IsPaid || d.PaidAmount > 0) &&
                            ((d.PaymentDate != null && d.PaymentDate >= startDay && d.PaymentDate < endExclusive) ||
                             (d.PaymentDate == null && d.UpdatedAt >= startDay && d.UpdatedAt < endExclusive)))
                .OrderByDescending(d => d.PaymentDate ?? d.UpdatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<CustomerDebt>> GetDebtsByCustomerNameAsync(string searchString)
        {
            return await GetListQuery()
                .Where(d => d.Customer.Name.Contains(searchString) || (d.Customer.PhoneNumber != null && d.Customer.PhoneNumber.Contains(searchString)))
                .OrderByDescending(d => d.DebtDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<CustomerDebt>> GetOldDebtsAsync(int days = 30)
        {
            var cutoffDate = DateTime.Now.AddDays(-days);
            return await GetListQuery()
                .Where(d => !d.IsPaid && d.DebtDate <= cutoffDate)
                .OrderBy(d => d.DebtDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalUnpaidDebtsAsync()
        {
            var debts = await _dbSet
                .AsNoTracking()
                .Where(d => !d.IsPaid)
                .Select(d => d.Amount - d.PaidAmount)
                .ToListAsync();
            return debts.Sum();
        }

        public async Task<decimal> GetTotalPaidDebtsAsync()
        {
            // FIX ISSUE-FIN-012: Sum all collected PaidAmount including partial payments on open debts
            var paidAmounts = await _dbSet
                .AsNoTracking()
                .Where(d => d.PaidAmount > 0)
                .Select(d => d.PaidAmount)
                .ToListAsync();
            return paidAmounts.Sum();
        }

    }
}

