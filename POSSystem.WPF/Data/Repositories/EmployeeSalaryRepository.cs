
namespace POSSystem.WPF.Data.Repositories
{
    public class EmployeeSalaryRepository : Repository<EmployeeSalary>, IEmployeeSalaryRepository
    {
        public EmployeeSalaryRepository(POSDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<EmployeeSalary>> GetByEmployeeIdAsync(int employeeId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(s => s.EmployeeId == employeeId)
                .OrderByDescending(s => s.SalaryDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<EmployeeSalary>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(s => s.Employee)
                .Where(s => s.SalaryDate >= startDate && s.SalaryDate <= endDate)
                .OrderByDescending(s => s.SalaryDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<EmployeeSalary>> GetUnpaidSalariesAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Include(s => s.Employee)
                .Where(s => !s.IsPaid)
                .OrderByDescending(s => s.SalaryDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<EmployeeSalary>> GetUnpaidSalariesByEmployeeIdAsync(int employeeId)
        {
            return await _dbSet
                .Where(s => !s.IsPaid && s.EmployeeId == employeeId)
                .Include(s => s.Employee)
                .OrderByDescending(s => s.SalaryDate)
                .ToListAsync();
        }

        public async Task<EmployeeSalary?> GetLatestSalaryAsync(int employeeId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(s => s.EmployeeId == employeeId)
                .OrderByDescending(s => s.SalaryDate)
                .FirstOrDefaultAsync();
        }
    }
}

