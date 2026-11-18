using System.Windows.Input;
using POSSystem.WPF.ViewModels.Cashier;
using POSSystem.WPF.ViewModels;

namespace POSSystem.WPF.Shered.Commands
{
    public class CashierManagementCommands
    {
        public ICommand LoadCashierSalesReportCommand { get; }
        public ICommand LoadDailySalesReportCommand { get; }
        public ICommand LoadDeliveryReportCommand { get; }
        public ICommand LoadShiftReportCommand { get; }

        public ICommand LoadCashBoxReportCommand { get; }
        public ICommand LoadAuditReportCommand { get; }
        public ICommand RefreshCommand { get; }

        public CashierManagementCommands(CashierManagementViewModel vm)
        {
            LoadCashierSalesReportCommand = new RelayCommand(async () => await vm.LoadCashierSalesReportAsync());
            LoadDailySalesReportCommand = new RelayCommand(async () => await vm.LoadDailySalesReportAsync());
            LoadDeliveryReportCommand = new RelayCommand(async () => await vm.LoadDeliveryReportAsync());
            LoadShiftReportCommand = new RelayCommand(async () => await vm.LoadShiftReportAsync());

            LoadCashBoxReportCommand = new RelayCommand(async () => await vm.LoadCashBoxReportAsync());
            LoadAuditReportCommand = new RelayCommand(async () => await vm.LoadAuditReportAsync());
            RefreshCommand = new RelayCommand(async () =>
            {
                await vm.LoadCashiersListAsync();
                vm.RefreshCurrentReport();
            });
        }
    }
}

