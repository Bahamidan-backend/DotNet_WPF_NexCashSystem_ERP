using POSSystem.WPF.Models;
using POSSystem.WPF.Data.Repositories.Interfase;
using Microsoft.EntityFrameworkCore;

namespace POSSystem.WPF.Data.Repositories
{
    public class CustomerRepository : Repository<Customer>, ICustomerRepository
    {
        public CustomerRepository(POSDbContext context) : base(context)
        {
        }

        public async Task<Customer?> GetByPhoneNumberAsync(string phoneNumber)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber && c.IsActive);
        }

        public async Task<Customer?> GetByNameAsync(string name)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(c => c.Name == name && c.IsActive);
        }
    }
}
