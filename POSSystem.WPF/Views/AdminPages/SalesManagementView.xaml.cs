using POSSystem.WPF.Controls;
using POSSystem.WPF.Models;
using POSSystem.WPF.Services;
using POSSystem.WPF.ViewModels.Sales;
using POSSystem.WPF.Views;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace POSSystem.WPF.Views.AdminPages
{
    public partial class SalesManagementView : UserControl
    {
        private SalesManagementViewModel _viewModel;

        public SalesManagementView()
        {
            InitializeComponent();
            _viewModel = new SalesManagementViewModel();
            DataContext = _viewModel;

            _viewModel.OrderDetailsRequested += OnOrderDetailsRequested;
            _viewModel.NavigateToReturnsManagementRequested += OnNavigateToReturnsManagementRequested;
        }

        private void OnNavigateToReturnsManagementRequested(object? sender, Order order)
        {
            NavigationService.Instance.ShowReturnsManagementView(AdminDashboardWindow._currentUser, order.OrderNumber);
        }

        private void OnOrderDetailsRequested(object? sender, Order order)
        {
            var details = new SalesOrderDetailsControl();
            details.Initialize(order);
            details.CloseRequested += (_, _) => HideOverlay();
            ShowOverlay(details);
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Order order)
                _viewModel.ViewOrderDetails(order);
        }

        private void Reprint_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Order order)
                _viewModel.ReprintOrder(order);
        }

        private void Return_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Order order)
                _viewModel.InitiateReturn(order);
        }

        private void SaveReportPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var orders = _viewModel.SalesManagementProberty.SalesReportPager.SourceItems.Count > 0 
                    ? _viewModel.SalesManagementProberty.SalesReportPager.SourceItems.ToList() 
                    : _viewModel.SalesManagementProberty.SalesReport.ToList();
                var columnHeaders = new[] { "رقم الفاتورة", "التاريخ", "العميل", "طريقة الدفع", "المبلغ الإجمالي" };

                var summaryBuilder = new StringBuilder();
                summaryBuilder.AppendLine($"إجمالي المبيعات: {_viewModel.SalesManagementProberty.TotalSalesAmount:#,##0} ريال");
                summaryBuilder.AppendLine($"إجمالي المرتجعات: {_viewModel.SalesManagementProberty.TotalReturnAmount:#,##0} ريال");
                summaryBuilder.Append($"صافي الأرباح: {_viewModel.SalesManagementProberty.NetSalesAmount:#,##0} ريال");

                if (_viewModel.SalesManagementProberty.HasWalletSummaries)
                {
                    summaryBuilder.AppendLine();
                    summaryBuilder.AppendLine();
                    summaryBuilder.AppendLine("--- تفاصيل إيداعات المحافظ والبنوك ---");
                    foreach (var wallet in _viewModel.SalesManagementProberty.WalletSummaries)
                    {
                        summaryBuilder.AppendLine($"{wallet.BankOrWallet} ({wallet.Currency}): {wallet.TotalAmount:#,##0} ({wallet.DisplaySubtitle})");
                    }
                }

                Services.PrintService.PrintReport(
                    "تقرير المبيعات",
                    $"الفترة من: {_viewModel.SalesManagementProberty.ReportStartDate:yyyy/MM/dd} إلى: {_viewModel.SalesManagementProberty.ReportEndDate:yyyy/MM/dd}",
                    orders,
                    columnHeaders,
                    item =>
                    {
                        if (item == null) return new string[columnHeaders.Length];
                        var order = item as Order;
                        if (order == null) return new string[columnHeaders.Length];
                        return new[]
                        {
                            order.OrderNumber ?? "",
                            order.CreatedAt.ToString("yyyy/MM/dd"),
                            order.CustomerDisplayName ?? "",
                            order.PaymentMethodDisplay ?? "",
                            order.Total.ToString("#,##0") + " ريال"
                        };
                    },
                    summaryBuilder.ToString(),
                    preferPdf: true);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ التقرير: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveProductSalesInventoryReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var products = _viewModel.SalesManagementProberty.ProductSalesBreakdown.ToList();
                var categories = _viewModel.SalesManagementProberty.CategorySalesBreakdown.ToList();

                if (products.Count == 0)
                {
                    MessageBox.Show("لا توجد مبيعات أصناف خلال الفترة المحددة لتصديرها.", "معلومة", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var columnHeaders = new[]
                {
                    "القسم / الفئة",
                    "اسم الصنف / المنتج",
                    "إجمالي الكمية المباعة",
                    "تفصيل الأحجام المباعة",
                    "عدد الطلبات",
                    "إجمالي قيمة المبيعات"
                };

                var summaryBuilder = new StringBuilder();
                summaryBuilder.AppendLine($"الفترة: من {_viewModel.SalesManagementProberty.ReportStartDate:yyyy/MM/dd} إلى {_viewModel.SalesManagementProberty.ReportEndDate:yyyy/MM/dd}");
                summaryBuilder.AppendLine($"إجمالي عدد الأقسام المباعة: {categories.Count} قسم | إجمالي الأصناف: {products.Count} صنف");
                summaryBuilder.AppendLine($"إجمالي الكميات المباعة: {_viewModel.SalesManagementProberty.TotalSoldPortionsSum:0.##} حبة/نفر | إجمالي الإيرادات: {_viewModel.SalesManagementProberty.TotalProductsSalesRevenue:#,##0} ريال");

                if (categories.Count > 0)
                {
                    summaryBuilder.AppendLine();
                    summaryBuilder.AppendLine("--- ملخص جرد مبيعات الأقسام ---");
                    foreach (var cat in categories)
                    {
                        summaryBuilder.AppendLine($"• {cat.CategoryName}: إجمالي ({cat.TotalSoldQuantityText}) - [{cat.ProductsDetailsSummaryText}] - ({cat.TotalRevenue:#,##0} ريال)");
                    }
                }

                Services.PrintService.PrintReport(
                    "تقرير جرد مبيعات الأقسام والمنتجات اليومي",
                    $"الفترة من: {_viewModel.SalesManagementProberty.ReportStartDate:yyyy/MM/dd} إلى: {_viewModel.SalesManagementProberty.ReportEndDate:yyyy/MM/dd}",
                    products,
                    columnHeaders,
                    item =>
                    {
                        if (item is not POSSystem.WPF.Shered.Proberty.ProductSalesReportItem p)
                            return new string[columnHeaders.Length];

                        return new[]
                        {
                            p.CategoryName ?? "",
                            p.ProductName ?? "",
                            p.TotalSoldQuantityText ?? "",
                            p.SizesBreakdownText ?? "",
                            p.OrdersCount.ToString(),
                            $"{p.TotalRevenue:#,##0} ريال"
                        };
                    },
                    summaryBuilder.ToString(),
                    preferPdf: true);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"خطأ في تصدير تقرير الجرد اليومي: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source != MainTabControl || _viewModel == null) return;

            if (MainTabControl.SelectedItem == ReturnsTab)
                _ = _viewModel.ExecuteLoadReturnsAsync();
            else if (MainTabControl.SelectedItem == ProductsInventoryTab)
                _ = _viewModel.ExecuteLoadProductSalesReportAsync(
                    _viewModel.SalesManagementProberty.ReportStartDate.Date,
                    _viewModel.SalesManagementProberty.ReportEndDate.Date.AddDays(1).AddTicks(-1));
        }

        private void ShowOverlay(UserControl content)
        {
            if (Window.GetWindow(this) is AdminDashboardWindow adminWindow)
                adminWindow.ShowOverlay(content);
        }

        private void HideOverlay()
        {
            if (Window.GetWindow(this) is AdminDashboardWindow adminWindow)
                adminWindow.CloseOverlay();
        }
    }
}
