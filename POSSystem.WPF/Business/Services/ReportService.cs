using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business.Helpers;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;
using POSSystem.WPF.Models.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace POSSystem.WPF.Business.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISaleService _saleService;
        private readonly IOrderService _orderService;
        private readonly IUserService _userService;
        private readonly IWarehouseService _warehouseService;
        private readonly ICashBoxTransactionService _cashBoxTransactionService;
        private readonly IShiftService _shiftService;
        private readonly ICustomerDebtService _customerDebtService;
        private readonly IAddonService _addonService;

        public ReportService(IUnitOfWork unitOfWork, 
                             ISaleService saleService, 
                             IOrderService orderService, 
                             IUserService userService, 
                             IWarehouseService warehouseService, 
                             ICashBoxTransactionService cashBoxTransactionService,
                             IShiftService shiftService,
                             ICustomerDebtService customerDebtService,
                             IAddonService addonService)
        {
            _unitOfWork = unitOfWork;
            _saleService = saleService;
            _orderService = orderService;
            _userService = userService;
            _warehouseService = warehouseService;
            _cashBoxTransactionService = cashBoxTransactionService;
            _shiftService = shiftService;
            _customerDebtService = customerDebtService;
            _addonService = addonService;
        }

        public async Task<IEnumerable<DailySalesReport>> GetDailySalesReportsAsync(DateTime startDate, DateTime endDate)
        {
            var reports = new List<DailySalesReport>();
            var orders = await _orderService.GetOrdersByDateRangeAsync(startDate, endDate.AddDays(1).AddSeconds(-1), false);
            var salesOrders = SalesMetricsHelper.FilterCountableSales(orders).ToList();

            var groupedOrders = salesOrders.GroupBy(o => o.CreatedAt.Date).ToDictionary(g => g.Key, g => g.ToList());
            var allDates = groupedOrders.Keys.OrderByDescending(d => d);

            foreach (var date in allDates)
            {
                var dayOrders = groupedOrders.ContainsKey(date) ? groupedOrders[date] : new List<Order>();

                int invoiceCount = dayOrders.Count;
                var (cashSales, cardSales, debtSales) = SalesMetricsHelper.SumByPaymentMethod(dayOrders);
                decimal totalRevenue = SalesMetricsHelper.SumGrossSales(dayOrders);
                decimal avgSale = invoiceCount > 0 ? totalRevenue / invoiceCount : 0;

                reports.Add(new DailySalesReport
                {
                    Date = date,
                    InvoiceCount = invoiceCount,
                    TotalRevenue = totalRevenue,
                    CashSales = cashSales,
                    CardSales = cardSales,
                    DebtSales = debtSales,
                    AverageSaleAmount = avgSale
                });
            }
            return reports;
        }

        public async Task<IEnumerable<CashierSalesReport>> GetCashierSalesReportsAsync(DateTime startDate, DateTime endDate)
        {
            var reports = new List<CashierSalesReport>();
            
            // Use Orders only
            var orders = (await _orderService.GetOrdersByDateRangeAsync(startDate, endDate.AddDays(1).AddSeconds(-1), false)).ToList();
            var salesOrders = SalesMetricsHelper.FilterCountableSales(orders).ToList();

            if (salesOrders.Any())
            {
                var groupedOrders = salesOrders.GroupBy(o => (int?)o.UserId).ToDictionary(g => g.Key, g => g.ToList());
                var activeUserIds = groupedOrders.Keys.Where(id => id.HasValue).Select(id => id!.Value).ToList();
                var userNamesById = await _unitOfWork.GetContext().Users
                    .AsNoTracking()
                    .Where(u => activeUserIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.Name })
                    .ToDictionaryAsync(u => u.Id, u => u.Name);

                var allUserIds = groupedOrders.Keys;

                foreach (var userId in allUserIds)
                {
                    var userOrders = groupedOrders.ContainsKey(userId) ? groupedOrders[userId] : new List<Order>();
                    
                    var user = userId.HasValue && userNamesById.TryGetValue(userId.Value, out var name)
                        ? new User { Id = userId.Value, Name = name }
                        : null;

                    var (cashSales, cardSales, debtSales) = SalesMetricsHelper.SumByPaymentMethod(userOrders);
                    var totalRevenue = SalesMetricsHelper.SumGrossSales(userOrders);
                    var totalCount = SalesMetricsHelper.CountCountableSales(userOrders);
                    var avgSale = totalCount > 0 ? totalRevenue / totalCount : 0;

                    reports.Add(new CashierSalesReport
                    {
                        UserId = userId ?? 0,
                        CashierName = user?.Name ?? "غير معروف",
                        InvoiceCount = totalCount,
                        TotalRevenue = totalRevenue,
                        CashSales = cashSales,
                        CardSales = cardSales,
                        DebtSales = debtSales,
                        AverageSaleAmount = avgSale
                    });
                }
                return reports.OrderByDescending(r => r.TotalRevenue);
            }
            
            return reports;
        }

        public async Task<IEnumerable<DeliveryReport>> GetDeliveryReportsAsync(DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var endExclusive = endDate.Date.AddDays(1);

            var ordersList = await _unitOfWork.GetContext().Orders
                .AsNoTracking()
                .Include(o => o.Delivery)
                .Where(o => o.OrderType == "delivery" && o.CreatedAt >= start && o.CreatedAt < endExclusive)
                .ToListAsync();

            var reports = new List<DeliveryReport>();

            var groupedByDriver = ordersList.GroupBy(o => 
                !string.IsNullOrWhiteSpace(o.Delivery?.DeliveryDriverName) 
                    ? o.Delivery.DeliveryDriverName.Trim() 
                    : "بدون سائق محدد");

            foreach (var group in groupedByDriver)
            {
                var driverOrders = group.ToList();
                var countableOrders = SalesMetricsHelper.FilterCountableSales(driverOrders).ToList();
                var totalValue = SalesMetricsHelper.SumGrossSales(countableOrders);
                var completed = driverOrders.Count(o => o.Status == OrderStatus.Completed);
                var pending = driverOrders.Count(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled);
                var count = driverOrders.Count; 
                var avg = count > 0 ? totalValue / count : 0;
                var totalFees = driverOrders.Sum(o => o.Delivery?.DeliveryFee ?? 0);

                var firstOrder = driverOrders.FirstOrDefault();

                reports.Add(new DeliveryReport
                {
                    UserId = firstOrder?.UserId ?? 0,
                    DeliveryPersonName = group.Key,
                    TotalOrders = count,
                    TotalOrderValue = totalValue,
                    CompletedOrders = completed,
                    PendingOrders = pending,
                    AverageOrderValue = avg,
                    TotalDeliveryFees = totalFees
                });
            }

            return reports.OrderByDescending(r => r.TotalOrders);
        }

        public async Task<IEnumerable<CashierProfitReport>> GetCashierProfitReportsAsync(DateTime startDate, DateTime endDate)
        {
            var reports = new List<CashierProfitReport>();
            
            // Use Orders with relations included to accurately calculate COGS
            var orders = await _orderService.GetOrdersByDateRangeAsync(startDate, endDate.AddDays(1).AddSeconds(-1), true);
            var ordersList = SalesMetricsHelper.FilterCountableSales(orders).ToList();

            if (!ordersList.Any()) return reports;

            var allUserIds = ordersList.Where(o => o.UserId > 0).Select(o => o.UserId).Distinct().ToList();
            var soldProductIds = ordersList
                .Where(o => o.OrderItems != null)
                .SelectMany(o => o.OrderItems)
                .Where(oi => oi.ProductId.HasValue)
                .Select(oi => oi.ProductId!.Value)
                .Distinct()
                .ToList();

            var context = _unitOfWork.GetContext();

            // Fetch only relevant users with AsNoTracking
            var userNamesById = await context.Users
                .AsNoTracking()
                .Where(u => allUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Name })
                .ToDictionaryAsync(u => u.Id, u => u.Name);

            // Fetch only relevant sold products (Id, Name, TypeWarehoseFromProduct, BasePrice, YieldRate) with AsNoTracking
            var relevantProducts = await context.Products
                .AsNoTracking()
                .Where(p => soldProductIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Name, p.TypeWarehoseFromProduct, p.BasePrice, p.YieldRate })
                .ToListAsync();

            var allWarehouseProducts = await context.warehouseFromProducts.AsNoTracking().ToListAsync();
            var warehouseEntityById = allWarehouseProducts.ToDictionary(w => w.Id);

            // Fast O(1) indexed lookups
            var warehouseProductByName = allWarehouseProducts
                .Where(w => !string.IsNullOrWhiteSpace(w.Name))
                .GroupBy(w => w.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var warehouseByName = allWarehouseProducts
                .Where(w => !string.IsNullOrWhiteSpace(w.Name))
                .GroupBy(w => w.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Price, StringComparer.OrdinalIgnoreCase);

            var warehouseById = allWarehouseProducts
                .ToDictionary(w => w.Id, w => w.Price);

            // جلب وصفات المنتجات المباعة لحساب تكلفة المكونات الحقيقية (BOM) بدقة
            var relevantProductIds = relevantProducts.Select(p => p.Id).ToList();
            var productRecipes = await context.Recipes
                .AsNoTracking()
                .Where(r => relevantProductIds.Contains(r.ProductId))
                .Select(r => new { r.ProductId, r.WarehouseFromProductId, r.Name, r.Quantity, r.portionUnit, r.IsMainIngredient })
                .ToListAsync();

            var recipesByProductId = productRecipes
                .GroupBy(r => r.ProductId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Create a fast O(1) lookup dictionary for cost prices
            var productCostCache = new Dictionary<int, decimal>();
            foreach (var product in relevantProducts)
            {
                decimal cost = 0;

                // أولاً: احتساب تكلفة مكونات الوصفة إن وجدت
                if (recipesByProductId.TryGetValue(product.Id, out var recipes) && recipes.Count > 0)
                {
                    decimal unitRecipeCost = 0;
                    foreach (var r in recipes)
                    {
                        if (r.Quantity <= 0) continue;
                        WarehouseFromProduct? whProd = null;
                        if (r.WarehouseFromProductId.HasValue && warehouseEntityById.TryGetValue(r.WarehouseFromProductId.Value, out whProd))
                        {
                        }
                        else if (!string.IsNullOrWhiteSpace(r.Name) && warehouseProductByName.TryGetValue(r.Name.Trim(), out var byName))
                        {
                            whProd = byName;
                        }

                        int yield = product.YieldRate > 0 ? product.YieldRate : 1;

                        if (whProd != null && whProd.Price > 0)
                        {
                            decimal convertedQty = UnitConversionHelper.ConvertQuantity(r.Quantity, r.portionUnit, whProd.PortionUnit);
                            if (r.IsMainIngredient)
                            {
                                unitRecipeCost += (convertedQty * whProd.Price) / yield;
                            }
                            else
                            {
                                unitRecipeCost += convertedQty * whProd.Price;
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(r.Name) && warehouseByName.TryGetValue(r.Name.Trim(), out var ingPrice) && ingPrice > 0)
                        {
                            if (r.IsMainIngredient)
                            {
                                unitRecipeCost += (r.Quantity * ingPrice) / yield;
                            }
                            else
                            {
                                unitRecipeCost += r.Quantity * ingPrice;
                            }
                        }
                    }
                    if (unitRecipeCost > 0)
                        cost = unitRecipeCost;
                }

                if (cost == 0 && !string.IsNullOrWhiteSpace(product.TypeWarehoseFromProduct))
                {
                    var trimmedType = product.TypeWarehoseFromProduct.Trim();
                    if (int.TryParse(trimmedType, out int wpId) && warehouseById.TryGetValue(wpId, out var priceById) && priceById > 0)
                        cost = priceById;
                    else if (warehouseByName.TryGetValue(trimmedType, out var priceByName) && priceByName > 0)
                        cost = priceByName;
                }

                if (cost == 0 && !string.IsNullOrWhiteSpace(product.Name))
                {
                    var trimmedName = product.Name.Trim();
                    if (warehouseByName.TryGetValue(trimmedName, out var priceByName) && priceByName > 0)
                        cost = priceByName;
                }

                if (cost == 0 && product.BasePrice > 0)
                {
                    cost = product.BasePrice;
                }

                productCostCache[product.Id] = cost;
            }

            var groupedOrders = ordersList.GroupBy(o => (int?)o.UserId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var kvp in groupedOrders)
            {
                var userId = kvp.Key;
                var userOrders = kvp.Value;
                var user = userId.HasValue && userNamesById.TryGetValue(userId.Value, out var name)
                    ? new User { Id = userId.Value, Name = name }
                    : null;

                decimal totalRevenue = 0;
                decimal totalCost = 0;

                foreach (var order in userOrders)
                {
                    totalRevenue += order.Total;

                    if (order.OrderItems != null)
                    {
                        foreach (var item in order.OrderItems)
                        {
                            decimal deduction = PortionSizeHelper.CalculateDeduction(item.Size, item.Quantity);
                            decimal factor = deduction > 0 ? deduction : item.Quantity;

                            if (item.UnitCost > 0)
                            {
                                totalCost += item.UnitCost * factor;
                            }
                            else
                            {
                                decimal costPrice = 0;
                                if (item.ProductId.HasValue && productCostCache.TryGetValue(item.ProductId.Value, out var cachedCost))
                                {
                                    costPrice = cachedCost;
                                }
                                totalCost += costPrice * factor;
                            }
                        }
                    }
                }

                var grossProfit = totalRevenue - totalCost;
                var profitPercentage = totalRevenue > 0 ? (grossProfit / totalRevenue) * 100 : 0;

                reports.Add(new CashierProfitReport
                {
                    UserId = userId ?? 0,
                    CashierName = user?.Name ?? "غير معروف",
                    TotalRevenue = totalRevenue,
                    TotalCost = totalCost,
                    GrossProfit = grossProfit,
                    NetProfit = grossProfit,
                    ProfitPercentage = profitPercentage,
                    InvoiceCount = userOrders.Count
                });
            }

            return reports.OrderByDescending(r => r.ProfitPercentage);
        }



        public async Task<IEnumerable<CashBoxReport>> GetCashBoxReportsAsync(DateTime startDate, DateTime endDate)
        {
            var reports = new List<CashBoxReport>();
            var transactions = await _cashBoxTransactionService.GetTransactionsByDateRangeAsync(startDate, endDate.AddDays(1).AddSeconds(-1));
            
            foreach (var transaction in transactions)
            {
                var cashierName = transaction.Shift?.User?.Name ?? transaction.User?.Name ?? "غير معروف";
                var shiftNumber = transaction.Shift?.ShiftNumber ?? "-";

                reports.Add(new CashBoxReport
                {
                    TransactionId = transaction.Id,
                    ShiftId = transaction.ShiftId,
                    ShiftNumber = shiftNumber,
                    CashierName = cashierName,
                    TransactionType = transaction.TypeText,
                    Amount = transaction.Amount,
                    Description = transaction.Description,
                    TransactionDate = transaction.TransactionDate,
                    TransactionTime = transaction.TransactionDate.ToString("HH:mm:ss"),
                    IsPersonal = transaction.IsPersonal,
                    ExpenseTypeText = transaction.Type switch
                    {
                        CashBoxTransactionType.Expense => (transaction.IsPersonal ? "شخصي (ينقص من الراتب)" : "للمطعم"),
                        CashBoxTransactionType.BankDeposit => "إيداع بنكي",
                        CashBoxTransactionType.Advance => "سلفة موظف",
                        CashBoxTransactionType.Debt => "دين",
                        CashBoxTransactionType.Income => "-",
                        _ => "-"
                    },
                    Notes = transaction.Notes ?? string.Empty
                });
            }
            return reports;
        }

        public async Task<IEnumerable<ShiftReport>> GetShiftReportsAsync(DateTime startDate, DateTime endDate)
        {
            var reports = new List<ShiftReport>();
            var startBound = startDate.Date;
            var endBound = endDate.Date.AddDays(1);
            var shiftList = await _unitOfWork.GetContext().Shifts
                .AsNoTracking()
                .Include(s => s.CashBoxTransactions)
                .Where(s => s.StartTime >= startBound && s.StartTime < endBound)
                .ToListAsync();
            var shiftUserIds = shiftList.Where(s => s.UserId > 0).Select(s => s.UserId).Distinct().ToList();
            var userNamesById = await _unitOfWork.GetContext().Users
                .AsNoTracking()
                .Where(u => shiftUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Name })
                .ToDictionaryAsync(u => u.Id, u => u.Name);

            // CRITICAL OPTIMIZATION: Fetch ALL orders in the date range ONCE with items for addon breakdown
            var maxEndDate = shiftList.Any() ? shiftList.Max(s => s.EndTime ?? DateTime.Today).AddDays(1) : endDate.AddDays(1);
            var allOrdersInRange = await _orderService.GetOrdersByDateRangeAsync(startDate.Date, maxEndDate, true);

            // FIX PERF-02: Filter paid debts by date range at DB level instead of loading all historical data
            var allPaidDebtsInRange = await _customerDebtService.GetPaidDebtsInRangeAsync(startDate.Date, endDate.Date);

            // FIX PERF-03: Filter return orders by date range at DB level with index-friendly bounds
            var allReturnsInRange = (await _unitOfWork.ReturnOrders.FindAsync(
                r => r.ReturnDate >= startBound && r.ReturnDate < endBound))
                .ToList();

            foreach (var shift in shiftList)
            {
                var user = shift.UserId > 0 && userNamesById.TryGetValue(shift.UserId, out var name)
                    ? new User { Id = shift.UserId, Name = name }
                    : null;
                
                var shiftOrders = allOrdersInRange
                    .Where(o => o.UserId == shift.UserId &&
                                o.CreatedAt >= shift.StartTime &&
                                (shift.EndTime == null || o.CreatedAt <= shift.EndTime))
                    .ToList();

                var salesOrders = SalesMetricsHelper.FilterCountableSales(shiftOrders).ToList();

                var shiftReturns = allReturnsInRange
                    .Where(r => r.CashierId == shift.UserId && r.ReturnDate >= shift.StartTime && (shift.EndTime == null || r.ReturnDate <= shift.EndTime))
                    .ToList();

                var netSalesOrders = shiftOrders.Where(o => o.Status == OrderStatus.Completed || 
                                                            o.Status == OrderStatus.PartiallyReturned).ToList();

                var paidDebts = allPaidDebtsInRange
                    .Where(d => d.ShiftId == shift.Id)
                    .ToList();

                var netSalesOrderItems = netSalesOrders.SelectMany(o => o.OrderItems).ToList();
                
                decimal shiftAddonsRevenue = 0;
                int shiftAddonsSold = 0;

                foreach (var item in netSalesOrderItems)
                {
                    // 1. Standalone Addon (sold as a main item)
                    if (item.AddonId.HasValue || (item.Product != null && (item.Product.CategoryName == "إضافات" || item.Product.Category?.Name == "إضافات")))
                    {
                        shiftAddonsSold += item.Quantity;
                        shiftAddonsRevenue += item.TotalPrice;
                    }

                    // 2. Linked Addons (Modifiers attached to a product)
                    if (item.addonAndOrderItems != null && item.addonAndOrderItems.Any())
                    {
                        foreach (var linkedAddon in item.addonAndOrderItems)
                        {
                            if (linkedAddon.Addon != null)
                            {
                                shiftAddonsSold += item.Quantity;
                                shiftAddonsRevenue += linkedAddon.Addon.Price * item.Quantity;
                            }
                        }
                    }
                }

                var nonSettlementTxs = shift.CashBoxTransactions?
                    .Where(t => t.Notes == null || (!t.Notes.Contains("[تسوية عجز مناوبة]") && !t.Notes.Contains("[تسوية زيادة مناوبة]")))
                    .ToList() ?? new List<CashBoxTransaction>();
                var expectedEndingCash = shift.StartingCash + 
                    nonSettlementTxs.Where(t => t.Type == CashBoxTransactionType.Income).Sum(t => t.Amount) -
                    nonSettlementTxs.Where(t => t.Type != CashBoxTransactionType.Income).Sum(t => t.Amount);
                var cashDiff = shift.EndingCash - expectedEndingCash;
                var diffText = !shift.EndTime.HasValue ? "جارية" : (cashDiff < 0 ? $"عجز ({Math.Abs(cashDiff):N0})" : (cashDiff > 0 ? $"زيادة ({cashDiff:N0})" : "مطابق"));

                var report = new ShiftReport
                {
                    ShiftNumber = shift.ShiftNumber ?? "-",
                    StartingCash = shift.StartingCash,
                    EndingCash = shift.EndingCash,
                    ExpectedEndingCash = expectedEndingCash,
                    CashDifference = cashDiff,
                    DifferenceStatusText = diffText,
                    EmployeeName = user?.Name ?? "غير معروف",
                    StartTime = shift.StartTime,
                    EndTime = shift.EndTime ?? DateTime.Now,
                    TotalSales = SalesMetricsHelper.SumGrossSales(salesOrders),
                    CashReceived = shift.EndTime.HasValue ? shift.EndingCash : (netSalesOrders.Where(o => o.PaymentMethod == PaymentMethod.Cash).Sum(o => o.Total) + paidDebts.Sum(d => d.Amount)),
                    TotalDebtsCreated = netSalesOrders.Where(o => o.PaymentMethod == PaymentMethod.OnCredit).Sum(o => o.Total),
                    InvoiceCount = salesOrders.Count,
                    DiscountCodesUsed = salesOrders.Count(o => o.Discount > 0),
                    TotalDiscountAmount = salesOrders.Where(o => o.Discount > 0).Sum(o => o.Discount),
                    AddonsSold = shiftAddonsSold,
                    AddonsRevenue = shiftAddonsRevenue,
                    ProductsSold = salesOrders.SelectMany(o => o.OrderItems ?? new List<OrderItem>()).Sum(oi => oi.Quantity),
                    ReturnedOrdersCount = shiftReturns.Count,
                    ReturnedOrdersAmount = shiftReturns.Sum(r => r.TotalAmount),
                    ReturnDetails = shiftReturns.Select(r => new ReturnDetail
                    {
                        SaleId = r.OriginalOrderId,
                        InvoiceNumber = r.ReturnNumber,
                        ReturnAmount = r.TotalAmount,
                        ReturnDate = r.ReturnDate,
                        Reason = r.Reason
                    }).ToList()
                };
                reports.Add(report);
            }

            return reports.OrderByDescending(r => r.StartTime);
        }

        public async Task<ShiftReportDto> GetUserShiftReportAsync(User user)
        {
            var dto = new ShiftReportDto();

            // Load current active shift only
            var currentShift = await _shiftService.GetActiveShiftByUserIdAsync(user.Id);

            dto.Shift = currentShift;

            if (currentShift != null)
            {

                // Load cash box transactions
                var transactions = await _cashBoxTransactionService.GetTransactionsByShiftIdAsync(currentShift.Id);
                dto.Transactions = transactions.ToList();

                // OPTIMIZATION: Pre-fetch only users associated with this shift or its transactions
                var transactionUserIds = dto.Transactions
                    .Where(t => t.UserId.HasValue)
                    .Select(t => t.UserId!.Value)
                    .ToList();
                if (currentShift.UserId > 0) transactionUserIds.Add(currentShift.UserId);
                var distinctUserIds = transactionUserIds.Distinct().ToList();

                var userNamesDict = await _unitOfWork.GetContext().Users
                    .AsNoTracking()
                    .Where(u => distinctUserIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.Name })
                    .ToDictionaryAsync(u => u.Id, u => u.Name);

                // Build TransactionDetails
                foreach (var transaction in dto.Transactions)
                {
                    // Logic mapped from ShiftService.LoadCashBoxTransactionsByShiftAsync
                    var cashierName = "غير معروف";
                    
                    if (transaction.User != null && !string.IsNullOrWhiteSpace(transaction.User.Name))
                        cashierName = transaction.User.Name;
                    else if (transaction.Shift?.User != null && !string.IsNullOrWhiteSpace(transaction.Shift.User.Name))
                        cashierName = transaction.Shift.User.Name;
                    else if (transaction.UserId.HasValue)
                    {
                        if (userNamesDict.TryGetValue(transaction.UserId.Value, out var name))
                        {
                            cashierName = name;
                        }
                    }

                    dto.TransactionDetails.Add(new CashBoxTransactionDetail
                    {
                        TransactionId = transaction.Id,
                        Date = transaction.TransactionDate,
                        Time = transaction.TransactionDate.ToString("HH:mm:ss"),
                        CashierName = cashierName,
                        Type = transaction.TypeText ?? "غير محدد",
                        Amount = transaction.Amount,
                        Description = !string.IsNullOrWhiteSpace(transaction.Description) ? transaction.Description : transaction.Name ?? "بدون وصف",
                        IsPersonal = transaction.IsPersonal,
                        ExpenseType = transaction.IsPersonal ? "شخصي (ينقص من الراتب)" : (transaction.Type == CashBoxTransactionType.BankDeposit ? "إيداع في البنك" : "للمطعم"),
                        Notes = transaction.Notes ?? ""
                    });
                }

                dto.CashBoxTransactionsCount = dto.Transactions.Count;
                dto.TotalCashOut = dto.Transactions
                    .Where(t => t.Type == CashBoxTransactionType.Expense ||
                                t.Type == CashBoxTransactionType.Advance ||
                                t.Type == CashBoxTransactionType.Debt ||
                                t.Type == CashBoxTransactionType.BankDeposit)
                    .Sum(t => t.Amount);
                dto.HasCashOut = dto.TotalCashOut > 0;
                dto.CurrentCashInBox = await _cashBoxTransactionService.GetCashBoxBalanceAsync(currentShift.Id);
                dto.StartingCash = currentShift.StartingCash;
                dto.EndingCash = currentShift.EndingCash;
                dto.ExpectedEndingCash = dto.CurrentCashInBox;
                dto.CashDifference = currentShift.IsActive ? 0 : (currentShift.EndingCash - dto.ExpectedEndingCash);
                dto.DifferenceStatusText = currentShift.IsActive 
                    ? "مناوبة نشطة جارية" 
                    : (dto.CashDifference < 0 
                        ? $"عجز نقدي ({Math.Abs(dto.CashDifference):N0} ريال)" 
                        : (dto.CashDifference > 0 
                            ? $"فائض نقدي ({dto.CashDifference:N0} ريال)" 
                            : "متطابق تماماً"));

                // Load orders
                var queryStart = currentShift.StartTime.Date;
                var queryEnd = (currentShift.EndTime?.Date ?? DateTime.Today).AddDays(1);

                var allOrders = await _orderService.GetOrdersByDateRangeAsync(queryStart, queryEnd);
                var shiftOrders = allOrders
                    .Where(o => o.UserId == user.Id &&
                                o.CreatedAt >= currentShift.StartTime &&
                                (currentShift.EndTime == null || o.CreatedAt <= currentShift.EndTime))
                    .ToList();
                
                dto.Orders = shiftOrders;

                var salesOrders = SalesMetricsHelper.FilterCountableSales(shiftOrders).ToList();

                var shiftReturns = (await _unitOfWork.ReturnOrders.FindAsync(r => r.CashierId == user.Id && r.ReturnDate >= queryStart && r.ReturnDate <= queryEnd)).ToList();

                var netSalesOrders = shiftOrders.Where(o => o.Status == OrderStatus.Completed || 
                                                            o.Status == OrderStatus.PartiallyReturned).ToList();

                var paidDebts = (await _unitOfWork.CustomerDebts.FindAsync(d => d.ShiftId == currentShift.Id && d.IsPaid)).ToList();

                dto.TotalInvoices = salesOrders.Count;
                dto.TotalSales = SalesMetricsHelper.SumGrossSales(salesOrders);
                var (cashSales, cardSales, creditSales) = SalesMetricsHelper.SumByPaymentMethod(salesOrders);
                dto.CashSales = cashSales;
                dto.CardSales = cardSales;
                dto.CreditSales = creditSales;
                dto.AverageInvoice = dto.TotalInvoices > 0 ? dto.TotalSales / dto.TotalInvoices : 0;

                dto.ReturnedOrdersCount = shiftReturns.Count;
                dto.ReturnedOrdersAmount = shiftReturns.Sum(r => r.TotalAmount);
                dto.TotalTax = salesOrders.Sum(o => o.Tax);

                dto.DiscountsUsed = salesOrders.Count(o => o.Discount > 0);
                dto.TotalDiscountAmount = salesOrders.Where(o => o.Discount > 0).Sum(o => o.Discount);

                // Addons 
                var netSalesOrderItems = netSalesOrders.SelectMany(o => o.OrderItems).ToList();
                
                decimal addonsRevenue = 0;
                int addonsSold = 0;

                foreach (var item in netSalesOrderItems)
                {
                    // 1. Standalone Addon (sold as a main item)
                    if (item.AddonId.HasValue || (item.Product != null && (item.Product.CategoryName == "إضافات" || item.Product.Category?.Name == "إضافات")))
                    {
                        addonsSold += item.Quantity;
                        addonsRevenue += item.TotalPrice;
                    }

                    // 2. Linked Addons (Modifiers attached to a product)
                    if (item.addonAndOrderItems != null && item.addonAndOrderItems.Any())
                    {
                        foreach (var linkedAddon in item.addonAndOrderItems)
                        {
                            // If Addon object is loaded (it should be due to GetOrdersByDateRangeAsync Includes)
                            if (linkedAddon.Addon != null)
                            {
                                addonsSold += item.Quantity;
                                addonsRevenue += linkedAddon.Addon.Price * item.Quantity;
                            }
                        }
                    }
                }

                dto.AddonsRevenue = addonsRevenue;
                dto.AddonsSold = addonsSold;

                 // Customer Debts
                 var shiftDebts = (await _unitOfWork.CustomerDebts.FindAsync(d => d.DebtDate >= queryStart && d.DebtDate <= queryEnd && !d.IsPaid)).ToList();
                 dto.CustomerDebtsCount = shiftDebts.Count;
                 dto.TotalCustomerDebts = shiftDebts.Sum(d => d.Amount);
            }

            return dto;
        }

        private async Task<IEnumerable<DateTime>> GetPaidDebtDatesAsync(DateTime startDate, DateTime endDate)
        {
            var start = startDate.Date;
            var endExclusive = endDate.Date.AddDays(1);

            return await _unitOfWork.GetContext().CustomerDebts
                .AsNoTracking()
                .Where(d => d.IsPaid && d.PaymentDate >= start && d.PaymentDate < endExclusive)
                .Select(d => d.PaymentDate!.Value.Date)
                .Distinct()
                .ToListAsync();
        }

        public async Task<CashierAuditSummaryDto> GetCashierAuditSummaryAsync(DateTime startDate, DateTime endDate, int? cashierId = null)
        {
            var summary = new CashierAuditSummaryDto
            {
                StartDate = startDate.Date,
                EndDate = endDate.Date
            };

            var queryStart = startDate.Date;
            var queryEnd = endDate.Date.AddDays(1);

            // Fetch orders in date range with items and relations
            var allOrders = (await _orderService.GetOrdersByDateRangeAsync(queryStart, queryEnd, true)).ToList();
            if (cashierId.HasValue && cashierId.Value > 0)
            {
                allOrders = allOrders.Where(o => o.UserId == cashierId.Value).ToList();
            }

            // Also fetch return orders in date range
            var returnOrders = (await _unitOfWork.ReturnOrders.FindAsync(r => r.ReturnDate >= queryStart && r.ReturnDate < queryEnd)).ToList();
            if (cashierId.HasValue && cashierId.Value > 0)
            {
                returnOrders = returnOrders.Where(r => r.CashierId == cashierId.Value).ToList();
            }

            // 1. Process Cancellations
            var cancelledOrders = allOrders.Where(o => o.Status == OrderStatus.Cancelled).OrderByDescending(o => o.UpdatedAt ?? o.CreatedAt).ToList();
            summary.CancelledOrdersCount = cancelledOrders.Count;
            summary.TotalCancelledAmount = cancelledOrders.Sum(o => o.Total);

            foreach (var order in cancelledOrders)
            {
                // Parse cancellation reason from Notes: e.g. [إلغاء الفاتورة بتاريخ ... - السبب: {reason}...]
                string reason = "بدون سبب مسجل";
                string cancelledBy = order.User?.Name ?? $"كاشير #{order.UserId}";
                if (!string.IsNullOrWhiteSpace(order.Notes))
                {
                    var lines = order.Notes.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (line.Contains("السبب:"))
                        {
                            var parts = line.Split(new[] { "السبب:" }, StringSplitOptions.None);
                            if (parts.Length > 1)
                            {
                                var rPart = parts[1].Split(new[] { " - بواسطة", "]" }, StringSplitOptions.None)[0].Trim();
                                if (!string.IsNullOrWhiteSpace(rPart))
                                {
                                    reason = rPart;
                                }
                            }
                        }
                        if (line.Contains("بواسطة مستخدم #"))
                        {
                            var parts = line.Split(new[] { "بواسطة مستخدم #" }, StringSplitOptions.None);
                            if (parts.Length > 1)
                            {
                                var userNum = parts[1].TrimEnd(']').Trim();
                                cancelledBy = $"مستخدم #{userNum}";
                            }
                        }
                    }
                }

                var itemsSummaryList = new List<string>();
                if (order.OrderItems != null && order.OrderItems.Any())
                {
                    foreach (var item in order.OrderItems)
                    {
                        string pName = item.Product?.Name ?? (item.Addon != null ? item.Addon.Name : "صنف");
                        itemsSummaryList.Add($"{item.Quantity}x {pName} ({item.TotalPrice:N0} ريال)");
                    }
                }
                string itemsSummary = itemsSummaryList.Count > 0 ? string.Join(" | ", itemsSummaryList) : "لا توجد أصناف مسجلة";

                var cancelTime = order.UpdatedAt ?? order.CreatedAt;
                var durationMinutes = (int)(cancelTime - order.CreatedAt).TotalMinutes;
                string durationText = durationMinutes <= 1 
                    ? "فور الطلب مباشرة" 
                    : $"بعد {durationMinutes} دقيقة من فتح الطلب";

                summary.Cancellations.Add(new CashierCancellationAuditDto
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    CreatedAt = order.CreatedAt,
                    CancelledAt = cancelTime,
                    TimeString = cancelTime.ToString("hh:mm tt"),
                    DateString = cancelTime.ToString("yyyy/MM/dd"),
                    CashierName = order.User?.Name ?? $"كاشير #{order.UserId}",
                    CustomerName = string.IsNullOrWhiteSpace(order.CustomerDisplayName) || order.CustomerDisplayName == "عميل مباشر"
                        ? (order.OrderTypeText ?? "عميل مباشر")
                        : order.CustomerDisplayName,
                    OrderTypeText = order.OrderTypeText ?? "محلي",
                    TotalAmount = order.Total,
                    ItemsSummary = itemsSummary,
                    Reason = reason,
                    DurationText = durationText,
                    CancelledBy = cancelledBy
                });
            }

            // 2. Process Discounts
            var discountOrders = allOrders.Where(o => o.Discount > 0 && o.Status != OrderStatus.Cancelled).OrderByDescending(o => o.CreatedAt).ToList();
            summary.DiscountInvoicesCount = discountOrders.Count;
            summary.TotalDiscountAmount = discountOrders.Sum(o => o.Discount);
            summary.MaxDiscountAmount = discountOrders.Count > 0 ? discountOrders.Max(o => o.Discount) : 0m;

            foreach (var order in discountOrders)
            {
                decimal subtotal = order.SubTotal > 0 ? order.SubTotal : (order.Total + order.Discount);
                decimal discountPct = subtotal > 0 ? (order.Discount / subtotal) * 100m : 0m;

                string notes = order.Notes ?? "";
                if (notes.Contains("Coupon:"))
                {
                    notes = notes.Replace("Coupon:", "كوبون: ");
                }

                summary.Discounts.Add(new CashierDiscountAuditDto
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    CreatedAt = order.CreatedAt,
                    TimeString = order.CreatedAt.ToString("hh:mm tt"),
                    DateString = order.CreatedAt.ToString("yyyy/MM/dd"),
                    CashierName = order.User?.Name ?? $"كاشير #{order.UserId}",
                    CustomerName = string.IsNullOrWhiteSpace(order.CustomerDisplayName) || order.CustomerDisplayName == "عميل مباشر"
                        ? (order.OrderTypeText ?? "عميل مباشر")
                        : order.CustomerDisplayName,
                    OrderTypeText = order.OrderTypeText ?? "محلي",
                    SubTotal = subtotal,
                    DiscountAmount = order.Discount,
                    DiscountPercentage = discountPct,
                    Total = order.Total,
                    PaymentMethodText = order.PaymentMethodText,
                    Notes = notes
                });
            }

            // 3. Process Returns
            summary.ReturnsCount = returnOrders.Count;
            summary.TotalReturnsAmount = returnOrders.Sum(r => r.TotalAmount);

            // 3.5 Process Cashier Leftover / Surplus Disposals (صرف الأكل الزايد والهدر من شاشة الكاشير)
            var context = _unitOfWork.GetContext();
            var leftoverQuery = context.StockWastes
                .AsNoTracking()
                .Include(sw => sw.ReportedByUser)
                .Include(sw => sw.WarehouseFromProduct)
                .Where(sw => sw.WasteDate >= queryStart && sw.WasteDate < queryEnd &&
                             (sw.Reason == WasteReason.StaffMeal ||
                              sw.Reason == WasteReason.Charity ||
                              (sw.Notes != null && sw.Notes.Contains("[صرف متبقي الكاشير:"))));

            if (cashierId.HasValue && cashierId.Value > 0)
            {
                leftoverQuery = leftoverQuery.Where(sw => sw.ReportedByUserId == cashierId.Value);
            }

            var leftoverWastes = await leftoverQuery
                .OrderByDescending(sw => sw.WasteDate)
                .ToListAsync();

            foreach (var sw in leftoverWastes)
            {
                string rawNotes = sw.Notes ?? string.Empty;
                string disposalType = sw.Reason switch
                {
                    WasteReason.StaffMeal => "غداء / إطعام العمال",
                    WasteReason.Charity => "صدقة / تبرع خيري",
                    _ => "هدر / تالف نهاية اليوم"
                };

                string productName = sw.WarehouseFromProduct?.Name ?? "وجبة";
                string balanceFlow = "-";
                string approvedBy = "بكلمة سر المناوبة";
                string extraNotes = string.Empty;

                // Parse structured tag: [صرف متبقي الكاشير: ...] الصنف: ... | الكمية المصروفة: ... (قبل: ... -> بعد: ...) | باعتماد: ... | ملاحظة: ...
                if (rawNotes.Contains("[صرف متبقي الكاشير:"))
                {
                    int startTag = rawNotes.IndexOf("[صرف متبقي الكاشير:") + "[صرف متبقي الكاشير:".Length;
                    int endTag = rawNotes.IndexOf("]", startTag);
                    if (endTag > startTag)
                    {
                        disposalType = rawNotes.Substring(startTag, endTag - startTag).Trim();
                    }
                }

                var segments = rawNotes.Split('|');
                foreach (var seg in segments)
                {
                    string s = seg.Trim();
                    int prodIdx = s.IndexOf("الصنف:");
                    if (prodIdx >= 0)
                    {
                        productName = s.Substring(prodIdx + "الصنف:".Length).Trim();
                    }
                    else if (s.StartsWith("الكمية المصروفة:"))
                    {
                        int parenStart = s.IndexOf('(');
                        int parenEnd = s.LastIndexOf(')');
                        if (parenStart >= 0 && parenEnd > parenStart)
                        {
                            balanceFlow = s.Substring(parenStart + 1, parenEnd - parenStart - 1).Replace("->", "←").Trim();
                        }
                    }
                    else if (s.StartsWith("باعتماد:"))
                    {
                        approvedBy = s.Substring("باعتماد:".Length).Trim();
                    }
                    else if (s.StartsWith("ملاحظة:"))
                    {
                        extraNotes = s.Substring("ملاحظة:".Length).Trim();
                    }
                }

                string badgeBg = "#EFF6FF";
                string badgeFg = "#1D4ED8";

                if (disposalType.Contains("عمال") || sw.Reason == WasteReason.StaffMeal)
                {
                    summary.StaffMealCount++;
                    summary.StaffMealQty += sw.Quantity;
                    summary.StaffMealCost += sw.TotalCost;
                    badgeBg = "#EFF6FF";
                    badgeFg = "#1D4ED8";
                }
                else if (disposalType.Contains("صدقة") || sw.Reason == WasteReason.Charity)
                {
                    summary.CharityCount++;
                    summary.CharityQty += sw.Quantity;
                    summary.CharityCost += sw.TotalCost;
                    badgeBg = "#ECFDF5";
                    badgeFg = "#047857";
                }
                else if (disposalType.Contains("ضيافة") || disposalType.Contains("آخر"))
                {
                    summary.HospitalityCount++;
                    summary.HospitalityQty += sw.Quantity;
                    summary.HospitalityCost += sw.TotalCost;
                    badgeBg = "#FEF3C7";
                    badgeFg = "#B45309";
                }
                else
                {
                    summary.EndOfDayWasteCount++;
                    summary.EndOfDayWasteQty += sw.Quantity;
                    summary.EndOfDayWasteCost += sw.TotalCost;
                    badgeBg = "#FEF2F2";
                    badgeFg = "#B91C1C";
                }

                summary.TotalLeftoverDisposalsCount++;
                summary.TotalLeftoverDisposalsQty += sw.Quantity;
                summary.TotalLeftoverDisposalsCost += sw.TotalCost;

                summary.LeftoverDisposals.Add(new CashierLeftoverDisposalAuditDto
                {
                    WasteId = sw.Id,
                    WasteDate = sw.WasteDate,
                    DateString = sw.WasteDate.ToString("yyyy/MM/dd"),
                    TimeString = sw.WasteDate.ToString("hh:mm tt"),
                    ProductName = productName,
                    DisposalType = disposalType,
                    BadgeBg = badgeBg,
                    BadgeFg = badgeFg,
                    Quantity = sw.Quantity,
                    BalanceFlowText = balanceFlow,
                    EstimatedCost = sw.TotalCost,
                    CashierName = sw.ReportedByUser?.Name ?? (sw.ReportedByUserId.HasValue ? $"كاشير #{sw.ReportedByUserId.Value}" : "الكاشير"),
                    ApprovedBy = approvedBy,
                    Notes = string.IsNullOrWhiteSpace(extraNotes) ? "تم الصرف بكلمة سر المناوبة" : extraNotes
                });
            }

            // 4. Cashier Risk Ranking / Comparison
            var allCashierIds = allOrders.Select(o => o.UserId)
                .Union(returnOrders.Select(r => r.CashierId))
                .Distinct()
                .ToList();

            var usersDict = (await _unitOfWork.Users.FindAsync(u => allCashierIds.Contains(u.Id)))
                .ToDictionary(u => u.Id, u => u.Name);

            foreach (var cid in allCashierIds)
            {
                var cOrders = allOrders.Where(o => o.UserId == cid).ToList();
                var cReturns = returnOrders.Where(r => r.CashierId == cid).ToList();

                var cDiscounts = cOrders.Where(o => o.Discount > 0 && o.Status != OrderStatus.Cancelled).ToList();
                var cCancelled = cOrders.Where(o => o.Status == OrderStatus.Cancelled).ToList();

                string cName = usersDict.TryGetValue(cid, out var name) ? name : $"كاشير #{cid}";

                // Risk calculation
                string riskLevel = "آمن ✅";
                string badgeColor = "#27ae60";

                int discCount = cDiscounts.Count;
                int cancelCount = cCancelled.Count;
                decimal discTotal = cDiscounts.Sum(o => o.Discount);
                decimal cancelTotal = cCancelled.Sum(o => o.Total);

                if (cancelCount >= 3 || discTotal >= 300m || cancelTotal >= 200m)
                {
                    riskLevel = "شبهة عالية 🚨";
                    badgeColor = "#e74c3c";
                }
                else if (cancelCount >= 1 || discCount >= 3 || discTotal >= 100m)
                {
                    riskLevel = "انتباه ⚠️";
                    badgeColor = "#f39c12";
                }

                summary.CashierRankings.Add(new CashierRiskRankingDto
                {
                    CashierId = cid,
                    CashierName = cName,
                    TotalOrders = cOrders.Count,
                    DiscountInvoicesCount = discCount,
                    TotalDiscountAmount = discTotal,
                    CancelledOrdersCount = cancelCount,
                    TotalCancelledAmount = cancelTotal,
                    ReturnsCount = cReturns.Count,
                    TotalReturnsAmount = cReturns.Sum(r => r.TotalAmount),
                    RiskLevel = riskLevel,
                    RiskBadgeColor = badgeColor
                });
            }

            summary.CashierRankings = summary.CashierRankings
                .OrderByDescending(r => r.CancelledOrdersCount * 50 + r.DiscountInvoicesCount * 20 + (int)r.TotalDiscountAmount)
                .ToList();

            // 5. Overall Security Assessment
            if (summary.CancelledOrdersCount == 0 && summary.DiscountInvoicesCount == 0 && summary.ReturnsCount == 0)
            {
                summary.SecurityStatus = "الوضع ممتاز ونظيف 100% ✅";
                summary.SecurityMessage = "لا توجد أي خصومات أو إلغاءات أو مرتجعات مسجلة في هذه الفترة.";
                summary.SecurityBadgeColor = "#27ae60";
            }
            else if (summary.CancelledOrdersCount > 3 || summary.TotalDiscountAmount > 500m || summary.TotalCancelledAmount > 300m)
            {
                summary.SecurityStatus = "🚨 تنبيه رقابي عالي - عمليات حساسة مرتفعة";
                summary.SecurityMessage = $"تم رصد {summary.CancelledOrdersCount} طلبات ملغاة بقيمة {summary.TotalCancelledAmount:N0} ريال، و {summary.DiscountInvoicesCount} عمليات خصم بقيمة {summary.TotalDiscountAmount:N0} ريال.";
                summary.SecurityBadgeColor = "#e74c3c";
            }
            else
            {
                summary.SecurityStatus = "⚠️ تنبيه رقابي اعتيادي - توجد عمليات حساسة";
                summary.SecurityMessage = $"تم تسجيل {summary.DiscountInvoicesCount} فواتير بخصومات ({summary.TotalDiscountAmount:N0} ريال) و {summary.CancelledOrdersCount} طلبات ملغاة ({summary.TotalCancelledAmount:N0} ريال).";
                summary.SecurityBadgeColor = "#f39c12";
            }

            return summary;
        }
    }
}
