using POSSystem.WPF.Models;

namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface IShiftRepository : IRepository<Shift>
    {
        Task<Shift?> GetActiveShiftByUserIdAsync(int userId);
        Task<IEnumerable<Shift>> GetShiftsByUserIdAsync(int userId);
        Task<IEnumerable<Shift>> GetShiftsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Shift?> GetShiftByShiftNumberAsync(string shiftNumber);
        Task<string> GenerateShiftNumberAsync();
    }
}


