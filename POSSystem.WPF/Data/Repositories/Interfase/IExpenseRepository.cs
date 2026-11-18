using POSSystem.WPF.Models;

namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface IExpenseRepository : IRepository<Expense>
    {
        Task<IEnumerable<Expense>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Expense>> GetByCategoryAsync(string category);
        Task<IEnumerable<Expense>> GetByTypeAsync(ExpenseType type);
        Task<decimal> GetTotalByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalByCategoryAsync(string category, DateTime? startDate = null, DateTime? endDate = null);
    }
}

