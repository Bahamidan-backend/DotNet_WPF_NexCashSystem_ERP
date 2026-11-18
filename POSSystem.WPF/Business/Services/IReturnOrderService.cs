using POSSystem.WPF.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace POSSystem.WPF.Business.Services
{
    public interface IReturnOrderService
    {
        Task<IEnumerable<ReturnOrder>> GetAllReturnsAsync();
        Task<ReturnOrder?> GetReturnByIdAsync(int id);
        
        /// <summary>
        /// Process a return for an order
        /// </summary>
        /// <param name="returnOrder">The return order details including items to return</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> ProcessReturnAsync(ReturnOrder returnOrder);
        
        Task<IEnumerable<ReturnOrder>> GetReturnsByOriginalOrderIdAsync(int orderId);
        Task<IEnumerable<Order>> GetOrdersAvailableForReturnAsync();

        event System.EventHandler ReturnProcessed;
    }
}
