using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Business.Services.interfase
{
    public interface IHeldOrderService
    {
        Task<IEnumerable<HeldOrder>> GetAllHeldOrdersAsync();
        Task<IEnumerable<HeldOrder>> GetHeldOrdersByShiftAsync(int shiftId);
        Task<HeldOrder?> GetHeldOrderByIdAsync(Guid id);
        Task<HeldOrder> SaveHeldOrderAsync(HeldOrder heldOrder);
        Task<bool> DeleteHeldOrderAsync(Guid id);
        Task<int> ClearAllHeldOrdersAsync();
    }
}

