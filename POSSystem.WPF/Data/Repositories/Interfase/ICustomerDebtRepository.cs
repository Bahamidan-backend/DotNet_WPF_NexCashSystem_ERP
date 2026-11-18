
namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface ICustomerDebtRepository : IRepository<CustomerDebt>
    {
        Task<IEnumerable<CustomerDebt>> GetUnpaidDebtsAsync();
        Task<IEnumerable<CustomerDebt>> GetPaidDebtsAsync();
        Task<IEnumerable<CustomerDebt>> GetPaidDebtsInRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<CustomerDebt>> GetDebtsByCustomerNameAsync(string customerName);
        Task<IEnumerable<CustomerDebt>> GetOldDebtsAsync(int days = 30);
        Task<decimal> GetTotalUnpaidDebtsAsync();
        Task<decimal> GetTotalPaidDebtsAsync();
    }
}

