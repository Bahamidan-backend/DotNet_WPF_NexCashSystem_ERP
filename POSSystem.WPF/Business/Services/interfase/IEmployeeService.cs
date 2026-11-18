
namespace POSSystem.WPF.Business.Services.interfase
{
    public interface IEmployeeService
    {
        // Employee management
        Task<IEnumerable<User>> GetAllEmployeesAsync();
        Task<User?> GetEmployeeByIdAsync(int id);
        Task<User> CreateEmployeeAsync(User employee);
        Task<User> UpdateEmployeeAsync(User employee);
        Task DeleteEmployeeAsync(int employeeId);
        
        // Employee Advances
        Task<IEnumerable<EmployeeAdvance>> GetEmployeeAdvancesAsync(int employeeId);
        Task<IEnumerable<EmployeeAdvance>> GetAllUnpaidAdvancesAsync();
        Task<EmployeeAdvance> CreateAdvanceAsync(EmployeeAdvance advance);
        Task<EmployeeAdvance> UpdateAdvanceAsync(EmployeeAdvance advance);
        Task DeleteAdvanceAsync(int advanceId);
        Task RepayAdvanceAsync(int advanceId, decimal amount, string? notes = null);
        Task MarkAdvanceAsRepaidAsync(int advanceId);
        Task<decimal> GetTotalUnpaidAdvancesAsync(int employeeId);
        
        // Employee Salaries
        Task<IEnumerable<EmployeeSalary>> GetEmployeeSalariesAsync(int employeeId);
        Task<IEnumerable<EmployeeSalary>> GetSalariesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<EmployeeSalary>> GetAllUnpaidSalariesAsync();
        Task<EmployeeSalary> CreateSalaryAsync(EmployeeSalary salary);
        Task<EmployeeSalary> UpdateSalaryAsync(EmployeeSalary salary);
        Task MarkSalaryAsPaidAsync(int salaryId, PaymentMethod paymentMethod = PaymentMethod.Cash, int? targetShiftId = null, string? walletOrChannelName = null, string? extraNotes = null);
        Task ProcessMonthlySalariesAsync(DateTime salaryDate);
        Task<List<User>?> GetUserByjobnamenameAsync(string DriverName);
        Task<List<EmployeeAdvance>> GetAllAdvancesAsync();
        
        // Employee Statement of Account (كشف حساب موظف)
        Task<List<EmployeeLedgerEntry>> GetEmployeeStatementAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null);



    }
}

