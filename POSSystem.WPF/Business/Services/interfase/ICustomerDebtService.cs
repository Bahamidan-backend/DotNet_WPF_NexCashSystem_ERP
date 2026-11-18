namespace POSSystem.WPF.Business.Services.interfase
{
    public interface ICustomerDebtService
    {
        Task<IEnumerable<CustomerDebt>> GetAllDebtsAsync();
        Task<IEnumerable<CustomerDebt>> GetUnpaidDebtsAsync();
        Task<IEnumerable<CustomerDebt>> GetPaidDebtsAsync();
        Task<IEnumerable<CustomerDebt>> GetPaidDebtsInRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<CustomerDebt>> GetDebtsByCustomerNameAsync(string customerName);
        Task<IEnumerable<CustomerDebt>> GetOldDebtsAsync(int days = 30);
        Task<CustomerDebt?> GetDebtByIdAsync(int id);
        Task<CustomerDebt> CreateDebtAsync(CustomerDebt debt);
        Task<CustomerDebt> UpdateDebtAsync(CustomerDebt debt);
        Task MarkDebtAsPaidAsync(int debtId, int userId, int? shiftId, PaymentMethod paymentMethod = PaymentMethod.Cash);
        Task RepayDebtAsync(int debtId, decimal amount, int userId, int? shiftId, string? notes = null, PaymentMethod paymentMethod = PaymentMethod.Cash);
        Task DeleteDebtAsync(int debtId);
        Task<decimal> GetTotalUnpaidDebtsAsync();
        Task<decimal> GetTotalPaidDebtsAsync();
        Task ConsolidateOpenSameDayDebtsAsync();
        Task<IEnumerable<CustomerLedgerEntry>> GetCustomerLedgerAsync(int customerId);
        Task<IEnumerable<CustomerLedgerEntry>> GetCustomerLedgerByNameOrPhoneAsync(string customerNameOrPhone);
    }
}
