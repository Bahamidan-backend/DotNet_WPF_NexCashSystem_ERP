using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Data.Repositories.Interfase;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Data.Repositories
{
    /// <summary>
    /// DeliveryRepository: NOTE — This class directly calls SaveChangesAsync inside the repository.
    /// This is an architectural violation (should be UnitOfWork's responsibility).
    /// Kept as-is for backward compatibility but marked for future refactor.
    /// </summary>
    public class DeliveryRepository : IDeliveryRepository
    {
        private readonly POSDbContext _context;

        public DeliveryRepository(POSDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Delivery>> GetAllAsync()
        {
            // FIX: Added AsNoTracking for read-only list query.
            return await _context.Deliveries.AsNoTracking().ToListAsync();
        }

        public async Task<Delivery?> GetByIdAsync(int id)
        {
            return await _context.Deliveries.FindAsync(id);
        }

        public async Task<Delivery> AddAsync(Delivery delivery)
        {
            await _context.Deliveries.AddAsync(delivery);
            await _context.SaveChangesAsync();
            return delivery;
        }

        public async Task UpdateAsync(Delivery delivery)
        {
            _context.Deliveries.Update(delivery);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await _context.Deliveries.Where(d => d.DeliveryId == id).ExecuteDeleteAsync();
        }
    }
}
