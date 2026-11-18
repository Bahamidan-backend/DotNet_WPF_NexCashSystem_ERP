using System.Windows.Input;
using POSSystem.WPF.ViewModels.Customers;
using POSSystem.WPF.ViewModels;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Shered.Commands
{
    public class CustomerManagementCommands
    {
        public ICommand LoadDebtsCommand { get; }
        public ICommand AddDebtCommand { get; }
        public ICommand EditDebtCommand { get; }
        public ICommand DeleteDebtCommand { get; }
        public ICommand MarkAsPaidCommand { get; }
        public ICommand RepayDebtCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ToggleViewCommand { get; }
        public ICommand NavigateToPaymentsCommand { get; }
        public ICommand ViewProductsCommand { get; }
        public ICommand ViewLedgerCommand { get; }

        public CustomerManagementCommands(CustomerManagementViewModel vm)
        {
            LoadDebtsCommand = new RelayCommand(async () => await vm.LoadDebtsAsync());
            AddDebtCommand = new RelayCommand(() => vm.AddDebt());
            EditDebtCommand = new RelayCommand<CustomerDebt>(vm.EditDebt);
            DeleteDebtCommand = new RelayCommand<CustomerDebt>( vm.DeleteDebt);
            MarkAsPaidCommand = new RelayCommand<CustomerDebt>(vm.MarkAsPaid);
            RepayDebtCommand = new RelayCommand<CustomerDebt>(vm.RepayDebt);
            SearchCommand = new RelayCommand(async () => await vm.SearchDebtsAsync());
            ToggleViewCommand = new RelayCommand(() => vm.ToggleView());
            NavigateToPaymentsCommand = new RelayCommand(() => vm.NavigateToPayments());
            ViewProductsCommand = new RelayCommand<CustomerDebt>(vm.ViewProducts);
            ViewLedgerCommand = new RelayCommand<CustomerDebt>(vm.ViewLedger);
        }
    }
}
