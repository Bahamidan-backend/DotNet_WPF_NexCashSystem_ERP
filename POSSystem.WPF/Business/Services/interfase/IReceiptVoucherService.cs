using POSSystem.WPF.Models;

namespace POSSystem.WPF.Business.Services.interfase
{
    public interface IReceiptVoucherService
    {
        Task<IEnumerable<ReceiptVoucher>> GetAllVouchersAsync();
        Task<IEnumerable<ReceiptVoucher>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<ReceiptVoucher>> GetByTypeAsync(VoucherType type);
        Task<IEnumerable<ReceiptVoucher>> GetByCustomerNameAsync(string customerName);
        Task<ReceiptVoucher?> GetVoucherByIdAsync(int id);
        Task<ReceiptVoucher> CreateVoucherAsync(ReceiptVoucher voucher);
        Task<ReceiptVoucher> UpdateVoucherAsync(ReceiptVoucher voucher);
        Task DeleteVoucherAsync(int id);
        Task<decimal> GetTotalReceiptsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<string> GenerateVoucherNumberAsync();
    }
}

