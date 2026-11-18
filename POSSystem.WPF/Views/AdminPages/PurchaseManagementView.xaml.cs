using POSSystem.WPF.ViewModels.Purchases;
using System.Windows.Controls;

namespace POSSystem.WPF.Views.AdminPages
{
    public partial class PurchaseManagementView : UserControl
    {
        public PurchaseManagementViewModel _viewModel;
        public PurchaseManagementView()
        {
            InitializeComponent();
            _viewModel= new PurchaseManagementViewModel();
            DataContext = _viewModel;
        }

        private async void AddSupplierButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var addSupplierWindow = new POSSystem.WPF.Views.Popups.AddEditSupplierPopup();
            if (addSupplierWindow.ShowDialog() == true)
            {
                await _viewModel.SupplierVM.LoadSuppliersAsync();
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel?.PurchaseManagementProberty?.SelectedWarehouse == null || CompoBoxWarhouses == null) return;

            var selctedWah = _viewModel.PurchaseManagementProberty.SelectedWarehouse;
            CompoBoxWarhouses.ItemsSource = selctedWah.warehouseFromProducts;
        }

        private void CompoBoxWarhouses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void SaveAsPdf_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (_viewModel?.PurchaseManagementProberty?.PurchasePager?.PagedItems == null) return;

                var purchases = _viewModel.PurchaseManagementProberty.PurchasePager.PagedItems.ToList();
                var columnHeaders = new[] { "رقم الشراء", "التاريخ", "المورد", "طريقة الدفع", "المبلغ الإجمالي" };

                Services.PrintService.PrintReport(
                    "تقرير المشتريات",
                    $"الفترة من: {_viewModel.PurchaseManagementProberty.ReportStartDate:yyyy/MM/dd} إلى: {_viewModel.PurchaseManagementProberty.ReportEndDate:yyyy/MM/dd}",
                    purchases,
                    columnHeaders,
                    (object item) =>
                    {
                        if (item == null) return new string[columnHeaders.Length];
                        var purchase = item as Models.Purchase;
                        if (purchase == null) return new string[columnHeaders.Length];
                        return new[]
                        {
                            purchase.PurchaseNumber ?? "",
                            purchase.PurchaseDate.ToString("yyyy/MM/dd"),
                            purchase.SupplierName ?? "",
                            purchase.PaymentMethodDisplay,
                            purchase.TotalAmount.ToString("#,##0") + " ريال"
                        };
                    },
                    $"إجمالي المشتريات: {_viewModel.PurchaseManagementProberty.TotalReportAmount:#,##0} ريال\n" +
                    $"إجمالي المرتجعات: {_viewModel.PurchaseManagementProberty.TotalReturnReportAmount:#,##0} ريال\n" +
                    $"صافي المشتريات: {_viewModel.PurchaseManagementProberty.NetReportAmount:#,##0} ريال",
                    preferPdf: true
                );
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ في حفظ التقرير: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void DataGrid_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new System.Windows.Input.MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = System.Windows.UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = System.Windows.Media.VisualTreeHelper.GetParent((System.Windows.DependencyObject)sender) as System.Windows.UIElement;
                parent?.RaiseEvent(eventArg);
            }
        }
    
        private void SelectWalletChip_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string walletName && !string.IsNullOrWhiteSpace(walletName))
            {
                if (DataContext is POSSystem.WPF.ViewModels.Purchases.PurchaseManagementViewModel vm)
                {
                    vm.PurchaseManagementProberty.DepositBankOrWallet = walletName;
                }
            }
        }

    }
}
