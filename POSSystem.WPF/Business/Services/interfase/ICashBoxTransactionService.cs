using POSSystem.WPF.Models;

namespace POSSystem.WPF.Business.Services.interfase
{
    public interface ICashBoxTransactionService
    {
        Task<CashBoxTransaction> CreateTransactionAsync(CashBoxTransaction transaction);
        Task<IEnumerable<CashBoxTransaction>> GetTransactionsByShiftIdAsync(int shiftId);
        Task<decimal> GetCashBoxBalanceAsync(int shiftId);
        Task<CashBoxTransaction> CreateAdvanceAsync(int shiftId, int userId, decimal amount, string description);
        Task<CashBoxTransaction> CreateExpenseAsync(int shiftId, decimal amount, string description);
        Task<IEnumerable<CashBoxTransaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<int> CleanupOrphanedCashBoxTransactionsAsync(int? shiftId = null);
    }
}


