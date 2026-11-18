using POSSystem.WPF.Models;

namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface IReceiptVoucherRepository : IRepository<ReceiptVoucher>
    {
        Task<IEnumerable<ReceiptVoucher>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<ReceiptVoucher>> GetByTypeAsync(VoucherType type);
        Task<IEnumerable<ReceiptVoucher>> GetByCustomerNameAsync(string customerName);
        Task<decimal> GetTotalReceiptsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}

