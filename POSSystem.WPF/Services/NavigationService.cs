using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;
using POSSystem.WPF.Views;
using POSSystem.WPF.Views.AdminPages;
using SettingsView = POSSystem.WPF.Views.AdminPages.SettingsView;
using CashierManagementView = POSSystem.WPF.Views.AdminPages.CashierManagementView;
using ExpenseManagementView = POSSystem.WPF.Views.AdminPages.ExpenseManagementView;
using MonthlyReportsView = POSSystem.WPF.Views.AdminPages.MonthlyReportsView;
using FinancialOverviewView = POSSystem.WPF.Views.AdminPages.FinancialOverviewView;
using FinancialAccountsView = POSSystem.WPF.Views.AdminPages.FinancialAccountsView;
using PurchaseManagementView = POSSystem.WPF.Views.AdminPages.PurchaseManagementView;
using SalesManagementView = POSSystem.WPF.Views.AdminPages.SalesManagementView;
using ReturnOrderView = POSSystem.WPF.Views.AdminPages.ReturnOrderView;
using KitchenManagementView = POSSystem.WPF.Views.AdminPages.KitchenManagementView;
using SupplierManagementView = POSSystem.WPF.Views.AdminPages.SupplierManagementView;
using PurchaseReturnView = POSSystem.WPF.Views.AdminPages.PurchaseReturnView;
using ReturnsManagementView = POSSystem.WPF.Views.AdminPages.ReturnsManagementView;

namespace POSSystem.WPF.Services
{
    public class NavigationService
    {
        private static NavigationService? _instance;
        public static NavigationService Instance => _instance ??= new NavigationService();

        private NavigationService() { }

        private readonly Dictionary<Type, UserControl> _viewCache = new();

        private T GetOrCreateView<T>() where T : UserControl
        {
            if (!_viewCache.TryGetValue(typeof(T), out var view))
            {
                view = (T)Activator.CreateInstance(typeof(T))!;
                _viewCache[typeof(T)] = view;
            }
            return (T)view;
        }

        /// <summary>
        /// FIX ISSUE-PERF-002 & ISSUE-ARCH-001:
        /// Disabled eager background instantiation of 10 heavy Admin Views on startup,
        /// which previously flooded the shared POSDbContext and UI Dispatcher with concurrent queries.
        /// Views are instantiated lazily on-demand in <15ms via GetOrCreateView<T>().
        /// </summary>
        public void PreloadViewsAsync()
        {
            // Intentionally no-op: lazy on-demand view creation avoids startup UI jitter and DbContext contention.
        }

        public event Action<UserControl>? OnNavigateToView;

        public void NavigateToAdminDashboard(User user)
        {
            _viewCache.Clear();
            var adminDashboard = new AdminDashboardWindow(user);
            Application.Current.MainWindow = adminDashboard;
            adminDashboard.Show();
            CloseCurrentWindow();
        }

        public void NavigateToCashierWindow(User user)
        {
            var cashierWindow = new CashierWindow(user);
            Application.Current.MainWindow = cashierWindow;
            cashierWindow.Show();
            CloseCurrentWindow();
        }

        public void NavigateToLogin()
        {
            _viewCache.Clear();
            var loginWindow = new LoginWindow();
            Application.Current.MainWindow = loginWindow;
            loginWindow.Show();
            CloseCurrentWindow();
        }

        public void ShowCashierView()
        {
            try
            {
                OnNavigateToView?.Invoke(GetOrCreateView<ProductsView>());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التنقل إلى صفحة الكاشير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowProductsView()
        {
            try
            {
                OnNavigateToView?.Invoke(GetOrCreateView<ProductsView>());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التنقل إلى صفحة المنتجات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowInventoryView()
        {
            OnNavigateToView?.Invoke(GetOrCreateView<InventoryView>());
        }

        public void ShowDiscountsView()
        {
            OnNavigateToView?.Invoke(GetOrCreateView<DiscountsView>());
        }

        public void ShowAddonManagementView()
        {
            OnNavigateToView?.Invoke(GetOrCreateView<AddonManagementView>());
        }

        public void ShowSettingsView()
        {
            OnNavigateToView?.Invoke(GetOrCreateView<SettingsView>());
        }

        public void ShowDashboardView()
        {
            OnNavigateToView?.Invoke(GetOrCreateView<DashboardView>());
        }

        public void ShowEmployeeManagementView()
        {
            try
            {
                OnNavigateToView?.Invoke(GetOrCreateView<EmployeeManagementView>());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التنقل إلى صفحة الموظفين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowCustomerManagementView()
        {
            try
            {
                OnNavigateToView?.Invoke(GetOrCreateView<CustomerManagementView>());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التنقل إلى صفحة العملاء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowCashierManagementView()
        {
            try
            {
                OnNavigateToView?.Invoke(GetOrCreateView<CashierManagementView>());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التنقل إلى صفحة إدارة الكاشير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowCashierAuditView()
        {
            try
            {
                var view = GetOrCreateView<CashierManagementView>();
                if (view.DataContext is ViewModels.Cashier.CashierManagementViewModel vm)
                {
                    vm.LoadAuditReportAsync().SafeFireAndForget();
                }
                OnNavigateToView?.Invoke(view);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التنقل إلى رقابة وتدقيق الكاشير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowExpenseManagementView(bool showVouchersByDefault = false)
        {
            try
            {
                var view = GetOrCreateView<ExpenseManagementView>();
                if (showVouchersByDefault && view.DataContext is ViewModels.Expenses.ExpenseManagementViewModel vm)
                {
                    vm.ShowVouchers();
                }
                OnNavigateToView?.Invoke(view);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التنقل إلى صفحة إدارة المصروفات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowPaymentsView()
        {
            try
            {
                OnNavigateToView?.Invoke(new ExpenseManagementView(showVouchersByDefault: true));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التنقل إلى صفحة المدفوعات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowMonthlyReportsView()
        {
            try
            {
                OnNavigateToView?.Invoke(GetOrCreateView<MonthlyReportsView>());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التنقل إلى صفحة التقارير الشهرية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowFinancialOverviewView()
        {
            try
            {
                OnNavigateToView?.Invoke(GetOrCreateView<FinancialOverviewView>());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التنقل إلى التقارير المالية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowFinancialAccountsView()
        {
            try
            {
                OnNavigateToView?.Invoke(GetOrCreateView<FinancialAccountsView>());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التنقل إلى إدارة الحسابات والمحافظ والبنوك: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowPurchaseManagementView()
        {
            try
            {
                OnNavigateToView?.Invoke(GetOrCreateView<PurchaseManagementView>());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التنقل إلى صفحة إدارة المشتريات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowSalesManagementView()
        {
            try
            {
                OnNavigateToView?.Invoke(GetOrCreateView<SalesManagementView>());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التنقل إلى صفحة إدارة المبيعات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowReturnsManagementView(User user, string? invoiceNumber = null)
        {
            try
            {
                OnNavigateToView?.Invoke(new ReturnsManagementView(user, invoiceNumber));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التنقل إلى صفحة المرتجعات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowKitchenManagementView()
        {
            try
            {
                OnNavigateToView?.Invoke(GetOrCreateView<KitchenManagementView>());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التنقل إلى صفحة إدارة المطابخ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowSuppliersView()
        {
            ShowPurchaseManagementView();
        }

        private void CloseCurrentWindow()
        {
            var windowsToClose = Application.Current.Windows.OfType<Window>()
                .Where(w => w != Application.Current.MainWindow)
                .ToList();

            foreach (var window in windowsToClose)
            {
                window.Close();
            }
        }
    }
}
