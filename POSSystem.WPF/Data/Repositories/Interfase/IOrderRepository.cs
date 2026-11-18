
namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<IEnumerable<Order>> GetOrdersWithItemsAsync();
        Task<PagedResult<Order>> GetOrdersWithItemsPagedAsync(int pageNumber, int pageSize);
        Task<IEnumerable<Order>> GetOrdersByUserAsync(int userId);
        Task<PagedResult<Order>> GetOrdersByUserPagedAsync(int userId, int pageNumber, int pageSize);
        Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate, bool includeRelations = true);
        Task<PagedResult<Order>> GetOrdersByDateRangePagedAsync(DateTime startDate, DateTime endDate, int pageNumber, int pageSize);
        Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status);
        Task<PagedResult<Order>> GetOrdersByStatusPagedAsync(OrderStatus status, int pageNumber, int pageSize);
        Task<Order?> GetOrderWithItemsAsync(int orderId);
        Task<string> GenerateUniqueOrderNumberAsync(int userId);
        Task<bool> OrderNumberExistsAsync(string orderNumber);
        Task<IEnumerable<Order>> GetTodayOrdersAsync();
        Task<decimal> GetTotalSalesByDateAsync(DateTime date);
        Task<IEnumerable<Order>> GetOrdersByDateRangeAddonsAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Order>> GetOrdersWithPymentMethodeAsync(PaymentMethod paymentMethod);
        Task<PagedResult<OrderListDto>> GetOrdersListPagedAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? customerSearch = null,
            OrderStatus? status = null,
            PaymentMethod? paymentMethod = null,
            int page = 1,
            int pageSize = 20);

    }
}
