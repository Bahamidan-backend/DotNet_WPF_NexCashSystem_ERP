using POSSystem.WPF.Services.Logging;


using System;
using POSSystem.WPF.ViewModels.Orders;
using POSSystem.WPF.ViewModels.Returns;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using POSSystem.WPF.Models;
using POSSystem.WPF.ViewModels;
using POSSystem.WPF.ViewModels.Settings;
using POSSystem.WPF.ViewModels.Cashier;
using POSSystem.WPF.Views.Components;
using POSSystem.WPF.Views.Popups;

namespace POSSystem.WPF.Views
{
    public partial class CashierWindow : Window
    {
        private readonly User _currentUser;
        private readonly CashierViewModel? _cashierViewModel;
        private readonly OrdersViewModel? _ordersViewModel;
        private readonly SettingsViewModel? _settingsViewModel;
        private readonly ProductSearchBar? _productserchbar;

        public CashierWindow(User user)
        {
            InitializeComponent();
            _currentUser = user;
            
            try
            {
                _cashierViewModel = new CashierViewModel(user);
                // FIX ISSUE-PERF-001: Reuse CashierViewModel's OrdersViewModel instead of instantiating a second duplicate instance
                _ordersViewModel = _cashierViewModel.OrdersVM;
                _settingsViewModel = new SettingsViewModel(user);
                _productserchbar = new ProductSearchBar();
                
                DataContext = _cashierViewModel;
                
                if (_cashierViewModel != null && _settingsViewModel != null)
                {
                    _cashierViewModel.settingsProberty = _settingsViewModel.settingsProberty;
                }
                
                // Set DataContext for pages
                System.Threading.Tasks.Task.Run(async () => {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                        _ordersViewModel.OrderDetailsRequested += (s, order) =>
                        {
                            var orderDetailsControl = new POSSystem.WPF.Controls.OrderDetailsControl();
                            orderDetailsControl.Initialize(order);
                            orderDetailsControl.CloseRequested += (sender, args) => HideOverlay();
                            ShowOverlay(orderDetailsControl);
                        };
                    });
                });

                if (_ordersViewModel != null)
                {
                    _ordersViewModel.ReopenOrderInCashierRequested += async (s, reopenedOrder) =>
                    {
                        if (_cashierViewModel != null && reopenedOrder != null)
                        {
                            await _cashierViewModel.LoadOrderIntoCartForEditAsync(reopenedOrder);
                            ShowProductsPage();
                        }
                    };
                }
                OrdersPage.DataContext = _ordersViewModel;
                SettingsPage.DataContext = _settingsViewModel;
                CashBoxReportPage.SetUser(user);
                
                // Initialize ReturnOrderView DataContext with the current user via its viewmodel
                ReturnOrdersPage.DataContext = new ReturnOrderViewModel(user);

                // Subscribe to navigation request from ViewModel
                if (_cashierViewModel != null)
                {
                    _cashierViewModel.ShowReturnOrdersRequested += (s, e) => ShowReturnOrdersPage();
                }

                // Set DataContext for OrderSidebar
                OrderSidebar.DataContext = _cashierViewModel;

                this.PreviewKeyDown += CashierWindow_PreviewKeyDown;
                this.Closed += CashierWindow_Closed;
                this.Activated += async (s, e) => { if (_cashierViewModel != null) await _cashierViewModel.RefreshAllCashierDataAsync(); };
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"Error initializing CashierWindow: {ex.Message}", ex);
                MessageBox.Show($"خطأ في تهيئة النافذة: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void CashierWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space || e.Key == Key.F1)
            {
                // If user is currently typing in a text box (search bar, delivery notes, etc.), do not intercept space
                if (e.Key == Key.Space)
                {
                    if (Keyboard.FocusedElement is System.Windows.Controls.Primitives.TextBoxBase ||
                        e.OriginalSource is System.Windows.Controls.Primitives.TextBoxBase)
                    {
                        return;
                    }
                }

                // Only process when in POS Products page and no popup overlays are open
                if (ProductsPage.Visibility == Visibility.Visible && OverlayGrid.Visibility != Visibility.Visible)
                {
                    if (_cashierViewModel?.Commands?.PlaceOrderCashCommand?.CanExecute(null) == true)
                    {
                        e.Handled = true;
                        _cashierViewModel.Commands.PlaceOrderCashCommand.Execute(null);
                    }
                }
            }
        }

        private void CashierWindow_Closed(object? sender, EventArgs e)
        {
            _cashierViewModel?.Dispose();
            _ordersViewModel?.Dispose();
            _settingsViewModel?.Dispose();
            if (ReturnOrdersPage.DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        
        private void ProductCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source &&
                (FindParent<Button>(source) != null ||
                 FindParent<System.Windows.Controls.Primitives.TextBoxBase>(source) != null))
            {
                return;
            }

            if (sender is FrameworkElement element && element.DataContext is Product product)
            {
                var viewModel = DataContext as CashierViewModel;
                viewModel?.AddProductToOrder(product);
            }
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent) return parent;
                child = System.Windows.Media.VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        private bool _isUpdatingProductQuantityBox = false;

        private static string NormalizeArabicDigits(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var chars = input.Trim().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] >= '\u0660' && chars[i] <= '\u0669')
                    chars[i] = (char)('0' + (chars[i] - '\u0660'));
                else if (chars[i] >= '\u06F0' && chars[i] <= '\u06F9')
                    chars[i] = (char)('0' + (chars[i] - '\u06F0'));
            }
            return new string(chars);
        }

        private void QuantityTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.SelectAll();
            }
        }

        private void QuantityTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb && !tb.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                tb.Focus();
                tb.SelectAll();
            }
        }

        private void QuantityTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var norm = NormalizeArabicDigits(e.Text);
            if (string.IsNullOrEmpty(norm) || !norm.All(char.IsDigit))
            {
                e.Handled = true;
            }
        }

        private void QuantityTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox tb)
            {
                e.Handled = true;
                Keyboard.ClearFocus();
            }
        }

        private void ProductQuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingProductQuantityBox || _cashierViewModel == null) return;
            if (sender is not TextBox tb || tb.Tag is not Product product) return;
            if (!tb.IsKeyboardFocusWithin) return;

            var norm = NormalizeArabicDigits(tb.Text);
            if (string.IsNullOrWhiteSpace(norm))
                return;

            if (!int.TryParse(norm, out int desiredQty) || desiredQty <= 0)
                return;

            _isUpdatingProductQuantityBox = true;
            try
            {
                int applied = _cashierViewModel.SetDirectProductQuantity(product, desiredQty, out bool wasCapped, out int maxAvailable);
                if (wasCapped)
                {
                    tb.Text = applied.ToString();
                    tb.CaretIndex = tb.Text.Length;
                    MessageBox.Show(
                        $"عذراً، الكمية المدخلة ({desiredQty}) أكبر من المتوفر في المخزون!\nأقصى كمية متاحة من '{product.Name}' هي ({maxAvailable}).",
                        "تنبيه - تجاوز الكمية المتاحة",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            finally
            {
                _isUpdatingProductQuantityBox = false;
            }
        }

        private void ProductQuantityTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingProductQuantityBox || _cashierViewModel == null) return;
            if (sender is not TextBox tb || tb.Tag is not Product product) return;

            var norm = NormalizeArabicDigits(tb.Text);
            if (!int.TryParse(norm, out int desiredQty) || desiredQty <= 0)
            {
                _isUpdatingProductQuantityBox = true;
                try
                {
                    tb.Text = Math.Max(1, product.Quantity).ToString();
                }
                finally
                {
                    _isUpdatingProductQuantityBox = false;
                }
            }
        }

        private void AddProductSize_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ProductSize productSize)
            {
                var viewModel = DataContext as CashierViewModel;
                if (viewModel != null)
                {
                    var parentProduct = productSize.Product ?? viewModel.Products.FirstOrDefault(p => p.Id == productSize.ProductId);
                    if (parentProduct != null)
                    {
                        viewModel.AddProductWithSizeToOrder(parentProduct, productSize.SizeName);
                    }
                }
            }
            e.Handled = true;
        }

        private void ShowOverlay(object content)
        {
            PopupContainer.Content = content;
            OverlayGrid.Visibility = Visibility.Visible;
        }

        private void HideOverlay()
        {
            OverlayGrid.Visibility = Visibility.Collapsed;
            PopupContainer.Content = null;
        }

        private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Optional: Check if the click was on the grid itself, not the content
            // Assuming the content captures its own clicks, this only fires for the background
             // HideOverlay(); 
             // Commented out to prevent accidental closing if clicking slightly outside or if UI logic requires explicit close
        }

        private void MenuToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (LeftColumn.Width.Value == 280)
            {
                // Close sidebar
                LeftColumn.Width = new GridLength(0);
            }
            else
            {
                // Open sidebar
                LeftColumn.Width = new GridLength(280);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                // Set clicked button to active style
                clickedButton.Style = FindResource("ActiveCategoryButtonStyle") as Style;

                // Execute the command if it exists
                if (clickedButton.Command != null && clickedButton.Command.CanExecute(clickedButton.CommandParameter))
                {

                    clickedButton.Command.Execute(clickedButton.CommandParameter);
                }
            }
        }


        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                // Handle specific navigation based on tag
                switch (clickedButton.Tag?.ToString())
                {
                    case "Dashboard":
                        // Navigate to Dashboard (Products page)
                        ShowProductsPage();
                        break;
                    case "Orders":
                        // Navigate to Orders (Order history)
                        ShowOrdersPage();
                        break;
                    case "CashBoxReport":
                        // Show cash box report page
                        ShowCashBoxReportPage();
                        break;
                    case "Settings":
                        // Navigate to Settings
                        ShowSettingsPage();
                        break;
                    case "ReturnOrders":
                        ShowReturnOrdersPage();
                        break;
                    case "AdminPanel":
                        if (PermissionGuard.CanEnterAdminPanelFromCashier(_currentUser))
                        {
                            NavigationService.Instance.NavigateToAdminDashboard(_currentUser);
                        }
                        break;
                }
            }
        }

        private void ShowOrdersPage()
        {
            // Hide products page and show orders page
            var productsPage = FindName("ProductsPage") as Border;
            var ordersPage = FindName("OrdersPage") as UserControl;
            var returnOrdersPage = FindName("ReturnOrdersPage") as UserControl;
            var cashBoxReportPage = FindName("CashBoxReportPage") as UserControl;
            var settingsPage = FindName("SettingsPage") as UserControl;
            var orderSidebar = FindName("OrderSidebar") as UserControl;
            
            if (productsPage != null && ordersPage != null && returnOrdersPage != null &&
                cashBoxReportPage != null && settingsPage != null && orderSidebar != null)
            {
                productsPage.Visibility = Visibility.Collapsed;
                ordersPage.Visibility = Visibility.Visible;
                returnOrdersPage.Visibility = Visibility.Collapsed;
                cashBoxReportPage.Visibility = Visibility.Collapsed;
                settingsPage.Visibility = Visibility.Collapsed;
                orderSidebar.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowReturnOrdersPage()
        {
            var productsPage = FindName("ProductsPage") as Border;
            var ordersPage = FindName("OrdersPage") as UserControl;
            var returnOrdersPage = FindName("ReturnOrdersPage") as UserControl;
            var cashBoxReportPage = FindName("CashBoxReportPage") as UserControl;
            var settingsPage = FindName("SettingsPage") as UserControl;
            var orderSidebar = FindName("OrderSidebar") as UserControl;
            
            if (productsPage != null && ordersPage != null && returnOrdersPage != null &&
                cashBoxReportPage != null && settingsPage != null && orderSidebar != null)
            {
                productsPage.Visibility = Visibility.Collapsed;
                ordersPage.Visibility = Visibility.Collapsed;
                returnOrdersPage.Visibility = Visibility.Visible;
                if (returnOrdersPage.DataContext is ReturnOrderViewModel vm)
                {
                    vm.RefreshOrders();
                }
                cashBoxReportPage.Visibility = Visibility.Collapsed;
                settingsPage.Visibility = Visibility.Collapsed;
                orderSidebar.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowCashBoxReportPage()
        {
            // Hide products page and show cash box report page
            var productsPage = FindName("ProductsPage") as Border;
            var ordersPage = FindName("OrdersPage") as UserControl;
            var returnOrdersPage = FindName("ReturnOrdersPage") as UserControl;
            var cashBoxReportPage = FindName("CashBoxReportPage") as UserControl;
            var settingsPage = FindName("SettingsPage") as UserControl;
            var orderSidebar = FindName("OrderSidebar") as UserControl;
            
            if (productsPage != null && ordersPage != null && returnOrdersPage != null &&
                cashBoxReportPage != null && settingsPage != null && orderSidebar != null)
            {
                productsPage.Visibility = Visibility.Collapsed;
                ordersPage.Visibility = Visibility.Collapsed;
                returnOrdersPage.Visibility = Visibility.Collapsed;
                cashBoxReportPage.Visibility = Visibility.Visible;
                CashBoxReportPage.RefreshReport();
                settingsPage.Visibility = Visibility.Collapsed;
                orderSidebar.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowSettingsPage()
        {
            // Hide products page and show settings page
            var productsPage = FindName("ProductsPage") as Border;
            var ordersPage = FindName("OrdersPage") as UserControl;
            var returnOrdersPage = FindName("ReturnOrdersPage") as UserControl;
            var cashBoxReportPage = FindName("CashBoxReportPage") as UserControl;
            var settingsPage = FindName("SettingsPage") as UserControl;
            var orderSidebar = FindName("OrderSidebar") as UserControl;
            
            if (productsPage != null && ordersPage != null && returnOrdersPage != null &&
                cashBoxReportPage != null && settingsPage != null && orderSidebar != null)
            {
                productsPage.Visibility = Visibility.Collapsed;
                ordersPage.Visibility = Visibility.Collapsed;
                returnOrdersPage.Visibility = Visibility.Collapsed;
                cashBoxReportPage.Visibility = Visibility.Collapsed;
                settingsPage.Visibility = Visibility.Visible;
                orderSidebar.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowProductsPage()
        {
            // Show products page and hide other pages
            var productsPage = FindName("ProductsPage") as Border;
            var ordersPage = FindName("OrdersPage") as UserControl;
            var returnOrdersPage = FindName("ReturnOrdersPage") as UserControl;
            var cashBoxReportPage = FindName("CashBoxReportPage") as UserControl;
            var settingsPage = FindName("SettingsPage") as UserControl;
            var orderSidebar = FindName("OrderSidebar") as UserControl;
            
            if (productsPage != null && ordersPage != null && returnOrdersPage != null &&
                cashBoxReportPage != null && settingsPage != null && orderSidebar != null)
            {
                productsPage.Visibility = Visibility.Visible;
                ordersPage.Visibility = Visibility.Collapsed;
                returnOrdersPage.Visibility = Visibility.Collapsed;
                cashBoxReportPage.Visibility = Visibility.Collapsed;
                settingsPage.Visibility = Visibility.Collapsed;
                orderSidebar.Visibility = Visibility.Visible;
            }
        }

        private void BackToProducts_Click(object sender, RoutedEventArgs e)
        {
            ShowProductsPage();
        }

        private void ViewOrderDetails_Click(object sender, RoutedEventArgs e)
        {
            Order? order = null;
            if (sender is Button btn && btn.Tag is Order tagOrder)
                order = tagOrder;
            else if (sender is Order directOrder)
                order = directOrder;

            if (order != null)
            {
                var orderDetailsControl = new POSSystem.WPF.Controls.OrderDetailsControl();
                orderDetailsControl.Initialize(order);
                orderDetailsControl.CloseRequested += (s, args) => HideOverlay();
                ShowOverlay(orderDetailsControl);
            }
        }

        private void ReprintOrder_Click(object sender, RoutedEventArgs e)
        {
            Order? order = null;
            if (sender is Button btn && btn.Tag is Order tagOrder)
                order = tagOrder;
            else if (sender is Order directOrder)
                order = directOrder;

            if (order != null)
            {
                _ = Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    POSSystem.WPF.StaticFunction.GloublStatic.PrintReceipt(order);
                }));
                MessageBox.Show($" تم إعادة طباعة فاتورة {order.OrderNumber}", 
                    "طباعة", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CancelOrder_Click(object sender, RoutedEventArgs e)
        {
            Order? order = null;
            if (sender is Button btn && btn.Tag is Order tagOrder)
                order = tagOrder;
            else if (sender is Order directOrder)
                order = directOrder;

            if (order != null && order.CanCancel)
            {
                _ordersViewModel?.CancelOrder(order);
            }
        }

        private void RefreshOrders_Click(object sender, RoutedEventArgs e)
        {
            // Refresh orders data
            var ordersPage = FindName("OrdersPage") as UserControl;
            if (ordersPage?.DataContext is OrdersViewModel ordersViewModel)
            {
                ordersViewModel.RefreshOrders();
            }
        }

        // Settings page event handlers
        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsViewModel != null)
            {
                // Execute the save settings command
                if (_settingsViewModel.Commands.SaveSettingsCommand.CanExecute(null))
                {
                    _settingsViewModel.Commands.SaveSettingsCommand.Execute(null);
                }
            }
        }

        private void RestartSystem_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsViewModel != null)
            {
                // Execute the restart system command
                if (_settingsViewModel.Commands.RestartSystemCommand.CanExecute(null))
                {
                    _settingsViewModel.Commands.RestartSystemCommand.Execute(null);
                }
            }
        }

        private void ClearData_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsViewModel != null)
            {
                // Execute the clear data command
                if (_settingsViewModel.Commands.ClearDataCommand.CanExecute(null))
                {
                    _settingsViewModel.Commands.ClearDataCommand.Execute(null);
                }
            }
        }

        private void ExportData_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsViewModel != null)
            {
                // Execute the export data command
                if (_settingsViewModel.Commands.ExportDataCommand.CanExecute(null))
                {
                    _settingsViewModel.Commands.ExportDataCommand.Execute(null);
                }
            }
        }

        private void ProductSearchBar_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void ProductSearchBar_DismantleClick(object sender, RoutedEventArgs e)
        {
            if (_cashierViewModel != null)
            {
                var dismantlePopup = new POSSystem.WPF.Views.Popups.DismantleProductPopup(_cashierViewModel);
                dismantlePopup.CloseRequested += (s, args) => HideOverlay();
                ShowOverlay(dismantlePopup);
            }
        }

        public void OpenLeftoverDisposalPopup()
        {
            var disposalPopup = new POSSystem.WPF.Views.Popups.CashierLeftoverDisposalPopup(_cashierViewModel, _cashierViewModel?.CurrentUser ?? _currentUser);
            disposalPopup.CloseRequested += async (s, args) =>
            {
                HideOverlay();
                if (_cashierViewModel != null)
                {
                    await _cashierViewModel.LoadProductsAsync();
                    _cashierViewModel.FilterProducts();
                }
            };
            ShowOverlay(disposalPopup);
        }

        private void ProductSearchBar_CookingBatchClick(object sender, RoutedEventArgs e)
        {
            var batchPopup = new POSSystem.WPF.Views.Popups.KitchenCookingBatchPopup(_cashierViewModel?.CurrentUser ?? _currentUser);
            batchPopup.CloseRequested += async (s, args) =>
            {
                HideOverlay();
                if (_cashierViewModel != null)
                {
                    await _cashierViewModel.LoadProductsAsync();
                }
            };
            ShowOverlay(batchPopup);
        }

        private void OrderSidebar_Loaded(object sender, RoutedEventArgs e)
        {
            // Ensure DataContext is set when OrderSidebar loads
            if (sender is UserControl orderSidebar && orderSidebar.DataContext == null)
            {
                orderSidebar.DataContext = _cashierViewModel;
            }
        }
    }
}
