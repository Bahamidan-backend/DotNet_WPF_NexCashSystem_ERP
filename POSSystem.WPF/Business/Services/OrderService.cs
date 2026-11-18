using POSSystem.WPF.Services.Logging;

using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Models;
using POSSystem.WPF.Helpers;
using Microsoft.EntityFrameworkCore;

namespace POSSystem.WPF.Business.Services
{
    /// <summary>
    /// Pure business-layer service for managing Orders, stock deduction, and payment handling.
    /// Does NOT contain any WPF/UI references.
    /// </summary>
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICustomerDebtService _customerDebtService;
        private readonly ICashBoxTransactionService _cashBoxTransactionService;

        public OrderService(
            IUnitOfWork unitOfWork,
            ICustomerDebtService customerDebtService,
            ICashBoxTransactionService cashBoxTransactionService)
        {
            _unitOfWork = unitOfWork;
            _customerDebtService = customerDebtService;
            _cashBoxTransactionService = cashBoxTransactionService;
        }

        // ─────────────────────────────────────────────────────────────
        // Read operations
        // ─────────────────────────────────────────────────────────────

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            return await _unitOfWork.Orders.GetAllAsync();
        }

        public async Task<IEnumerable<Order>> GetOrdersWithItemsAsync()
        {
            return await _unitOfWork.Orders.GetOrdersWithItemsAsync();
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            return await _unitOfWork.Orders.GetByIdAsync(id);
        }

        public async Task<Order?> GetOrderWithItemsAsync(int id)
        {
            return await _unitOfWork.Orders.GetOrderWithItemsAsync(id);
        }

        public async Task<IEnumerable<Order>> GetOrdersByUserAsync(int userId)
        {
            return await _unitOfWork.Orders.GetOrdersByUserAsync(userId);
        }

        public async Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(
            DateTime startDate, DateTime endDate, bool includeRelations = true)
        {
            return await _unitOfWork.Orders.GetOrdersByDateRangeAsync(startDate, endDate, includeRelations);
        }

        public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status)
        {
            return await _unitOfWork.Orders.GetOrdersByStatusAsync(status);
        }

        public async Task<IEnumerable<Order>> GetTodayOrdersAsync()
        {
            return await _unitOfWork.Orders.GetTodayOrdersAsync();
        }

        public async Task<IEnumerable<Order>> GetOrdersByDateRangeAddonsAsync(DateTime startDate, DateTime endDate)
        {
            return await _unitOfWork.Orders.GetOrdersByDateRangeAddonsAsync(startDate, endDate);
        }

        /// <summary>
        /// Returns orders with a specific payment method, projected as CustomerDebt for display purposes.
        /// </summary>
        public async Task<IEnumerable<CustomerDebt>> GetOrdersWithPymentMethodeAsync(PaymentMethod paymentMethod)
        {
            var ordersOnCredit = await _unitOfWork.Orders.GetOrdersWithPymentMethodeAsync(paymentMethod);
            return ordersOnCredit.Select(x => new CustomerDebt
            {
                CustomerName = x.OrderNumber,
                DebtDate = x.CreatedAt,
                PaymentDate = x.CreatedAt,
                Amount = x.Total,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt ?? x.CreatedAt,
                Notes = x.Notes,
                IsPaid = false,
                Id = x.Id,
            });
        }

        public async Task<decimal> GetTotalSalesByDateAsync(DateTime date)
        {
            return await _unitOfWork.Orders.GetTotalSalesByDateAsync(date);
        }

        // ─────────────────────────────────────────────────────────────
        // Write operations
        // ─────────────────────────────────────────────────────────────

        public async Task<Order> CreateOrderAsync(Order order)
        {
            if (!ValidateOrder(order))
                throw new ArgumentException("Invalid order data");

            // Assign current active shift
            var activeShift = await _unitOfWork.Shifts.FirstOrDefaultAsync(s => s.UserId == order.UserId && s.IsActive);
            order.ShiftId = activeShift?.Id;

            // Generate unique order number
            order.OrderNumber = await _unitOfWork.Orders.GenerateUniqueOrderNumberAsync(order.UserId);

            var now = DateTime.Now;
            order.CreatedAt = now;
            order.UpdatedAt = now;

            // Authoritative catalog price lookup (FIN-0003): prevent unauthorized price manipulation
            var context = _unitOfWork.GetContext();
            var orderUser = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == order.UserId);
            bool canEditPrice = orderUser != null && (
                orderUser.Role == UserRole.Admin ||
                POSSystem.WPF.Services.PermissionGuard.HasPermission(orderUser, POSSystem.WPF.Services.SystemPermissions.SalesEditItemPrice) ||
                POSSystem.WPF.Services.PermissionGuard.HasPermission(orderUser, POSSystem.WPF.Services.SystemPermissions.ProductsChangePrice));

            var itemProductIds = (order.OrderItems ?? new List<OrderItem>())
                .Where(i => i.ProductId.HasValue)
                .Select(i => i.ProductId!.Value)
                .Distinct()
                .ToList();

            var dbProductsMap = itemProductIds.Count > 0
                ? await context.Products.AsNoTracking()
                    .Include(p => p.ProductSizes)
                    .Where(p => itemProductIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id)
                : new Dictionary<int, Product>();

            // Clear entity tracking to avoid conflicts
            _unitOfWork.ClearEntityTracking();

            var newOrder = new Order
            {
                OrderNumber = order.OrderNumber,
                OrderType = order.OrderType,
                UserId = order.UserId,
                SubTotal = order.SubTotal,
                ServiceCharge = Math.Max(0m, order.ServiceCharge),
                Discount = Math.Max(0m, order.Discount),
                TaxRate = Math.Max(0m, order.TaxRate),
                TaxAmount = order.TaxAmount,
                Total = order.Total,
                Notes = order.Notes,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                CompletedAt = order.CompletedAt,
                PaymentMethod = order.PaymentMethod,
                Delivery = order.Delivery,
                ShiftId = order.ShiftId,
                OrderItems = new List<OrderItem>()
            };

            foreach (var item in order.OrderItems ?? new List<OrderItem>())
            {
                decimal effectiveUnitPrice = item.UnitPrice;
                if (!canEditPrice && item.ProductId.HasValue && dbProductsMap.TryGetValue(item.ProductId.Value, out var dbProd))
                {
                    var normSize = item.Size?.Trim() ?? string.Empty;
                    var matchedSize = dbProd.ProductSizes?.FirstOrDefault(s =>
                        string.Equals(s.SizeName?.Trim(), normSize, StringComparison.OrdinalIgnoreCase));
                    decimal catalogPrice = matchedSize != null && matchedSize.Price > 0
                        ? matchedSize.Price
                        : Math.Round(dbProd.Price * POSSystem.WPF.Helpers.PortionSizeHelper.GetSizeMultiplier(normSize), 2);
                    if (catalogPrice > 0 && effectiveUnitPrice != catalogPrice)
                    {
                        effectiveUnitPrice = catalogPrice;
                    }
                }

                decimal effectiveTotalPrice = Math.Round(effectiveUnitPrice * item.Quantity, 2);

                var newOrderItem = new OrderItem
                {
                    ProductId  = item.ProductId,
                    AddonId    = item.AddonId,
                    Quantity   = item.Quantity,
                    UnitPrice  = effectiveUnitPrice,
                    TotalPrice = effectiveTotalPrice,
                    Notes      = item.Notes,
                    CreatedAt  = item.CreatedAt,
                    UpdatedAt  = item.UpdatedAt,
                    Size       = item.Size ?? string.Empty,
                    addonAndOrderItems = (item.addonAndOrderItems ?? new List<AddonAndOrderItem>()).Select(x => new AddonAndOrderItem
                    {
                        AddonId = x.AddonId
                    }).ToList(),
                };
                newOrder.OrderItems.Add(newOrderItem);
            }

            newOrder.SubTotal = Math.Round(newOrder.OrderItems.Sum(i => i.TotalPrice), 2);
            newOrder.Discount = Math.Clamp(newOrder.Discount, 0m, newOrder.SubTotal);
            newOrder.TaxAmount = order.TaxAmount > 0
                ? order.TaxAmount
                : (newOrder.TaxRate > 0 ? Math.Round(newOrder.SubTotal * newOrder.TaxRate, 2) : 0m);
            decimal deliveryFee = newOrder.Delivery?.DeliveryFee ?? 0m;
            newOrder.Total = Math.Round(newOrder.SubTotal - newOrder.Discount + newOrder.ServiceCharge + newOrder.TaxAmount + deliveryFee, 2);

            await _unitOfWork.Orders.AddAsync(newOrder);
            await _unitOfWork.SaveChangesAsync();

            return newOrder;
        }

        public async Task<Order> UpdateOrderAsync(Order order)
        {
            if (!ValidateOrder(order))
                throw new ArgumentException("Invalid order data");

            var existingOrder = await _unitOfWork.Orders.GetOrderWithItemsAsync(order.Id);
            if (existingOrder == null)
                throw new ArgumentException("Order not found");

            // When a delivery order is completed, driver settles cash into the cashier drawer
            var context = _unitOfWork.GetContext();
            var entry = context.Entry(existingOrder);
            var originalStatus = entry.Property(o => o.Status).OriginalValue;

            bool isDelivery = existingOrder.OrderType == "delivery";
            bool isStatusBecomingCompleted = (originalStatus != OrderStatus.Completed && order.Status == OrderStatus.Completed)
                                          || (existingOrder.Status != OrderStatus.Completed && order.Status == OrderStatus.Completed);

            bool alreadyDeposited = false;
            if (isDelivery && (order.Status == OrderStatus.Completed || isStatusBecomingCompleted))
            {
                alreadyDeposited = await _unitOfWork.CashBoxTransactions.AnyAsync(t => t.Name.Contains(existingOrder.OrderNumber));
            }

            bool isBecomingCompleted = isStatusBecomingCompleted || (order.Status == OrderStatus.Completed && !alreadyDeposited);

            if (isBecomingCompleted && isDelivery && !alreadyDeposited)
            {
                decimal cashToDeposit = 0m;
                if (existingOrder.PaymentMethod == PaymentMethod.Cash)
                {
                    cashToDeposit = existingOrder.Total;
                }
                else if (existingOrder.PaymentMethod == PaymentMethod.Split)
                {
                    var split = POSSystem.WPF.Helpers.OrderSplitHelper.ParseSplit(existingOrder.Notes);
                    if (split != null && split.CashAmount > 0)
                    {
                        cashToDeposit = split.CashAmount;
                    }
                }

                if (cashToDeposit > 0)
                {
                    var shift = existingOrder.ShiftId.HasValue ? await _unitOfWork.Shifts.GetByIdAsync(existingOrder.ShiftId.Value) : null;
                    if (shift == null || !shift.IsActive)
                    {
                        shift = await _unitOfWork.GetContext().Shifts
                            .Where(s => s.IsActive)
                            .OrderByDescending(s => s.UserId == existingOrder.UserId)
                            .FirstOrDefaultAsync();
                    }

                        if (shift != null)
                        {
                            var driverName = existingOrder.Delivery?.DeliveryDriverName ?? "سائق التوصيل";
                            var cashTransaction = new CashBoxTransaction
                            {
                                Name = $"تحصيل توصيل - {existingOrder.OrderNumber}",
                                ShiftId = shift.Id,
                                UserId = existingOrder.UserId,
                                Type = CashBoxTransactionType.Income,
                                Amount = cashToDeposit,
                                Description = $"تحصيل نقدية طلب توصيل مكتمل #{existingOrder.OrderNumber} (السائق: {driverName}) بمبلغ {cashToDeposit:N0}",
                                CreatedAt = DateTime.Now,
                                UpdatedAt = DateTime.Now,
                                TransactionDate = DateTime.Now
                            };
                            await _cashBoxTransactionService.CreateTransactionAsync(cashTransaction);
                        }
                    }
                }

            // Update properties on the already-tracked entity instance to prevent EF Core identity map conflicts
            existingOrder.Status = order.Status;
            existingOrder.CompletedAt = order.CompletedAt ?? (order.Status == OrderStatus.Completed ? DateTime.Now : null);
            existingOrder.Notes = order.Notes;
            existingOrder.SubTotal = order.SubTotal;
            existingOrder.Total = order.Total;
            existingOrder.Discount = order.Discount;
            existingOrder.ServiceCharge = order.ServiceCharge;
            existingOrder.TaxAmount = order.TaxAmount;
            existingOrder.TaxRate = order.TaxRate;
            existingOrder.UpdatedAt = DateTime.Now;

            await _unitOfWork.SaveChangesAsync();

            // Sync the caller's UI model object
            order.Status = existingOrder.Status;
            order.CompletedAt = existingOrder.CompletedAt;
            order.UpdatedAt = existingOrder.UpdatedAt;

            return existingOrder;
        }

        public async Task CancelOrderAsync(int orderId, string reason, int? cancelledByUserId = null, bool restoreInventory = true)
        {
            var order = await _unitOfWork.Orders.GetOrderWithItemsAsync(orderId);
            if (order == null)
                throw new ArgumentException("Order not found");

            // FIX ISSUE-FIN-004: Prevent cancelling an order that was already returned to avoid double cash refund and double stock restoration
            if (order.Status == OrderStatus.Cancelled)
                return;
            if (order.Status == OrderStatus.Returned || order.Status == OrderStatus.PartiallyReturned)
                throw new InvalidOperationException("لا يمكن إلغاء طلب تم إرجاعه كلياً أو جزئياً. يرجى استخدام شاشة المرتجعات لمعالجة الكميات المتبقية.");

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // 1. Restore stock ONLY if restoreInventory is true (if food was not already prepared/consumed)
                if (restoreInventory && order.OrderItems != null && order.OrderItems.Any())
                {
                    await RestoreStockForOrderItemsAsync(order.OrderItems, order.Id, order.OrderNumber, cancelledByUserId ?? order.UserId);
                }

                // 2. Reverse cash drawer transaction if cash was collected in drawer
                decimal cashRefundAmount = 0;
                if (order.OrderType != "delivery" || order.Status == OrderStatus.Completed)
                {
                    if (order.PaymentMethod == PaymentMethod.Cash)
                    {
                        cashRefundAmount = order.Total;
                    }
                    else if (order.PaymentMethod == PaymentMethod.Split)
                    {
                        var splitInfo = POSSystem.WPF.Helpers.OrderSplitHelper.ParseSplit(order.Notes);
                        cashRefundAmount = splitInfo?.CashAmount ?? 0;
                    }
                }

                if (cashRefundAmount > 0)
                {
                    var shift = order.ShiftId.HasValue ? await _unitOfWork.Shifts.GetByIdAsync(order.ShiftId.Value) : null;
                    if (shift == null || !shift.IsActive)
                    {
                        shift = await _unitOfWork.GetContext().Shifts
                            .Where(s => s.IsActive)
                            .OrderByDescending(s => s.UserId == order.UserId)
                            .FirstOrDefaultAsync();
                    }

                    if (shift != null)
                    {
                        var refundTrans = new CashBoxTransaction
                        {
                            Name = $"إلغاء بيع - {order.OrderNumber}",
                            ShiftId = shift.Id,
                            UserId = cancelledByUserId ?? order.UserId,
                            Type = CashBoxTransactionType.Expense,
                            Amount = cashRefundAmount,
                            Description = $"استرداد نقدي لإلغاء الفاتورة #{order.OrderNumber} - السبب: {reason}",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            TransactionDate = DateTime.Now
                        };
                        await _cashBoxTransactionService.CreateTransactionAsync(refundTrans);
                    }
                }

                // 3. Reverse linked CustomerDebt if order was OnCredit
                var linkedDebt = await _unitOfWork.CustomerDebts.FirstOrDefaultAsync(d => d.OrderId == order.Id);
                if (linkedDebt != null)
                {
                    if (linkedDebt.PaidAmount <= 0)
                    {
                        _unitOfWork.CustomerDebts.Remove(linkedDebt);
                    }
                    else
                    {
                        linkedDebt.Amount = linkedDebt.PaidAmount;
                        linkedDebt.IsPaid = true;
                        linkedDebt.Notes = string.IsNullOrWhiteSpace(linkedDebt.Notes)
                            ? $"[إلغاء الفاتورة #{order.OrderNumber} - السبب: {reason}]"
                            : $"{linkedDebt.Notes} | [إلغاء الفاتورة #{order.OrderNumber}]";
                        linkedDebt.UpdatedAt = DateTime.Now;
                        _unitOfWork.CustomerDebts.Update(linkedDebt);
                    }
                }

                // 4. Mark Order as Cancelled with audit log in Notes
                order.Status = OrderStatus.Cancelled;
                var now = DateTime.Now;
                var cancelNote = $"[إلغاء الفاتورة بتاريخ {now:yyyy/MM/dd HH:mm} - السبب: {reason}" + 
                                (cancelledByUserId.HasValue ? $" - بواسطة مستخدم #{cancelledByUserId}]" : "]");
                order.Notes = string.IsNullOrWhiteSpace(order.Notes) ? cancelNote : $"{order.Notes}\n{cancelNote}";
                order.UpdatedAt = now;

                _unitOfWork.Orders.Update(order);
                await _unitOfWork.SaveChangesAsync();

                AppLogger.LogAudit(
                    (cancelledByUserId?.ToString() ?? order.UserId.ToString()),
                    "CANCEL_ORDER",
                    $"Order #{order.OrderNumber} (Total: {order.Total:N0}) CANCELLED. Reason: {reason}");
            });

            ServiceLocator.NotifyInventoryChanged();
            ServiceLocator.NotifyProductsChanged();
        }

        public async Task DeleteOrderAsync(int orderId)
        {
            var order = await _unitOfWork.Orders.GetOrderWithItemsAsync(orderId);
            if (order == null)
                throw new ArgumentException("Order not found");

            await CancelOrderAsync(orderId, "طلب حذف الفاتورة (تحويل لإلغاء مسبب)");


        }

        public async Task<string> GenerateUniqueOrderNumberAsync(int userId)
        {
            return await _unitOfWork.Orders.GenerateUniqueOrderNumberAsync(userId);
        }

        /// <summary>
        /// Validates order data. Returns true if valid, false otherwise.
        /// Synchronous — no DB access needed for basic validation.
        /// </summary>
        public bool ValidateOrder(Order order)
        {
            if (order == null) return false;
            if (string.IsNullOrWhiteSpace(order.OrderNumber)) return false;
            if (order.UserId <= 0) return false;
            if (order.OrderItems == null || !order.OrderItems.Any()) return false;
            if (order.Total <= 0) return false;

            foreach (var item in order.OrderItems)
            {
                if (!item.ProductId.HasValue && !item.AddonId.HasValue) return false;
                if (item.Quantity <= 0) return false;
                if (item.UnitPrice <= 0) return false;
                if (item.TotalPrice <= 0) return false;
            }

            return true;
        }

        /// <summary>
        /// Async wrapper for interface compatibility. Delegates to the synchronous ValidateOrder.
        /// </summary>
        public Task<bool> ValidateOrderAsync(Order order)
        {
            return Task.FromResult(ValidateOrder(order));
        }

        public async Task<bool> ProcessOrderAsync(Order order)
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                _unitOfWork.ClearEntityTracking();
                order.Id = 0;
                if (order.OrderItems != null)
                {
                    foreach (var item in order.OrderItems)
                    {
                        item.Id = 0;
                        if (item.addonAndOrderItems != null)
                            foreach (var addon in item.addonAndOrderItems) addon.id = 0;
                    }
                }

                await CreateOrderAsync(order);
                await UpdateStockForOrderItemsAsync(order.OrderItems, order.Id, order.OrderNumber, order.UserId);
                await _unitOfWork.SaveChangesAsync();
    
            });
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // UI helpers (in-memory only, no DB interaction)
        // ─────────────────────────────────────────────────────────────

        public void AddProductToOrder(Product product, bool hasActiveShift)
        {
            // NOTE: This method operates on the CashierProperty state passed in from the ViewModel.
            // Intentionally kept here for IOrderService contract compatibility.
            // The caller (ViewModel) must pass a valid CashierProperty reference.
        }

        public Task RemoveProductFromOrder(OrderItem orderItem, System.Collections.ObjectModel.ObservableCollection<Product> allProducts)
        {
            return Task.CompletedTask;
        }

        public Task IncreaseQuantity(OrderItem orderItem)
        {
            if (orderItem != null) orderItem.Quantity++;
            return Task.CompletedTask;
        }

        public Task DecreaseQuantity(OrderItem orderItem)
        {
            if (orderItem != null && orderItem.Quantity > 1)
                orderItem.Quantity--;
            return Task.CompletedTask;
        }

        // ─────────────────────────────────────────────────────────────
        // Place Order Transaction
        // ─────────────────────────────────────────────────────────────

        public event EventHandler<Order>? OrderPlaced;

        public async Task<Order> PlaceOrderTransactionAsync(
            Order order,
            PaymentMethod paymentMethod,
            User user,
            Shift? shift,
            CustomerDebt? customerDebt = null)
        {
            Order createdOrder = null!;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // 1. Create Order
                createdOrder = await CreateOrderAsync(order);

                // 2. Update Stock
                await UpdateStockForOrderItemsAsync(createdOrder.OrderItems, createdOrder.Id, createdOrder.OrderNumber, user.Id);
    

                // 3. Handle Debt (OnCredit or Split with Debt portion) — uses injected ICustomerDebtService
                if ((paymentMethod == PaymentMethod.OnCredit || paymentMethod == PaymentMethod.Split) && customerDebt != null)
                {
                    if (paymentMethod == PaymentMethod.OnCredit || customerDebt.Amount <= 0)
                        customerDebt.Amount = createdOrder.Total;
                    customerDebt.OrderId = createdOrder.Id;
                    customerDebt.UserId = user.Id;
                    customerDebt.ShiftId = shift?.Id;
                    customerDebt.UpdatedAt = DateTime.Now;

                    var savedDebt = await _customerDebtService.CreateDebtAsync(customerDebt);
                    if (savedDebt != null && savedDebt != customerDebt)
                    {
                        customerDebt.Id = savedDebt.Id;
                        customerDebt.CustomerId = savedDebt.CustomerId;
                        customerDebt.Amount = savedDebt.Amount;
                        customerDebt.PaidAmount = savedDebt.PaidAmount;
                        customerDebt.Notes = savedDebt.Notes;
                    }
                }

                // 4. Handle CashBox (Cash) — uses injected ICashBoxTransactionService
                // Cash in drawer: recorded immediately for dinein/takeaway. Delivery cash is deferred until completion/driver settlement.
                if (paymentMethod == PaymentMethod.Cash && shift != null && createdOrder.OrderType != "delivery")
                {
                    var cashTransaction = new CashBoxTransaction
                    {
                        Name = $"بيع - {createdOrder.OrderNumber}",
                        ShiftId = shift.Id,
                        UserId = user.Id,
                        Type = CashBoxTransactionType.Income,
                        Amount = createdOrder.Total,
                        Description = $"بيع - {createdOrder.OrderNumber}",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        TransactionDate = DateTime.Now
                    };
                    await _unitOfWork.CashBoxTransactions.AddAsync(cashTransaction);
                }
                else if (paymentMethod == PaymentMethod.Split && shift != null && createdOrder.OrderType != "delivery")
                {
                    var splitInfo = POSSystem.WPF.Helpers.OrderSplitHelper.ParseSplit(createdOrder.Notes);
                    if (splitInfo != null && splitInfo.CashAmount > 0)
                    {
                        var cashTransaction = new CashBoxTransaction
                        {
                            Name = $"بيع مجزأ (نقدي) - {createdOrder.OrderNumber}",
                            ShiftId = shift.Id,
                            UserId = user.Id,
                            Type = CashBoxTransactionType.Income,
                            Amount = splitInfo.CashAmount,
                            Description = $"مبيعات مجزأة (نقدي {splitInfo.CashAmount:N0} من {createdOrder.Total:N0}) - {createdOrder.OrderNumber}",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            TransactionDate = DateTime.Now
                        };
                        await _unitOfWork.CashBoxTransactions.AddAsync(cashTransaction);
                    }
                }

                await _unitOfWork.SaveChangesAsync();
            });

            OrderPlaced?.Invoke(this, createdOrder);
            AppLogger.LogInfo($"[NEW ORDER] #{createdOrder.OrderNumber} | Type: {createdOrder.OrderType} | Total: {createdOrder.Total:N0} | Method: {paymentMethod} | User: {user.Name}");
            return createdOrder;
        }

        // ─────────────────────────────────────────────────────────────
        // Stock management (private helpers)
        // ─────────────────────────────────────────────────────────────

        private async Task UpdateStockForOrderItemsAsync(IEnumerable<OrderItem> orderItems, int? orderId = null, string? orderNumber = null, int? userId = null)
        {
            var productIds = orderItems
                .Where(i => i.ProductId.HasValue)
                .Select(i => i.ProductId!.Value)
                .Distinct()
                .ToList();

            var products = await _unitOfWork.Products
                .Include(p => p.Recipe)
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();
            var productMap = products.ToDictionary(p => p.Id);

            // Pre-fetch all required addon IDs (including base addons) in a single query — prevents N+1
            var addonIds = orderItems
                .Where(i => i.AddonId.HasValue)
                .Select(i => i.AddonId!.Value)
                .ToList();
            addonIds.AddRange(orderItems
                .SelectMany(i => i.addonAndOrderItems)
                .Select(a => a.AddonId));
            addonIds = addonIds.Distinct().ToList();

            var addonsList = (await _unitOfWork.Addons.FindAsync(a => addonIds.Contains(a.Id))).ToList();
            var addonMap = addonsList.ToDictionary(a => a.Id);

            var missingBaseAddonIds = addonsList
                .Where(a => a.BaseAddonId.HasValue && a.BaseAddonQuantity > 0 && a.DeductOnSale && !addonMap.ContainsKey(a.BaseAddonId.Value))
                .Select(a => a.BaseAddonId!.Value)
                .Distinct()
                .ToList();

            if (missingBaseAddonIds.Count > 0)
            {
                var baseAddons = await _unitOfWork.Addons.FindAsync(a => missingBaseAddonIds.Contains(a.Id));
                foreach (var ba in baseAddons)
                    addonMap[ba.Id] = ba;
            }
            // Pre-fetch raw material ingredients for any products that have recipes defined (by FK or by Name)
            var allRecipeIngredientIds = products
                .Where(p => p.Recipe != null)
                .SelectMany(p => p.Recipe)
                .Where(r => r.WarehouseFromProductId.HasValue)
                .Select(r => r.WarehouseFromProductId!.Value)
                .Distinct()
                .ToList();

            var allRecipeIngredientNames = products
                .Where(p => p.Recipe != null)
                .SelectMany(p => p.Recipe)
                .Where(r => !string.IsNullOrWhiteSpace(r.Name) && r.Quantity > 0)
                .Select(r => r.Name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var whIngredients = (allRecipeIngredientIds.Any() || allRecipeIngredientNames.Any())
                ? await _unitOfWork.warehouseFromProduct.FindAsync(wp => wp.IsActive && (allRecipeIngredientIds.Contains(wp.Id) || allRecipeIngredientNames.Contains(wp.Name)))
                : Enumerable.Empty<WarehouseFromProduct>();

            var whIngredientsById = whIngredients.ToDictionary(w => w.Id);
            var whIngredientsMap = new Dictionary<string, WarehouseFromProduct>(StringComparer.OrdinalIgnoreCase);
            foreach (var whp in whIngredients)
            {
                if (!string.IsNullOrWhiteSpace(whp.Name) && !whIngredientsMap.ContainsKey(whp.Name.Trim()))
                    whIngredientsMap[whp.Name.Trim()] = whp;
            }

            var now = DateTime.Now;

            // =========================================================================
            // PHASE 1: PRE-VALIDATION (التحقق المسبق الشامل لجميع المكونات والمنتجات)
            // التحقق من توفر كامل الكميات المطلوبة في المخزون والمستودع قبل إجراء أي تعديل
            // =========================================================================

            // 1. تجميع وفحص كميات المنتجات الجاهزة
            var productDeductions = new Dictionary<int, decimal>();
            foreach (var item in orderItems)
            {
                if (item.ProductId.HasValue && productMap.ContainsKey(item.ProductId.Value))
                {
                    decimal deduction = PortionSizeHelper.CalculateDeduction(item.Size, item.Quantity);
                    productDeductions[item.ProductId.Value] = productDeductions.GetValueOrDefault(item.ProductId.Value) + deduction;
                }
            }

            foreach (var (productId, totalDeduction) in productDeductions)
            {
                var product = productMap[productId];
                if (product.IsUntrackedStock) continue;
                if (product.TotalValue < totalDeduction)
                {
                    throw new InvalidOperationException(
                        $"كمية غير كافية للمنتج '{product.Name}'. المتوفر: {product.TotalValue:N0}، المطلوب للطلب: {totalDeduction:N0}");
                }
            }

            // 2. تجميع وفحص كميات الإضافات الأساسية
            var baseAddonDeductions = new Dictionary<int, decimal>();
            foreach (var item in orderItems)
            {
                if (item.addonAndOrderItems != null && item.addonAndOrderItems.Any())
                {
                    foreach (var linked in item.addonAndOrderItems)
                    {
                        if (addonMap.TryGetValue(linked.AddonId, out var linkedAddon) &&
                            linkedAddon.BaseAddonId.HasValue && linkedAddon.BaseAddonQuantity > 0 && linkedAddon.DeductOnSale)
                        {
                            decimal totalDeduct = item.Quantity * linkedAddon.BaseAddonQuantity;
                            baseAddonDeductions[linkedAddon.BaseAddonId.Value] = baseAddonDeductions.GetValueOrDefault(linkedAddon.BaseAddonId.Value) + totalDeduct;
                        }
                    }
                }

                if (item.AddonId.HasValue && addonMap.TryGetValue(item.AddonId.Value, out var addon) &&
                    addon.BaseAddonId.HasValue && addon.BaseAddonQuantity > 0 && addon.DeductOnSale)
                {
                    decimal totalDeduct = item.Quantity * addon.BaseAddonQuantity;
                    baseAddonDeductions[addon.BaseAddonId.Value] = baseAddonDeductions.GetValueOrDefault(addon.BaseAddonId.Value) + totalDeduct;
                }
            }

            foreach (var (baseAddonId, requiredQty) in baseAddonDeductions)
            {
                if (addonMap.TryGetValue(baseAddonId, out var baseAddon))
                {
                    if (baseAddon.Quantity < requiredQty)
                    {
                        throw new InvalidOperationException(
                            $"كمية غير كافية من الإضافة الأساسية '{baseAddon.Name}'. المتوفر: {baseAddon.Quantity:N0}، المطلوب للطلب: {requiredQty:N0}");
                    }
                }
            }

            // 3. تجميع وفحص كميات المواد الخام في المستودع (Warehouse Ingredients)
            var ingredientDeductions = new Dictionary<int, decimal>();
            foreach (var item in orderItems)
            {
                if (item.ProductId.HasValue && productMap.TryGetValue(item.ProductId.Value, out var product))
                {
                    bool isBatchCookedProduct = product.Recipe != null && (
                        product.Recipe.Any(r => r.IsMainIngredient) || 
                        product.YieldRate > 0 || 
                        product.AvailableQtyInPOS > 0
                    );

                    if (!isBatchCookedProduct && product.Recipe != null && product.Recipe.Any())
                    {
                        decimal deduction = PortionSizeHelper.CalculateDeduction(item.Size, item.Quantity);
                        decimal factor = deduction > 0 ? deduction : item.Quantity;

                        foreach (var recipe in product.Recipe)
                        {
                            if (recipe.Quantity <= 0) continue;
                            if (recipe.IsMainIngredient) continue;

                            WarehouseFromProduct? whIngredient = null;
                            if (recipe.WarehouseFromProductId.HasValue && whIngredientsById.TryGetValue(recipe.WarehouseFromProductId.Value, out var foundById))
                                whIngredient = foundById;
                            else if (!string.IsNullOrWhiteSpace(recipe.Name) && whIngredientsMap.TryGetValue(recipe.Name.Trim(), out var foundByName))
                                whIngredient = foundByName;

                            if (whIngredient != null)
                            {
                                decimal ingredientQty = recipe.Quantity * factor;
                                decimal convertedQty = UnitConversionHelper.ConvertQuantity(ingredientQty, recipe.portionUnit, whIngredient.PortionUnit);
                                ingredientDeductions[whIngredient.Id] = ingredientDeductions.GetValueOrDefault(whIngredient.Id) + convertedQty;
                            }
                        }
                    }
                }
            }

            foreach (var (whIngredientId, totalNeeded) in ingredientDeductions)
            {
                if (whIngredientsById.TryGetValue(whIngredientId, out var whIngredient))
                {
                    if (whIngredient.StockQuantity < totalNeeded)
                    {
                        string unitName = whIngredient.PortionUnit.HasValue
                            ? PortionUnitHelper.GetDisplayName(whIngredient.PortionUnit.Value)
                            : string.Empty;

                        throw new InvalidOperationException(
                            $"كمية غير كافية من المادة الخام '{whIngredient.Name}' في المستودع. المتوفر: {whIngredient.StockQuantity:N0} {unitName}، المطلوب للطلب: {totalNeeded:N0} {unitName}".Trim());
                    }
                }
            }

            // =========================================================================
            // PHASE 2: EXECUTION (تنفيذ الخصم الفعلي بعد اجتياز التحقق بنجاح 100%)
            // =========================================================================

            foreach (var item in orderItems)
            {
                if (item.ProductId.HasValue && productMap.TryGetValue(item.ProductId.Value, out var product))
                {
                    decimal deduction = PortionSizeHelper.CalculateDeduction(item.Size, item.Quantity);
                    decimal factor = deduction > 0 ? deduction : item.Quantity;

                    // 1. حساب وحفظ التكلفة التاريخية للبند (UnitCost & TotalCost) عند تنفيذ الطلب لحساب COGS بدقة تامة
                    decimal unitItemCost = 0m;
                    if (product.Recipe != null && product.Recipe.Any())
                    {
                        foreach (var r in product.Recipe)
                        {
                            if (r.Quantity <= 0) continue;
                            WarehouseFromProduct? whProd = null;
                            if (r.WarehouseFromProductId.HasValue && whIngredientsById.TryGetValue(r.WarehouseFromProductId.Value, out var foundById))
                                whProd = foundById;
                            else if (!string.IsNullOrWhiteSpace(r.Name) && whIngredientsMap.TryGetValue(r.Name.Trim(), out var foundByName))
                                whProd = foundByName;

                            if (whProd != null && whProd.Price > 0)
                            {
                                decimal converted = UnitConversionHelper.ConvertQuantity(r.Quantity, r.portionUnit, whProd.PortionUnit);
                                if (r.IsMainIngredient)
                                {
                                    int yield = product.YieldRate > 0 ? product.YieldRate : 1;
                                    unitItemCost += (converted * whProd.Price) / yield;
                                }
                                else
                                {
                                    unitItemCost += converted * whProd.Price;
                                }
                            }
                        }
                    }
                    else
                    {
                        unitItemCost = product.BasePrice > 0 ? product.BasePrice : 0m;
                    }

                    item.UnitCost = Math.Round(unitItemCost, 2);
                    item.TotalCost = Math.Round(unitItemCost * factor, 2);
                    _unitOfWork.OrdersItem.Update(item);

                    if (!product.IsUntrackedStock)
                    {
                        product.TotalValue -= deduction;

                        if (product.PersonNumberFood > 0)
                            product.StockQuantity = product.TotalValue / product.PersonNumberFood;
                        else
                            product.StockQuantity = product.TotalValue;

                        product.UpdatedAt = now;
                        _unitOfWork.Products.Update(product);
                    }

                    // Deduct recipe raw materials from warehouse ONLY if product was not already batch cooked
                    bool isBatchCookedProduct = product.Recipe != null && (
                        product.Recipe.Any(r => r.IsMainIngredient) || 
                        product.YieldRate > 0 || 
                        product.AvailableQtyInPOS > 0
                    );

                    if (!isBatchCookedProduct && product.Recipe != null && product.Recipe.Any())
                    {
                        foreach (var recipe in product.Recipe)
                        {
                            if (recipe.Quantity <= 0) continue;
                            if (recipe.IsMainIngredient) continue;

                            WarehouseFromProduct? whIngredient = null;
                            if (recipe.WarehouseFromProductId.HasValue && whIngredientsById.TryGetValue(recipe.WarehouseFromProductId.Value, out var foundById))
                            {
                                whIngredient = foundById;
                            }
                            else if (!string.IsNullOrWhiteSpace(recipe.Name) && whIngredientsMap.TryGetValue(recipe.Name.Trim(), out var foundByName))
                            {
                                whIngredient = foundByName;
                            }

                            if (whIngredient != null)
                            {
                                decimal ingredientQty = recipe.Quantity * factor;
                                decimal convertedQty = UnitConversionHelper.ConvertQuantity(ingredientQty, recipe.portionUnit, whIngredient.PortionUnit);
                                
                                decimal beforeQty = whIngredient.StockQuantity;
                                whIngredient.StockQuantity -= convertedQty;
                                decimal afterQty = whIngredient.StockQuantity;
                                whIngredient.UpdatedAt = now;
                                _unitOfWork.warehouseFromProduct.Update(whIngredient);

                                // تسجيل حركة استهلاك المخزون (StockMovement) بدقة رياضية متطابقة
                                await _unitOfWork.StockMovements.AddAsync(new StockMovement
                                {
                                    WarehouseFromProductId = whIngredient.Id,
                                    WarehouseId = whIngredient.WarehouseId,
                                    MovementType = StockMovementType.Sale,
                                    Quantity = -convertedQty,
                                    BalanceBefore = beforeQty,
                                    BalanceAfter = afterQty,
                                    UnitCost = whIngredient.Price,
                                    TotalCost = convertedQty * whIngredient.Price,
                                    OrderId = orderId ?? (item.OrderId > 0 ? item.OrderId : null),
                                    ReferenceNumber = orderNumber ?? (orderId.HasValue ? $"ORD-{orderId.Value}" : null),
                                    UserId = userId,
                                    Notes = $"استهلاك مكونات الوجبة '{product.Name}' للطلب #{orderNumber ?? orderId?.ToString() ?? item.OrderId.ToString()}",
                                    MovementDate = now,
                                    CreatedAt = now
                                });
                            }
                        }
                    }
                }
                else if (item.AddonId.HasValue && addonMap.TryGetValue(item.AddonId.Value, out var ordAddon))
                {
                    item.UnitCost = ordAddon.BuyingPrice;
                    item.TotalCost = Math.Round(ordAddon.BuyingPrice * item.Quantity, 2);
                    _unitOfWork.OrdersItem.Update(item);
                }

                if (item.addonAndOrderItems != null && item.addonAndOrderItems.Any())
                {
                    foreach (var linkedAddonItem in item.addonAndOrderItems)
                    {
                        if (addonMap.TryGetValue(linkedAddonItem.AddonId, out var linkedAddon))
                            DeductAddonStock(linkedAddon, item.Quantity, addonMap, now);
                    }
                }

                if (item.AddonId.HasValue && addonMap.TryGetValue(item.AddonId.Value, out var addon))
                    DeductAddonStock(addon, item.Quantity, addonMap, now);
            }
        }

        public async Task RestoreStockForOrderItemsAsync(IEnumerable<OrderItem> orderItems, int? orderId = null, string? orderNumber = null, int? userId = null)
        {
            var productIds = orderItems
                .Where(i => i.ProductId.HasValue)
                .Select(i => i.ProductId!.Value)
                .Distinct()
                .ToList();

            var products = await _unitOfWork.Products
                .Include(p => p.Recipe)
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();
            var productMap = products.ToDictionary(p => p.Id);

            var addonIds = orderItems
                .Where(i => i.AddonId.HasValue)
                .Select(i => i.AddonId!.Value)
                .ToList();
            addonIds.AddRange(orderItems
                .SelectMany(i => i.addonAndOrderItems ?? new List<AddonAndOrderItem>())
                .Select(a => a.AddonId));
            addonIds = addonIds.Distinct().ToList();

            var addonsList = (await _unitOfWork.Addons.FindAsync(a => addonIds.Contains(a.Id))).ToList();
            var addonMap = addonsList.ToDictionary(a => a.Id);

            var missingBaseAddonIds = addonsList
                .Where(a => a.BaseAddonId.HasValue && a.BaseAddonQuantity > 0 && a.DeductOnSale && !addonMap.ContainsKey(a.BaseAddonId.Value))
                .Select(a => a.BaseAddonId!.Value)
                .Distinct()
                .ToList();

            if (missingBaseAddonIds.Count > 0)
            {
                var baseAddons = await _unitOfWork.Addons.FindAsync(a => missingBaseAddonIds.Contains(a.Id));
                foreach (var ba in baseAddons)
                    addonMap[ba.Id] = ba;
            }

            var allRestoreRecipeIds = products
                .Where(p => p.Recipe != null)
                .SelectMany(p => p.Recipe)
                .Where(r => r.WarehouseFromProductId.HasValue)
                .Select(r => r.WarehouseFromProductId!.Value)
                .Distinct()
                .ToList();

            var allRestoreRecipeNames = products
                .Where(p => p.Recipe != null)
                .SelectMany(p => p.Recipe)
                .Where(r => !string.IsNullOrWhiteSpace(r.Name) && r.Quantity > 0)
                .Select(r => r.Name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var whRestoreIngredients = (allRestoreRecipeIds.Any() || allRestoreRecipeNames.Any())
                ? await _unitOfWork.warehouseFromProduct.FindAsync(wp => wp.IsActive && (allRestoreRecipeIds.Contains(wp.Id) || allRestoreRecipeNames.Contains(wp.Name)))
                : Enumerable.Empty<WarehouseFromProduct>();

            var whRestoreById = whRestoreIngredients.ToDictionary(w => w.Id);
            var whRestoreMap = new Dictionary<string, WarehouseFromProduct>(StringComparer.OrdinalIgnoreCase);
            foreach (var whp in whRestoreIngredients)
            {
                if (!string.IsNullOrWhiteSpace(whp.Name) && !whRestoreMap.ContainsKey(whp.Name.Trim()))
                    whRestoreMap[whp.Name.Trim()] = whp;
            }

            var now = DateTime.Now;

            foreach (var item in orderItems)
            {
                if (item.ProductId.HasValue && productMap.TryGetValue(item.ProductId.Value, out var product))
                {
                    decimal addition = PortionSizeHelper.CalculateDeduction(item.Size, item.Quantity);
                    if (!product.IsUntrackedStock)
                    {
                        product.TotalValue += addition;

                        if (product.PersonNumberFood > 0)
                            product.StockQuantity = product.TotalValue / product.PersonNumberFood;
                        else
                            product.StockQuantity = product.TotalValue;
                    }

                    product.UpdatedAt = now;
                    _unitOfWork.Products.Update(product);

                    // Restore recipe raw materials back to warehouse ONLY if product was not batch-cooked
                    bool isBatchCookedProduct = product.Recipe != null && (
                        product.Recipe.Any(r => r.IsMainIngredient) || 
                        product.YieldRate > 0 || 
                        product.AvailableQtyInPOS > 0
                    );

                    if (!isBatchCookedProduct && product.Recipe != null && product.Recipe.Any())
                    {
                        foreach (var recipe in product.Recipe)
                        {
                            if (recipe.Quantity <= 0) continue;
                            if (recipe.IsMainIngredient) continue;

                            WarehouseFromProduct? whIngredient = null;
                            if (recipe.WarehouseFromProductId.HasValue && whRestoreById.TryGetValue(recipe.WarehouseFromProductId.Value, out var foundById))
                            {
                                whIngredient = foundById;
                            }
                            else if (!string.IsNullOrWhiteSpace(recipe.Name) && whRestoreMap.TryGetValue(recipe.Name.Trim(), out var foundByName))
                            {
                                whIngredient = foundByName;
                            }

                            if (whIngredient != null)
                            {
                                decimal factor = addition > 0 ? addition : item.Quantity;
                                decimal ingredientQty = recipe.Quantity * factor;
                                decimal convertedQty = UnitConversionHelper.ConvertQuantity(ingredientQty, recipe.portionUnit, whIngredient.PortionUnit);
                                decimal beforeQty = whIngredient.StockQuantity;
                                whIngredient.StockQuantity += convertedQty;
                                decimal afterQty = whIngredient.StockQuantity;
                                whIngredient.UpdatedAt = now;
                                _unitOfWork.warehouseFromProduct.Update(whIngredient);

                                // تسجيل حركة استعادة المرتجع في المخزون (StockMovement)
                                await _unitOfWork.StockMovements.AddAsync(new StockMovement
                                {
                                    WarehouseFromProductId = whIngredient.Id,
                                    WarehouseId = whIngredient.WarehouseId,
                                    MovementType = StockMovementType.SaleReturn,
                                    Quantity = convertedQty,
                                    BalanceBefore = beforeQty,
                                    BalanceAfter = afterQty,
                                    UnitCost = whIngredient.Price,
                                    TotalCost = convertedQty * whIngredient.Price,
                                    OrderId = orderId ?? (item.OrderId > 0 ? item.OrderId : null),
                                    ReferenceNumber = orderNumber ?? (orderId.HasValue ? $"ORD-{orderId.Value}" : null),
                                    UserId = userId,
                                    Notes = $"استعادة مكونات الوجبة '{product.Name}' من مرتجع الطلب #{orderNumber ?? orderId?.ToString() ?? item.OrderId.ToString()}",
                                    MovementDate = now,
                                    CreatedAt = now
                                });
                            }
                        }
                    }
                }

                if (item.addonAndOrderItems != null && item.addonAndOrderItems.Any())
                {
                    foreach (var linkedAddonItem in item.addonAndOrderItems)
                    {
                        if (addonMap.TryGetValue(linkedAddonItem.AddonId, out var linkedAddon))
                            RestoreAddonStock(linkedAddon, item.Quantity, addonMap, now);
                    }
                }

                if (item.AddonId.HasValue && addonMap.TryGetValue(item.AddonId.Value, out var addon))
                    RestoreAddonStock(addon, item.Quantity, addonMap, now);
            }
        }

        private void DeductAddonStock(Addon addon, int quantity, Dictionary<int, Addon> addonMap, DateTime now)
        {
            if (addon.BaseAddonId.HasValue && addon.BaseAddonQuantity > 0 && addon.DeductOnSale)
            {
                if (addonMap.TryGetValue(addon.BaseAddonId.Value, out var baseAddon))
                {
                    decimal totalDeduction = quantity * addon.BaseAddonQuantity;
                    baseAddon.Quantity -= totalDeduction;

                    if (baseAddon.Quantity < 0)
                        throw new InvalidOperationException(
                            $"كمية غير كافية من الإضافة الأساسية ({baseAddon.Name}) للمكون: {addon.Name}");

                    baseAddon.UpdatedAt = now;
                    _unitOfWork.Addons.Update(baseAddon);
                }
            }

            addon.Quantity -= quantity;
            addon.UpdatedAt = now;
            _unitOfWork.Addons.Update(addon);
        }

        private void RestoreAddonStock(Addon addon, int quantity, Dictionary<int, Addon> addonMap, DateTime now)
        {
            if (addon.BaseAddonId.HasValue && addon.BaseAddonQuantity > 0 && addon.DeductOnSale)
            {
                if (addonMap.TryGetValue(addon.BaseAddonId.Value, out var baseAddon))
                {
                    baseAddon.Quantity += quantity * addon.BaseAddonQuantity;
                    baseAddon.UpdatedAt = now;
                    _unitOfWork.Addons.Update(baseAddon);
                }
            }

            addon.Quantity += quantity;
            addon.UpdatedAt = now;
            _unitOfWork.Addons.Update(addon);
        }
    }
}
