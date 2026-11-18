
namespace POSSystem.WPF.Business.Services.interfase
{
    public interface IOrderService
    {
        event EventHandler<Order> OrderPlaced;

        // ─── Read ───────────────────────────────────────────────────
        Task<IEnumerable<Order>> GetAllOrdersAsync();
        Task<IEnumerable<Order>> GetOrdersWithItemsAsync();
        Task<Order?> GetOrderByIdAsync(int id);
        Task<Order?> GetOrderWithItemsAsync(int id);
        Task<IEnumerable<Order>> GetOrdersByUserAsync(int userId);
        Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate, bool includeRelations = true);
        Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status);
        Task<IEnumerable<Order>> GetTodayOrdersAsync();
        Task<IEnumerable<Order>> GetOrdersByDateRangeAddonsAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<CustomerDebt>> GetOrdersWithPymentMethodeAsync(PaymentMethod paymentMethod);
        Task<decimal> GetTotalSalesByDateAsync(DateTime date);

        // ─── Write ──────────────────────────────────────────────────
        Task<Order> CreateOrderAsync(Order order);
        Task<Order> UpdateOrderAsync(Order order);
        Task DeleteOrderAsync(int orderId);
        Task CancelOrderAsync(int orderId, string reason, int? cancelledByUserId = null, bool restoreInventory = true);
        Task<string> GenerateUniqueOrderNumberAsync(int userId);

        // ─── Validation ─────────────────────────────────────────────
        bool ValidateOrder(Order order);
        Task<bool> ValidateOrderAsync(Order order);

        // ─── Stock ──────────────────────────────────────────────────
        Task RestoreStockForOrderItemsAsync(IEnumerable<OrderItem> orderItems, int? orderId = null, string? orderNumber = null, int? userId = null);

        // ─── Transaction ────────────────────────────────────────────
        Task<bool> ProcessOrderAsync(Order order);
        Task<Order> PlaceOrderTransactionAsync(Order order, PaymentMethod paymentMethod, User user, Shift? shift, CustomerDebt? customerDebt = null);

        // ─── UI helpers (stateless, operate on passed-in collections) ─
        void AddProductToOrder(Product product, bool hasActiveShift);
        Task RemoveProductFromOrder(OrderItem orderItem, System.Collections.ObjectModel.ObservableCollection<Product> allProducts);
        Task IncreaseQuantity(OrderItem orderItem);
        Task DecreaseQuantity(OrderItem orderItem);
    }
}
