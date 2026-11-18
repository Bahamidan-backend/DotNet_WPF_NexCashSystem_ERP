using POSSystem.WPF.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace POSSystem.WPF.Data.Repositories
{
    public interface IReturnOrderRepository : IRepository<ReturnOrder>
    {
        Task<IEnumerable<ReturnOrder>> GetAllReturnsWithDetailsAsync();
        Task<ReturnOrder?> GetReturnWithDetailsAsync(int returnId);
        Task<IEnumerable<ReturnOrder>> GetReturnsByOriginalOrderIdAsync(int originalOrderId);
        Task<string> GenerateUniqueReturnNumberAsync();
    }
}
