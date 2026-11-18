
namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface IEmployeeSalaryRepository : IRepository<EmployeeSalary>
    {
        Task<IEnumerable<EmployeeSalary>> GetByEmployeeIdAsync(int employeeId);
        Task<IEnumerable<EmployeeSalary>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<EmployeeSalary>> GetUnpaidSalariesAsync();
        Task<IEnumerable<EmployeeSalary>> GetUnpaidSalariesByEmployeeIdAsync(int employeeId);
        Task<EmployeeSalary?> GetLatestSalaryAsync(int employeeId);
    }
}

