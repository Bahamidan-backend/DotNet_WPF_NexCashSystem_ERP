using POSSystem.WPF.Models;

namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface ICashBoxTransactionRepository : IRepository<CashBoxTransaction>
    {
        Task<IEnumerable<CashBoxTransaction>> GetByShiftIdAsync(int shiftId);
        Task<IEnumerable<CashBoxTransaction>> GetByUserIdAsync(int userId);
        Task<decimal> GetTotalByShiftIdAsync(int shiftId);
        Task<decimal> GetTotalByTypeAsync(int shiftId, CashBoxTransactionType type);
        Task<IEnumerable<CashBoxTransaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}


