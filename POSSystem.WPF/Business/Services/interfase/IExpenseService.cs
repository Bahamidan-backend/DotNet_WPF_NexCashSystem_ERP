using POSSystem.WPF.Models;

namespace POSSystem.WPF.Business.Services.interfase
{
    public interface IExpenseService
    {
        Task<IEnumerable<Expense>> GetAllExpensesAsync();
        Task<IEnumerable<Expense>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Expense>> GetByCategoryAsync(string category);
        Task<IEnumerable<Expense>> GetByTypeAsync(ExpenseType type);
        Task<Expense?> GetExpenseByIdAsync(int id);
        Task<Expense> CreateExpenseAsync(Expense expense);
        Task<Expense> UpdateExpenseAsync(Expense expense);
        Task DeleteExpenseAsync(int id);
        Task<decimal> GetTotalByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalByCategoryAsync(string category, DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<string, decimal>> GetExpensesByCategoryAsync(DateTime startDate, DateTime endDate);
    }
}

