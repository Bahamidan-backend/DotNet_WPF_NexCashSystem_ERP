
namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface IEmployeeAdvanceRepository : IRepository<EmployeeAdvance>
    {
        Task<IEnumerable<EmployeeAdvance>> GetByEmployeeIdAsync(int employeeId);
        Task<IEnumerable<EmployeeAdvance>> GetUnpaidAdvancesAsync(int employeeId);
        Task<IEnumerable<EmployeeAdvance>> GetUnpaidAdvancesAsync();
        Task<decimal> GetTotalUnpaidAdvancesAsync(int employeeId);
        Task<List<EmployeeAdvance>> GetAllAdvancesAsync();

    }
}

