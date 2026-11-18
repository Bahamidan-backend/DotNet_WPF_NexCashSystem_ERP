using POSSystem.WPF.Data.Repositories.Interfase;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Data.Repositories
{
    public class ReceiptVoucherRepository : Repository<ReceiptVoucher>, IReceiptVoucherRepository
    {
        public ReceiptVoucherRepository(POSDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ReceiptVoucher>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var endExclusive = endDate.Date.AddDays(1);

            return await _dbSet
                .AsNoTracking()
                .Where(v => v.VoucherDate >= start && v.VoucherDate < endExclusive)
                .OrderByDescending(v => v.VoucherDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<ReceiptVoucher>> GetByTypeAsync(VoucherType type)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(v => v.Type == type)
                .OrderByDescending(v => v.VoucherDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<ReceiptVoucher>> GetByCustomerNameAsync(string customerName)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(v => v.CustomerName.Contains(customerName))
                .OrderByDescending(v => v.VoucherDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalReceiptsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var endExclusive = endDate.Date.AddDays(1);

            var amounts = await _dbSet
                .AsNoTracking()
                .Where(v => v.Type == VoucherType.Receipt && v.VoucherDate >= start && v.VoucherDate < endExclusive)
                .Select(v => v.Amount)
                .ToListAsync();
            return amounts.Sum();
        }

        public async Task<decimal> GetTotalPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var endExclusive = endDate.Date.AddDays(1);

            var amounts = await _dbSet
                .AsNoTracking()
                .Where(v => v.Type == VoucherType.Payment && v.VoucherDate >= start && v.VoucherDate < endExclusive)
                .Select(v => v.Amount)
                .ToListAsync();
            return amounts.Sum();
        }
    }
}

