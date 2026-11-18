
namespace POSSystem.WPF.Data.Repositories
{
    public class OrderItemRepository : Repository<OrderItem>, IOrderItemRepository
    {
        public OrderItemRepository(POSDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<OrderItem>> GetOrderItemWithItemsAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Include(o => o.Product)
                .Include(o => o.Order)
                    .ThenInclude(oi => oi.User)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Returns all order items that belong to a specific user's orders.
        /// </summary>
        public async Task<IEnumerable<OrderItem>> GetOrdersByUserAsync(int userId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(o => o.Product)
                .Include(o => o.Order)
                    .ThenInclude(oi => oi.User)
                // FIX: Was incorrectly filtering by o.Id == userId (wrong field).
                // Corrected to filter by the order's user id.
                .Where(o => o.Order != null && o.Order.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }
    }
}
