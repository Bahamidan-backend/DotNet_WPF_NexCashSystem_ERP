using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface IDeliveryRepository
    {
        Task<IEnumerable<Delivery>> GetAllAsync();
        Task<Delivery?> GetByIdAsync(int id);
        Task<Delivery> AddAsync(Delivery delivery);
        Task UpdateAsync(Delivery delivery);
        Task DeleteAsync(int id);
    }
}
