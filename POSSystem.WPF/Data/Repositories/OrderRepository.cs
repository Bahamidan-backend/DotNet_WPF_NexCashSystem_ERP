

namespace POSSystem.WPF.Data.Repositories
{
    using Microsoft.EntityFrameworkCore;
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        public OrderRepository(POSDbContext context) : base(context)
        {
        }

        private IQueryable<Order> GetBaseOrderQuery(bool asNoTracking = false, bool includeRelations = true)
        {
            IQueryable<Order> query = _dbSet;
            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            if (includeRelations)
            {
                return query
                    .Include(o => o.User)
                    .Include(o => o.Delivery)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                            .ThenInclude(p => p.Category)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Addon)
                            .ThenInclude(a => a.Category)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.addonAndOrderItems)
                            .ThenInclude(aoi => aoi.Addon)
                    .AsSplitQuery();
            }

            return query;
        }

        public async Task<IEnumerable<Order>> GetOrdersWithItemsAsync()
        {
            return await GetBaseOrderQuery(asNoTracking: true)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<PagedResult<Order>> GetOrdersWithItemsPagedAsync(int pageNumber, int pageSize)
        {
            var totalCount = await _dbSet.AsNoTracking().CountAsync();
            var items = await GetBaseOrderQuery(asNoTracking: true)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return new PagedResult<Order>(items, totalCount, pageNumber, pageSize);
        }


        public async Task<IEnumerable<Order>> GetOrdersWithPymentMethodeAsync(PaymentMethod paymentMethod)
        {
            return await GetBaseOrderQuery(asNoTracking: true)
                .Where(x=>x.PaymentMethod == paymentMethod)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetOrdersByUserAsync(int userId)
        {
            return await GetBaseOrderQuery(asNoTracking: true)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<PagedResult<Order>> GetOrdersByUserPagedAsync(int userId, int pageNumber, int pageSize)
        {
            var totalCount = await _dbSet.AsNoTracking().Where(o => o.UserId == userId).CountAsync();
            var items = await GetBaseOrderQuery(asNoTracking: true)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return new PagedResult<Order>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate, bool includeRelations = true)
        {
            return await GetBaseOrderQuery(asNoTracking: true, includeRelations: includeRelations)
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<PagedResult<Order>> GetOrdersByDateRangePagedAsync(DateTime startDate, DateTime endDate, int pageNumber, int pageSize)
        {
            var totalCount = await _dbSet.AsNoTracking().Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate).CountAsync();
            var items = await GetBaseOrderQuery(asNoTracking: true)
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return new PagedResult<Order>(items, totalCount, pageNumber, pageSize);
        }


        public async Task<IEnumerable<Order>> GetOrdersByDateRangeAddonsAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.Delivery)
                .Include(o => o.OrderItems).ThenInclude(x => x.addonAndOrderItems)
                .AsSplitQuery()
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }



        public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status)
        {
            return await GetBaseOrderQuery(asNoTracking: true)
                .Where(o => o.Status == status)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<PagedResult<Order>> GetOrdersByStatusPagedAsync(OrderStatus status, int pageNumber, int pageSize)
        {
            var totalCount = await _dbSet.AsNoTracking().Where(o => o.Status == status).CountAsync();
            var items = await GetBaseOrderQuery(asNoTracking: true)
                .Where(o => o.Status == status)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return new PagedResult<Order>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<Order?> GetOrderWithItemsAsync(int orderId)
        {
            return await GetBaseOrderQuery()
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<string> GenerateUniqueOrderNumberAsync(int userId)
        {
            // Fast path (PERF-0001): Use indexed Max(Id) and recent window instead of materializing 100k historical strings
            int maxId = await _dbSet.AsNoTracking().Select(o => (int?)o.Id).MaxAsync() ?? 0;
            var recentNumbers = await _dbSet
                .AsNoTracking()
                .OrderByDescending(o => o.Id)
                .Take(500)
                .Where(o => o.OrderNumber != null && o.OrderNumber != "")
                .Select(o => o.OrderNumber)
                .ToListAsync();

            int maxSequential = maxId;
            foreach (var numStr in recentNumbers)
            {
                if (int.TryParse(numStr.Trim(), out int plainNum) && plainNum > maxSequential)
                {
                    maxSequential = plainNum;
                }
            }

            int nextNumber = maxSequential + 1;
            while (await _dbSet.AsNoTracking().AnyAsync(o => o.OrderNumber == nextNumber.ToString()))
            {
                nextNumber++;
            }

            return nextNumber.ToString();
        }

        public async Task<bool> OrderNumberExistsAsync(string orderNumber)
        {
            return await _dbSet.AnyAsync(o => o.OrderNumber == orderNumber);
        }

        public async Task<IEnumerable<Order>> GetTodayOrdersAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            
            return await GetBaseOrderQuery(asNoTracking: true)
                .Where(o => o.CreatedAt >= today && o.CreatedAt < tomorrow)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalSalesByDateAsync(DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);
            
            var totals = await _dbSet
                .AsNoTracking()
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt < endDate &&
                           (o.Status == OrderStatus.Completed ||
                            o.Status == OrderStatus.Returned ||
                            o.Status == OrderStatus.PartiallyReturned))
                .Select(o => o.Total)
                .ToListAsync();
            return totals.Sum();
        }

        // Projection methods for lists - avoid N+1 by using Select instead of Include
        public async Task<PagedResult<OrderListDto>> GetOrdersListPagedAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? customerSearch = null,
            OrderStatus? status = null,
            PaymentMethod? paymentMethod = null,
            int page = 1,
            int pageSize = 20)
        {
            var query = _dbSet.AsNoTracking();

            if (startDate.HasValue) query = query.Where(o => o.CreatedAt >= startDate.Value);
            if (endDate.HasValue) query = query.Where(o => o.CreatedAt < endDate.Value.AddDays(1));
            if (status.HasValue) query = query.Where(o => o.Status == status.Value);
            if (paymentMethod.HasValue) query = query.Where(o => o.PaymentMethod == paymentMethod.Value);
            
            if (!string.IsNullOrWhiteSpace(customerSearch))
            {
                query = query.Where(o => o.Delivery != null && 
                    (o.Delivery.DeliveryCustomerName.Contains(customerSearch) || 
                     o.Delivery.DeliveryPhone.Contains(customerSearch)));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderListDto(
                    o.Id,
                    o.OrderNumber,
                    o.CreatedAt,
                    o.Total,
                    o.Status,
                    o.User!.Name,
                    o.ShiftId,
                    o.Shift != null ? o.Shift.Name : "",
                    o.PaymentMethod,
                    o.OrderItems.Count,
                    o.Delivery != null ? o.Delivery.DeliveryCustomerName : "عميل مباشر",
                    o.PaymentMethod == PaymentMethod.Cash ? "نقدي (كاش)" : 
                    o.PaymentMethod == PaymentMethod.DebitCard ? "شبكة (مدى)" : 
                    o.PaymentMethod == PaymentMethod.CreditCard ? "بطاقة ائتمان" : 
                    o.PaymentMethod == PaymentMethod.MobilePayment ? "دفع إلكتروني" : 
                    o.PaymentMethod == PaymentMethod.OnCredit ? "آجل (دين)" : 
                    o.PaymentMethod == PaymentMethod.BankTransfer ? "تحويل بنكي" : o.PaymentMethod.ToString(),
                    o.PaymentMethod == PaymentMethod.OnCredit && o.Status != OrderStatus.Completed ? "دين غير مسدد" : "",
                    o.PaymentMethod == PaymentMethod.OnCredit && o.Status != OrderStatus.Completed
                ))
                .ToListAsync();

            return new PagedResult<OrderListDto>(items, totalCount, page, pageSize);
        }

        public async Task<OrderDetailDto?> GetOrderDetailAsync(int orderId, CancellationToken ct = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(o => o.Id == orderId)
                .Select(o => new OrderDetailDto(
                    o.Id,
                    o.OrderNumber,
                    o.CreatedAt,
                    o.SubTotal,
                    o.ServiceCharge,
                    o.Discount,
                    o.Total,
                    o.Status,
                    o.PaymentMethod,
                    o.Notes,
                    o.UserId,
                    o.User!.Name,
                    o.ShiftId,
                    o.Shift != null ? o.Shift.Name : null,
                    o.Delivery != null ? new DeliveryDto(
                        o.Delivery.DeliveryId,
                        o.Delivery.DeliveryCustomerName!,
                        o.Delivery.DeliveryPhone!,
                        o.Delivery.DeliveryAddress!,
                        o.Delivery.DeliveryDriverName,
                        o.Delivery.DeliveryFee
                    ) : null,
                    o.OrderItems.Select(oi => new OrderItemDto(
                        oi.Id,
                        oi.ProductId,
                        oi.Product != null ? oi.Product.Name : null,
                        oi.AddonId,
                        oi.Addon != null ? oi.Addon.Name : null,
                        oi.Quantity,
                        oi.UnitPrice,
                        oi.TotalPrice,
                        oi.Size,
                        oi.Notes,
                         oi.addonAndOrderItems.Where(ai => ai.Addon != null).Select(ai => new AddonDto(ai.AddonId, ai.Addon!.Name, ai.Addon.Price)).ToList()
                    )).ToList()
                ))
                .FirstOrDefaultAsync(ct);
        }
    }
}
