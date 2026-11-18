using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.Shered.Commands
{
    public class AdminDashboardCommand
    {

        private readonly NavigationService _navigationService;



        public AdminDashboardCommand()
        {
            _navigationService = NavigationService.Instance;
            NavigateToCashierCommand = new RelayCommand(() => _navigationService.ShowCashierView());
            NavigateToProductsCommand = new RelayCommand(() => _navigationService.ShowProductsView());
            NavigateToInventoryCommand = new RelayCommand(() => _navigationService.ShowInventoryView());
            NavigateToDiscountsCommand = new RelayCommand(() => _navigationService.ShowDiscountsView());
            NavigateToAddonManagementCommand = new RelayCommand(() => _navigationService.ShowAddonManagementView());
            NavigateToReturnsManagementCommand = new RelayCommand(() => _navigationService.ShowReturnsManagementView(AdminDashboardWindow._currentUser));
            NavigateToSettingsCommand = new RelayCommand(() => _navigationService.ShowSettingsView());
            NavigateToDashboardCommand = new RelayCommand(() => _navigationService.ShowDashboardView());
            NavigateToEmployeeManagementCommand = new RelayCommand(() => _navigationService.ShowEmployeeManagementView());
            NavigateToCustomerManagementCommand = new RelayCommand(() => _navigationService.ShowCustomerManagementView());
            NavigateToCashierManagementCommand = new RelayCommand(() => _navigationService.ShowCashierManagementView());
            NavigateToCashierAuditCommand = new RelayCommand(() => _navigationService.ShowCashierAuditView());
            NavigateToExpenseManagementCommand = new RelayCommand(() => _navigationService.ShowExpenseManagementView());
            NavigateToPurchaseManagementCommand = new RelayCommand(() => _navigationService.ShowPurchaseManagementView());
            NavigateToSalesManagementCommand = new RelayCommand(() => _navigationService.ShowSalesManagementView());
            NavigateToMonthlyReportsCommand = new RelayCommand(() => _navigationService.ShowMonthlyReportsView());
            NavigateToSuppliersCommand = new RelayCommand(() => _navigationService.ShowSuppliersView());
            NavigateToKitchenManagementCommand = new RelayCommand(() => _navigationService.ShowKitchenManagementView());

            NavigateToFinancialOverviewCommand = new RelayCommand(() => _navigationService.ShowFinancialOverviewView());
            NavigateToFinancialAccountsCommand = new RelayCommand(() => _navigationService.ShowFinancialAccountsView());
            LogoutCommand = new RelayCommand(() => _navigationService.NavigateToLogin());
        }


        public ICommand NavigateToCashierCommand { get; set; }
        public ICommand NavigateToProductsCommand { get;  set;}
        public ICommand NavigateToInventoryCommand { get; set;}
        public ICommand NavigateToDiscountsCommand { get; set;}
        public ICommand NavigateToAddonManagementCommand { get; set; }
        public ICommand NavigateToReturnsManagementCommand { get; set; }
        public ICommand NavigateToSettingsCommand { get;        set;}
        public ICommand NavigateToDashboardCommand { get;       set;}
        public ICommand NavigateToEmployeeManagementCommand { get;set; }
        public ICommand NavigateToCustomerManagementCommand { get;set; }
        public ICommand NavigateToCashierManagementCommand { get; set; }
        public ICommand NavigateToCashierAuditCommand { get; set; }
        public ICommand NavigateToExpenseManagementCommand { get; set; }
        public ICommand NavigateToPurchaseManagementCommand { get; set; }
        public ICommand NavigateToSalesManagementCommand { get; set; }
        public ICommand NavigateToMonthlyReportsCommand { get; set; }
        public ICommand NavigateToSuppliersCommand { get; set; }
        public ICommand NavigateToKitchenManagementCommand { get; set; }

        public ICommand NavigateToFinancialOverviewCommand { get; set; }
        public ICommand NavigateToFinancialAccountsCommand { get; set; }
        public ICommand LogoutCommand { get; set; }
        public ICommand ToggleSidebarCommand { get; set; }
        public ICommand FilterCommand { get; set; }

    }
}
