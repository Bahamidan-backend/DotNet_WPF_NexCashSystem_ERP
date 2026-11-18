
namespace POSSystem.WPF.Data.Repositories
{
    public class EmployeeAdvanceRepository : Repository<EmployeeAdvance>, IEmployeeAdvanceRepository
    {
        public EmployeeAdvanceRepository(POSDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<EmployeeAdvance>> GetByEmployeeIdAsync(int employeeId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(a => a.EmployeeId == employeeId)
                .OrderByDescending(a => a.AdvanceDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<EmployeeAdvance>> GetUnpaidAdvancesAsync(int employeeId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(a => a.EmployeeId == employeeId && !a.IsRepaid)
                .OrderByDescending(a => a.AdvanceDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<EmployeeAdvance>> GetUnpaidAdvancesAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(a => !a.IsRepaid)
                .Include(a => a.Employee)
                .OrderByDescending(a => a.AdvanceDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalUnpaidAdvancesAsync(int employeeId)
        {
            var unpaidList = await _dbSet
                .AsNoTracking()
                .Where(a => a.EmployeeId == employeeId && !a.IsRepaid)
                .Select(a => a.Amount - a.PaidAmount)
                .ToListAsync();
            return unpaidList.Sum();
        }

        public async Task<List<EmployeeAdvance>> GetAllAdvancesAsync()
        {
            return await _dbSet
                .Include(a => a.Employee)
                .Where(a =>
                    !a.IsRepaid ||
                    (a.IsRepaid && a.Employee.IsActive)
                )
                .AsNoTracking()
                .ToListAsync();
        }

    }
}

