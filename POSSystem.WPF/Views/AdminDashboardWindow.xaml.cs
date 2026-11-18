using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using POSSystem.WPF.ViewModels.Admin;
using POSSystem.WPF.ViewModels;
using POSSystem.WPF.Services;

namespace POSSystem.WPF.Views
{
    public partial class AdminDashboardWindow : Window
    {
        public static  User _currentUser;
        private readonly NavigationService _navigationService;

        public AdminDashboardWindow(User usr)
        {
            InitializeComponent();
            _currentUser = usr;
            _navigationService = NavigationService.Instance;
            
            var adminVm = new AdminDashboardViewModel(usr);
            adminVm.SearchTextChanged += OnGlobalSearchTextChanged;
            DataContext = adminVm;
            
            // Subscribe to navigation events
            _navigationService.OnNavigateToView += NavigateToView;
            POSSystem.WPF.Business.ServiceLocator.UserPermissionsChanged += OnUserPermissionsChanged;
            this.Activated += AdminDashboardWindow_Activated;
            this.Closed += (_, _) => POSSystem.WPF.Business.ServiceLocator.UserPermissionsChanged -= OnUserPermissionsChanged;
            
            // Show first permitted view by default
            NavigateToDefaultPermittedView(usr);
        }

        private void OnUserPermissionsChanged(User updatedUser)
        {
            if (updatedUser == null || _currentUser == null || updatedUser.Id != _currentUser.Id) return;
            Dispatcher.Invoke(() =>
            {
                _currentUser.Name = updatedUser.Name;
                _currentUser.JobTitle = updatedUser.JobTitle;
                _currentUser.Role = updatedUser.Role;
                _currentUser.RoleTemplateName = updatedUser.RoleTemplateName;
                _currentUser.PermissionsJson = updatedUser.PermissionsJson;
                AdminSidebarControl?.ApplyUserPermissions(_currentUser);
                if (MainContentArea?.Content is POSSystem.WPF.Views.AdminPages.ProductsView pv)
                {
                    pv.ApplyPermissions();
                }
            });
        }

        private async void AdminDashboardWindow_Activated(object? sender, System.EventArgs e)
        {
            try
            {
                if (_currentUser == null) return;
                var freshUser = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                    Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(
                        POSSystem.WPF.Business.ServiceLocator.UnitOfWork.GetContext().Users),
                    u => u.Id == _currentUser.Id);
                if (freshUser != null)
                {
                    OnUserPermissionsChanged(freshUser);
                }
            }
            catch { }
        }

        private void NavigateToDefaultPermittedView(User usr)
        {
            if (PermissionGuard.HasPermission(usr, SystemPermissions.DashboardView))
            {
                _navigationService.ShowDashboardView();
                return;
            }
            if (PermissionGuard.HasPermission(usr, SystemPermissions.ProductsView)) { _navigationService.ShowProductsView(); return; }
            if (PermissionGuard.HasPermission(usr, SystemPermissions.InventoryView)) { _navigationService.ShowInventoryView(); return; }
            // AddonManagement removed per Note 4
            if (PermissionGuard.HasPermission(usr, SystemPermissions.DiscountsManage)) { _navigationService.ShowDiscountsView(); return; }
            if (PermissionGuard.HasPermission(usr, SystemPermissions.PurchasesView)) { _navigationService.ShowPurchaseManagementView(); return; }
            if (PermissionGuard.HasPermission(usr, SystemPermissions.SalesView)) { _navigationService.ShowSalesManagementView(); return; }
            if (PermissionGuard.HasPermission(usr, SystemPermissions.ReturnsView)) { _navigationService.ShowReturnsManagementView(usr); return; }
            if (PermissionGuard.HasPermission(usr, SystemPermissions.CustomersView)) { _navigationService.ShowCustomerManagementView(); return; }
            if (PermissionGuard.HasPermission(usr, SystemPermissions.EmployeesView)) { _navigationService.ShowEmployeeManagementView(); return; }
            if (PermissionGuard.HasPermission(usr, SystemPermissions.CashBoxView)) { _navigationService.ShowCashierManagementView(); return; }
            if (PermissionGuard.HasPermission(usr, SystemPermissions.KitchenView)) { _navigationService.ShowKitchenManagementView(); return; }
            if (PermissionGuard.HasPermission(usr, SystemPermissions.FinancialAccountsView)) { _navigationService.ShowFinancialAccountsView(); return; }
            if (PermissionGuard.HasPermission(usr, SystemPermissions.ExpensesView)) { _navigationService.ShowExpenseManagementView(); return; }
            if (PermissionGuard.HasPermission(usr, SystemPermissions.FinancialReportsView)) { _navigationService.ShowFinancialOverviewView(); return; }
            if (PermissionGuard.HasPermission(usr, SystemPermissions.GeneralReportsView)) { _navigationService.ShowMonthlyReportsView(); return; }
            if (PermissionGuard.HasPermission(usr, SystemPermissions.SettingsView)) { _navigationService.ShowSettingsView(); return; }

            _navigationService.ShowDashboardView();
        }


        private void NavigateToView(UserControl view)
        {
            // Find the content area and replace its content
            if (MainContentArea != null)
            {
                MainContentArea.Content = view;
            }

            // Reset search query when switching views
            if (DataContext is AdminDashboardViewModel vm && !string.IsNullOrEmpty(vm.SearchText))
            {
                vm.SearchText = string.Empty;
            }
        }

        private void OnGlobalSearchTextChanged(string query)
        {
            if (MainContentArea?.Content is FrameworkElement element)
            {
                if (element is ISearchable viewSearchable)
                {
                    viewSearchable.ApplySearch(query);
                }
                else if (element.DataContext is ISearchable vmSearchable)
                {
                    vmSearchable.ApplySearch(query);
                }
            }
        }

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                switch (tag)
                {
                    case "Dashboard":
                        _navigationService.ShowDashboardView();
                        break;
                    case "Products":
                        _navigationService.ShowProductsView();
                        break;
                    case "Inventory":
                        _navigationService.ShowInventoryView();
                        break;
                    case "PurchaseManagement":
                        _navigationService.ShowPurchaseManagementView();
                        break;
                    case "SalesManagement":
                        _navigationService.ShowSalesManagementView();
                        break;
                    case "Suppliers":
                        _navigationService.ShowSuppliersView();
                        break;
                    case "ReturnsManagement":
                        _navigationService.ShowReturnsManagementView(_currentUser);
                        break;
                    case "Discounts":
                        _navigationService.ShowDiscountsView();
                        break;
                    case "AddonManagement":
                        _navigationService.ShowAddonManagementView();
                        break;
                    case "EmployeeManagement":
                        _navigationService.ShowEmployeeManagementView();
                        break;
                    case "CustomerManagement":
                        _navigationService.ShowCustomerManagementView();
                        break;
                    case "CashierManagement":
                        _navigationService.ShowCashierManagementView();
                        break;
                    case "CashierAudit":
                        _navigationService.ShowCashierAuditView();
                        break;
                    case "FinancialAccounts":
                        _navigationService.ShowFinancialAccountsView();
                        break;
                    case "ExpenseManagement":
                        _navigationService.ShowExpenseManagementView();
                        break;
                    case "FinancialOverview":
                        _navigationService.ShowFinancialOverviewView();
                        break;
                    case "MonthlyReports":
                        _navigationService.ShowMonthlyReportsView();
                        break;
                    case "KitchenManagement":
                        _navigationService.ShowKitchenManagementView();
                        break;
                    case "Settings":
                        _navigationService.ShowSettingsView();
                        break;
                }
            }
        }


        public void ShowOverlay(UserControl content)
        {
            if (OverlayContainer != null && OverlayContent != null)
            {
                OverlayContent.Content = content;
                OverlayContainer.Visibility = Visibility.Visible;
            }
        }

        public void CloseOverlay()
        {
            if (OverlayContainer != null && OverlayContent != null)
            {
                OverlayContainer.Visibility = Visibility.Collapsed;
                OverlayContent.Content = null;
            }
        }

        private void OverlayBackdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
                CloseOverlay();
        }
    }
}
