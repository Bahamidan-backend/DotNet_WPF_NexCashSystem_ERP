using POSSystem.WPF.Models;

namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<Customer?> GetByPhoneNumberAsync(string phoneNumber);
        Task<Customer?> GetByNameAsync(string name);
    }
}
