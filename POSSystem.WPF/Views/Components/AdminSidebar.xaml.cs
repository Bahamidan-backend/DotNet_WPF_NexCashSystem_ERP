using System.Linq;
using System.Windows;
using System.Windows.Controls;
using POSSystem.WPF.Models;
using POSSystem.WPF.Services;
using POSSystem.WPF.Views;
using POSSystem.WPF.Views.AdminPages;

namespace POSSystem.WPF.Views.Components
{
    public partial class AdminSidebar : UserControl
    {
        public AdminSidebar()
        {
            InitializeComponent();
            Loaded += AdminSidebar_Loaded;
            Unloaded += AdminSidebar_Unloaded;
        }

        private void AdminSidebar_Loaded(object sender, RoutedEventArgs e)
        {
            NavigationService.Instance.OnNavigateToView += Instance_OnNavigateToView;
            ApplyUserPermissions(AdminDashboardWindow._currentUser);
        }

        private void AdminSidebar_Unloaded(object sender, RoutedEventArgs e)
        {
            NavigationService.Instance.OnNavigateToView -= Instance_OnNavigateToView;
        }

        public void ApplyUserPermissions(User? user)
        {
            if (user == null) return;

            RoleBadgeText.Text = !string.IsNullOrWhiteSpace(user.RoleTemplateName)
                ? user.RoleTemplateName
                : (user.Role == UserRole.Admin ? "مدير كامل الصلاحيات" : "موظف مخصص");

            OpenCashierModeButton.Visibility = (PermissionGuard.HasPermission(user, SystemPermissions.SalesCreateOrder) ||
                (!string.IsNullOrWhiteSpace(user.JobTitle) && user.JobTitle.Contains("كاشير")))
                ? Visibility.Visible
                : Visibility.Collapsed;

            SetButtonPermission(DashboardButton, user, SystemPermissions.DashboardView);
            MainSectionHeader.Visibility = DashboardButton.Visibility;

            SetButtonPermission(ProductsButton, user, SystemPermissions.ProductsView);
            SetButtonPermission(InventoryButton, user, SystemPermissions.InventoryView);
            AddonManagementButton.Visibility = Visibility.Collapsed; // Note 4: Removed AddonManagement
            SetButtonPermission(DiscountsButton, user, SystemPermissions.DiscountsManage);
            ProductsSectionHeader.Visibility = AnyVisible(ProductsButton, InventoryButton, AddonManagementButton, DiscountsButton);

            SetButtonPermission(PurchaseManagementButton, user, SystemPermissions.PurchasesView);
            SetButtonPermission(SalesManagementButton, user, SystemPermissions.SalesView);
            SetButtonPermission(ReturnsManagementButton, user, SystemPermissions.ReturnsView);
            SalesSectionHeader.Visibility = AnyVisible(PurchaseManagementButton, SalesManagementButton, ReturnsManagementButton);

            SetButtonPermission(CustomerManagementButton, user, SystemPermissions.CustomersView);
            SetButtonPermission(EmployeeManagementButton, user, SystemPermissions.EmployeesView);
            SetButtonPermission(CashierManagementButton, user, SystemPermissions.CashBoxView);
            SetButtonPermission(KitchenManagementButton, user, SystemPermissions.KitchenView);
            UsersSectionHeader.Visibility = AnyVisible(CustomerManagementButton, EmployeeManagementButton, CashierManagementButton, KitchenManagementButton);

            SetButtonPermission(FinancialAccountsButton, user, SystemPermissions.FinancialAccountsView);
            SetButtonPermission(ExpenseManagementButton, user, SystemPermissions.ExpensesView);
            SetButtonPermission(FinancialOverviewButton, user, SystemPermissions.FinancialReportsView);
            SetButtonPermission(MonthlyReportsButton, user, SystemPermissions.GeneralReportsView);
            ReportsSectionHeader.Visibility = AnyVisible(FinancialAccountsButton, ExpenseManagementButton, FinancialOverviewButton, MonthlyReportsButton);

            SetButtonPermission(SettingsButton, user, SystemPermissions.SettingsView);
            SettingsSectionHeader.Visibility = SettingsButton.Visibility;
        }

        private static void SetButtonPermission(Button button, User user, string permissionKey)
        {
            button.Visibility = PermissionGuard.HasPermission(user, permissionKey)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private static Visibility AnyVisible(params UIElement[] elements)
        {
            return elements.Any(e => e.Visibility == Visibility.Visible) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OpenCashierModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (AdminDashboardWindow._currentUser != null)
            {
                NavigationService.Instance.NavigateToCashierWindow(AdminDashboardWindow._currentUser);
            }
        }

        private void Instance_OnNavigateToView(UserControl view)
        {
            Button? targetButton = null;

            if (view is DashboardView) targetButton = DashboardButton;
            else if (view is ProductsView) targetButton = ProductsButton;
            else if (view is InventoryView) targetButton = InventoryButton;
            else if (view is PurchaseManagementView) targetButton = PurchaseManagementButton;
            else if (view is SalesManagementView) targetButton = SalesManagementButton;
            else if (view is ReturnsManagementView) targetButton = ReturnsManagementButton;
            else if (view is DiscountsView) targetButton = DiscountsButton;
            else if (view is AddonManagementView) targetButton = AddonManagementButton;
            else if (view is EmployeeManagementView) targetButton = EmployeeManagementButton;
            else if (view is CustomerManagementView) targetButton = CustomerManagementButton;
            else if (view is CashierManagementView) targetButton = CashierManagementButton;
            else if (view is FinancialAccountsView) targetButton = FinancialAccountsButton;
            else if (view is ExpenseManagementView) targetButton = ExpenseManagementButton;
            else if (view is FinancialOverviewView) targetButton = FinancialOverviewButton;
            else if (view is MonthlyReportsView) targetButton = MonthlyReportsButton;
            else if (view is SettingsView) targetButton = SettingsButton;
            else if (view is KitchenManagementView) targetButton = KitchenManagementButton;

            if (targetButton != null)
            {
                SetActiveButton(targetButton);
            }
        }

        public event RoutedEventHandler? NavigationButtonClick;

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                SetActiveButton(clickedButton);
            }
            NavigationButtonClick?.Invoke(sender, e);
        }

        private void SetActiveButton(Button activeButton)
        {
            // Reset all buttons
            if (activeButton.Parent is StackPanel stackPanel)
            {
                foreach (var child in stackPanel.Children)
                {
                    if (child is Button button)
                    {
                        button.Style = FindResource("NavigationButtonStyle") as Style;
                    }
                }
            }

            // Set active button
            activeButton.Style = FindResource("ActiveNavigationButtonStyle") as Style;
        }
    }
}
