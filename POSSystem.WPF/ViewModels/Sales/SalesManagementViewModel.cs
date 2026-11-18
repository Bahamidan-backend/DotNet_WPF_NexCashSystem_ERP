using POSSystem.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using POSSystem.WPF.Business;
using POSSystem.WPF.Business.Helpers;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Data;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;
using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.StaticFunction;

namespace POSSystem.WPF.ViewModels.Sales
{
    public partial class SalesManagementViewModel : BaseViewModel, POSSystem.WPF.Services.ISearchable
    {
        public void ApplySearch(string query)
        {
            SalesManagementProberty.CustomerSearchText = query;
        }
        public event EventHandler<Order>? OrderDetailsRequested;
        public event EventHandler<Order>? NavigateToReturnsManagementRequested;

        public SalesManagementProberty SalesManagementProberty { get; }

        private readonly Debouncer _filterDebouncer = new Debouncer(500);

        public SalesManagementViewModel()
        {
            var sp = POSSystem.WPF.App.ServiceProvider;
            _unitOfWork = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Data.UnitOfWork.IUnitOfWork>(sp);
            _orderService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IOrderService>(sp);
            _returnOrderService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.IReturnOrderService>(sp);
            SalesManagementProberty = new SalesManagementProberty();
            SalesManagementProberty.OnProductCategoryOrSearchChanged = () => ApplyProductAndCategoryBreakdownFilter();
            Commands = new SalesManagementCommands(this);

            ServiceLocator.ReturnOrderService.ReturnProcessed += OnReturnProcessed;

            SalesManagementProberty.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName is nameof(SalesManagementProberty.ReportStartDate) or 
                                      nameof(SalesManagementProberty.ReportEndDate) or 
                                      nameof(SalesManagementProberty.CustomerSearchText) or 
                                      nameof(SalesManagementProberty.PaymentMethodFilter) or
                                      nameof(SalesManagementProberty.StatusFilter))
                {
                    _filterDebouncer.RunAsync(() => LoadAllAsync()).SafeFireAndForget();
                }
            };

            LoadAllAsync().SafeFireAndForget();
        }

        private void OnReturnProcessed(object? sender, EventArgs e)
        {
            LoadAllAsync().SafeFireAndForget();
        }

        public override void Dispose()
        {
            if (_isDisposed) return;
            
            ServiceLocator.ReturnOrderService.ReturnProcessed -= OnReturnProcessed;

            base.Dispose();
        }

        private List<Order>? _cachedOrdersForCurrentLoad;

        public async Task LoadAllAsync()
        {
            var start = SalesManagementProberty.ReportStartDate.Date;
            var end = SalesManagementProberty.ReportEndDate.Date.AddDays(1).AddTicks(-1);

            _unitOfWork.ClearEntityTracking();
            _cachedOrdersForCurrentLoad = (await _orderService.GetOrdersByDateRangeAsync(start, end)).ToList();

            var orderIds = _cachedOrdersForCurrentLoad.Select(o => o.Id).ToList();
            var debts = (await _unitOfWork.CustomerDebts.FindAsync(d => d.OrderId != null && orderIds.Contains(d.OrderId.Value))).ToList();
            await AttachCustomersToDebtsAsync(debts);
            var debtMap = debts
                .Where(d => d.OrderId.HasValue)
                .GroupBy(d => d.OrderId!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var order in _cachedOrdersForCurrentLoad)
                EnrichOrderForDisplay(order, debtMap);

            await ExecuteLoadSalesLogAsync();
            await ExecuteLoadSalesReportAsync();
            await ExecuteLoadReturnsAsync();
            await ExecuteLoadProductSalesReportAsync(start, end);

            _cachedOrdersForCurrentLoad = null;
        }


        private List<ProductSalesReportItem> _allProductSalesCache = new();

        public async Task ExecuteLoadProductSalesReportAsync(DateTime start, DateTime end)
        {
            try
            {
                var db = _unitOfWork.GetContext();
                var items = await POSSystem.WPF.ViewModels.Reports.GeneralReportsViewModel.LoadProductSalesBreakdownForRangeAsync(db, start, end);
                _allProductSalesCache = items ?? new List<ProductSalesReportItem>();

                // Populate category filter options from DB Categories + sold items
                var dbCategories = await db.Categories.AsNoTracking().Where(c => c.IsActive).Select(c => c.Name).ToListAsync();
                var allCatNames = dbCategories
                    .Concat(_allProductSalesCache.Select(x => x.CategoryName))
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(c => c.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => c)
                    .ToList();

                string prevCat = SalesManagementProberty.SelectedProductCategoryFilter;
                SalesManagementProberty.ProductCategoriesFilterList.Clear();
                SalesManagementProberty.ProductCategoriesFilterList.Add("الكل (جميع الأقسام)");
                foreach (var cat in allCatNames)
                {
                    SalesManagementProberty.ProductCategoriesFilterList.Add(cat);
                }

                if (!string.IsNullOrWhiteSpace(prevCat) && SalesManagementProberty.ProductCategoriesFilterList.Contains(prevCat))
                    SalesManagementProberty.SelectedProductCategoryFilter = prevCat;
                else
                    SalesManagementProberty.SelectedProductCategoryFilter = "الكل (جميع الأقسام)";

                ApplyProductAndCategoryBreakdownFilter();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading product sales report: {ex.Message}");
            }
        }

        public void ApplyProductAndCategoryBreakdownFilter()
        {
            try
            {
                var source = _allProductSalesCache ?? new List<ProductSalesReportItem>();

                // 1. Build CategorySalesBreakdown (ملخص جرد الأقسام: الأرز، الدجاج، المشكلات...)
                var categoryGroups = source
                    .GroupBy(x => string.IsNullOrWhiteSpace(x.CategoryName) ? "أصناف عامة" : x.CategoryName.Trim())
                    .Select(g =>
                    {
                        var topItemsDetails = g
                            .OrderByDescending(i => i.TotalSoldQuantity)
                            .Select(i => $"{i.ProductName}: {i.TotalSoldQuantity:0.##} {i.UnitLabel}")
                            .ToList();

                        string primaryUnit = g.Select(i => i.UnitLabel).FirstOrDefault() ?? "حبة / نفر";
                        decimal totalCatQty = Math.Round(g.Sum(i => i.TotalSoldQuantity), 2);

                        return new CategorySalesSummaryItem
                        {
                            CategoryName = g.Key,
                            ProductsCount = g.Count(),
                            TotalSoldQuantity = totalCatQty,
                            TotalSoldQuantityText = $"{totalCatQty:0.##} ({primaryUnit})",
                            ProductsDetailsSummaryText = string.Join("  •  ", topItemsDetails),
                            OrdersCount = g.Sum(i => i.OrdersCount),
                            TotalRevenue = g.Sum(i => i.TotalRevenue)
                        };
                    })
                    .OrderByDescending(c => c.TotalSoldQuantity)
                    .ThenByDescending(c => c.TotalRevenue)
                    .ToList();

                SalesManagementProberty.CategorySalesBreakdown.Clear();
                foreach (var c in categoryGroups)
                {
                    SalesManagementProberty.CategorySalesBreakdown.Add(c);
                }

                // 2. Filter detailed ProductSalesBreakdown by selected category and search query
                IEnumerable<ProductSalesReportItem> filtered = source;
                var selCat = SalesManagementProberty.SelectedProductCategoryFilter;
                if (!string.IsNullOrWhiteSpace(selCat) && selCat != "الكل (جميع الأقسام)")
                {
                    filtered = filtered.Where(x => string.Equals(x.CategoryName?.Trim(), selCat.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                var q = SalesManagementProberty.ProductSalesSearchText?.Trim();
                if (!string.IsNullOrWhiteSpace(q))
                {
                    filtered = filtered.Where(x =>
                        (x.ProductName != null && x.ProductName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                        (x.CategoryName != null && x.CategoryName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                        (x.SizesBreakdownText != null && x.SizesBreakdownText.Contains(q, StringComparison.OrdinalIgnoreCase)));
                }

                SalesManagementProberty.ProductSalesBreakdown.Clear();
                foreach (var item in filtered)
                {
                    SalesManagementProberty.ProductSalesBreakdown.Add(item);
                }

                SalesManagementProberty.NotifyProductSalesChanged();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error filtering product/category sales report: {ex.Message}");
            }
        }

        public async Task ExecuteLoadSalesLogAsync()
        {
            try
            {
                var orders = await LoadFilteredOrdersAsync();
                SalesManagementProberty.SalesLogPager.SetItems(orders);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحميل سجل المبيعات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task ExecuteLoadSalesReportAsync()
        {
            try
            {
                var orders = await LoadFilteredOrdersAsync(includeStatusFilter: false);
                var completedSales = SalesMetricsHelper.FilterCountableSales(orders).ToList();

                SalesManagementProberty.SalesReportPager.SetItems(completedSales);
                SalesManagementProberty.SalesReport.Clear();
                foreach (var o in completedSales) SalesManagementProberty.SalesReport.Add(o);
                SalesManagementProberty.TotalSalesAmount = SalesMetricsHelper.SumGrossSales(completedSales);

                await LoadReturnTotalsAsync();
                UpdateWalletSummaries(completedSales);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحميل تقرير المبيعات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateWalletSummaries(List<Order> completedSales)
        {
            SalesManagementProberty.WalletSummaries.Clear();

            var depositOrders = completedSales
                .Where(o => o.PaymentMethod == PaymentMethod.MobilePayment || o.Notes?.Contains("[إيداع:") == true)
                .ToList();

            if (depositOrders.Count == 0)
            {
                SalesManagementProberty.HasWalletSummaries = false;
                SalesManagementProberty.TotalDepositSummaryText = string.Empty;
                return;
            }

            var groups = depositOrders
                .Select(o => new { Order = o, Deposit = o.DepositInfo })
                .GroupBy(x => new
                {
                    Bank = !string.IsNullOrWhiteSpace(x.Deposit?.BankOrWallet)
                        ? x.Deposit.BankOrWallet.Trim()
                        : (!string.IsNullOrWhiteSpace(x.Order.DepositBankOrWallet) ? x.Order.DepositBankOrWallet : "إيداع عام"),
                    Currency = !string.IsNullOrWhiteSpace(x.Deposit?.Currency)
                        ? x.Deposit.Currency.Trim()
                        : (!string.IsNullOrWhiteSpace(x.Order.DepositCurrency) ? x.Order.DepositCurrency : "ريال يمني")
                })
                .OrderBy(g => g.Key.Bank)
                .ThenBy(g => g.Key.Currency);

            int totalCount = 0;
            foreach (var group in groups)
            {
                decimal sum = group.Sum(x => (x.Deposit != null && x.Deposit.Amount > 0) ? x.Deposit.Amount : x.Order.Total);
                int count = group.Count();
                totalCount += count;

                SalesManagementProberty.WalletSummaries.Add(new WalletDepositSummary
                {
                    BankOrWallet = group.Key.Bank,
                    Currency = group.Key.Currency,
                    TotalAmount = sum,
                    OrderCount = count
                });
            }

            SalesManagementProberty.HasWalletSummaries = SalesManagementProberty.WalletSummaries.Count > 0;
            SalesManagementProberty.TotalDepositSummaryText = $"إجمالي الإيداعات: {totalCount} عملية";

            UpdateWalletFilterOptions(depositOrders);
        }

        private void UpdateWalletFilterOptions(List<Order> depositOrders)
        {
            var wallets = depositOrders
                .Select(o => o.DepositInfo?.BankOrWallet?.Trim())
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Distinct()
                .ToList();

            foreach (var wallet in wallets)
            {
                if (wallet == null) continue;
                var filterItem = $"إيداع - {wallet}";
                if (!SalesManagementProberty.PaymentMethods.Contains(filterItem))
                {
                    SalesManagementProberty.PaymentMethods.Add(filterItem);
                }
            }
        }

        public async Task ExecuteLoadReturnsAsync()
        {
            try
            {
                var start = SalesManagementProberty.ReportStartDate.Date;
                var end = SalesManagementProberty.ReportEndDate.Date.AddDays(1).AddTicks(-1);
                var returns = (await _unitOfWork.ReturnOrders.FindAsync(r => r.ReturnDate >= start && r.ReturnDate <= end))
                    .OrderByDescending(r => r.ReturnDate)
                    .ToList();

                var search = SalesManagementProberty.CustomerSearchText?.Trim();
                if (!string.IsNullOrWhiteSpace(search))
                {
                    returns = returns.Where(r =>
                        (r.OriginalOrder?.OrderNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (r.ReturnNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                        .ToList();
                }

                SalesManagementProberty.ReturnsPager.SetItems(returns);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحميل مرتجعات المبيعات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadReturnTotalsAsync()
        {
            var start = SalesManagementProberty.ReportStartDate.Date;
            var end = SalesManagementProberty.ReportEndDate.Date.AddDays(1).AddTicks(-1);

            var returnsInRange = await _unitOfWork.ReturnOrders.FindAsync(r => r.ReturnDate >= start && r.ReturnDate <= end);

            SalesManagementProberty.TotalReturnAmount = returnsInRange.Sum(r => r.TotalAmount);
        }

        private async Task<List<Order>> LoadFilteredOrdersAsync(bool includeStatusFilter = true)
        {
            List<Order> orders;
            if (_cachedOrdersForCurrentLoad != null)
            {
                orders = _cachedOrdersForCurrentLoad.ToList();
            }
            else
            {
                var start = SalesManagementProberty.ReportStartDate.Date;
                var end = SalesManagementProberty.ReportEndDate.Date.AddDays(1).AddTicks(-1);

                _unitOfWork.ClearEntityTracking();
                orders = (await _orderService.GetOrdersByDateRangeAsync(start, end)).ToList();

                var orderIds = orders.Select(o => o.Id).ToList();
                var debts = (await _unitOfWork.CustomerDebts.FindAsync(d => d.OrderId != null && orderIds.Contains(d.OrderId.Value))).ToList();
                await AttachCustomersToDebtsAsync(debts);
                var debtMap = debts
                    .Where(d => d.OrderId.HasValue)
                    .GroupBy(d => d.OrderId!.Value)
                    .ToDictionary(g => g.Key, g => g.First());

                foreach (var order in orders)
                    EnrichOrderForDisplay(order, debtMap);
            }

            var search = SalesManagementProberty.CustomerSearchText?.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                orders = orders.Where(o =>
                    (o.OrderNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (o.CustomerDisplayName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (o.User?.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
            }

            if (includeStatusFilter)
                orders = ApplyStatusFilter(orders, SalesManagementProberty.StatusFilter);

            orders = ApplyPaymentMethodFilter(orders, SalesManagementProberty.PaymentMethodFilter);

            return orders.OrderByDescending(o => o.CreatedAt).ToList();
        }

        private static List<Order> ApplyPaymentMethodFilter(List<Order> orders, string paymentMethodFilter)
        {
            if (string.IsNullOrWhiteSpace(paymentMethodFilter) || paymentMethodFilter == "الكل")
                return orders;

            var walletList = new[]
            {
                "حاسب كريمي", "محفظة جيب", "محفظة فلوسك", "محفظة ون كاش",
                "محفظة جوالي", "محفظة موبايل موني", "محفظة يمن والت"
            };

            if (walletList.Any(w => paymentMethodFilter.Contains(w, StringComparison.OrdinalIgnoreCase)))
            {
                return orders.Where(o => (o.PaymentMethod == PaymentMethod.MobilePayment || o.Notes?.Contains("[إيداع:") == true) &&
                                         ((o.DepositInfo?.BankOrWallet?.Contains(paymentMethodFilter, StringComparison.OrdinalIgnoreCase) == true) ||
                                          (o.Notes?.Contains(paymentMethodFilter, StringComparison.OrdinalIgnoreCase) == true))).ToList();
            }

            if (paymentMethodFilter.StartsWith("إيداع - ", StringComparison.OrdinalIgnoreCase))
            {
                var wallet = paymentMethodFilter.Substring("إيداع - ".Length).Trim();
                return orders.Where(o => (o.PaymentMethod == PaymentMethod.MobilePayment || o.Notes?.Contains("[إيداع:") == true) &&
                                         ((o.DepositInfo?.BankOrWallet?.Equals(wallet, StringComparison.OrdinalIgnoreCase) == true) ||
                                          (o.Notes?.Contains(wallet, StringComparison.OrdinalIgnoreCase) == true))).ToList();
            }

            return paymentMethodFilter switch
            {
                "نقدي (كاش)" => orders.Where(o => o.PaymentMethod == PaymentMethod.Cash).ToList(),
                "بطاقة (شبكة/فيزا)" => orders.Where(o => o.PaymentMethod == PaymentMethod.CreditCard || o.PaymentMethod == PaymentMethod.DebitCard).ToList(),
                "تحويل بنكي" => orders.Where(o => o.PaymentMethod == PaymentMethod.BankTransfer).ToList(),
                "آجل (دين)" => orders.Where(o => o.PaymentMethod == PaymentMethod.OnCredit).ToList(),
                "دفع إلكتروني" or "إيداع / محفظة" or "إيداع / دفع إلكتروني" or "إيداع عام (جميع المحافظ)" => orders.Where(o => o.PaymentMethod == PaymentMethod.MobilePayment || o.Notes?.Contains("[إيداع:") == true).ToList(),
                _ => orders
            };
        }

        private static List<Order> ApplyStatusFilter(List<Order> orders, string statusFilter)
        {
            return statusFilter switch
            {
                "مكتمل" => orders.Where(o => o.Status == OrderStatus.Completed).ToList(),
                "ملغي" => orders.Where(o => o.Status == OrderStatus.Cancelled).ToList(),
                "مرتجع" => orders.Where(o => o.Status is OrderStatus.Returned or OrderStatus.PartiallyReturned).ToList(),
                "آجل (دين)" => orders.Where(o => o.PaymentMethod == PaymentMethod.OnCredit).ToList(),
                "خصم" => orders.Where(o => o.Discount > 0).ToList(),
                _ => orders
            };
        }

        private async Task AttachCustomersToDebtsAsync(List<CustomerDebt> debts)
        {
            if (debts.Count == 0) return;

            var customerIds = debts.Select(d => d.CustomerId).Distinct().ToList();
            var customers = (await _unitOfWork.Customers.FindAsync(c => customerIds.Contains(c.Id)))
                .ToDictionary(c => c.Id);

            foreach (var debt in debts)
            {
                if (customers.TryGetValue(debt.CustomerId, out var customer))
                    debt.Customer = customer;
            }
        }

        private static void EnrichOrderForDisplay(Order order, Dictionary<int, CustomerDebt> debtMap)
        {
            if (order.OrderType == "delivery" && !string.IsNullOrWhiteSpace(order.Delivery?.DeliveryCustomerName))
                order.CustomerDisplayName = order.Delivery.DeliveryCustomerName;
            else if (order.PaymentMethod == PaymentMethod.OnCredit && debtMap.TryGetValue(order.Id, out var debt))
            {
                var customerName = debt.Customer?.Name ?? debt.CustomerName;
                order.CustomerDisplayName = string.IsNullOrWhiteSpace(customerName) ? "عميل آجل" : customerName;
            }
            else
                order.CustomerDisplayName = "عميل مباشر";

            order.IsHighRisk = order.Status is OrderStatus.Returned
                or OrderStatus.PartiallyReturned
                or OrderStatus.Cancelled;

            order.HasDiscountWarning = order.Discount > 0 && !order.IsHighRisk;

            order.RiskLabel = order.Status switch
            {
                OrderStatus.Cancelled => "ملغي",
                OrderStatus.Returned => "مرتجع كامل",
                OrderStatus.PartiallyReturned => "مرتجع جزئي",
                _ when order.Discount > 0 => "خصم",
                _ when order.PaymentMethod == PaymentMethod.OnCredit => "آجل",
                _ => string.Empty
            };
        }

        public void ViewOrderDetails(Order? order)
        {
            if (order != null)
                OrderDetailsRequested?.Invoke(this, order);
        }

        public void ReprintOrder(Order? order)
        {
            if (order == null) return;
            _ = Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                GloublStatic.PrintReceipt(order);
            }));
            MessageBox.Show($"تم إعادة طباعة فاتورة {order.OrderNumber}",
                "طباعة", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void InitiateReturn(Order? order)
        {
            if (order == null) return;

            if (order.Status is OrderStatus.Cancelled or OrderStatus.Returned)
            {
                MessageBox.Show("لا يمكن إجراء مرتجع على فاتورة ملغاة أو مرتجعة بالكامل.",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NavigateToReturnsManagementRequested?.Invoke(this, order);
        }
    }
}
