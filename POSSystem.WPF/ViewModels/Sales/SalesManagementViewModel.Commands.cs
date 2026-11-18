using System;
using System.Windows.Input;
using POSSystem.WPF.Models;
using POSSystem.WPF.Shered.Commands;

namespace POSSystem.WPF.ViewModels.Sales
{
    public partial class SalesManagementViewModel
    {
        public class SalesManagementCommands
        {
            public ICommand LoadSalesLogCommand { get; }
            public ICommand LoadSalesReportCommand { get; }
            public ICommand LoadReturnsCommand { get; }
            public ICommand ViewOrderDetailsCommand { get; }
            public ICommand ReprintOrderCommand { get; }
            public ICommand InitiateReturnCommand { get; }
            public ICommand SetTodayReportFilterCommand { get; }
            public ICommand SetMonthReportFilterCommand { get; }
            public ICommand ClearCategoryFilterCommand { get; }

            public SalesManagementCommands(SalesManagementViewModel vm)
            {
                LoadSalesLogCommand = new RelayCommand(async () =>
                {
                    await vm.LoadAllAsync();
                });
                LoadSalesReportCommand = new RelayCommand(async () =>
                {
                    await vm.LoadAllAsync();
                });
                LoadReturnsCommand = new RelayCommand(async () => await vm.ExecuteLoadReturnsAsync());
                ViewOrderDetailsCommand = new RelayCommand<Order>(vm.ViewOrderDetails);
                ReprintOrderCommand = new RelayCommand<Order>(vm.ReprintOrder);
                InitiateReturnCommand = new RelayCommand<Order>(vm.InitiateReturn);
                SetTodayReportFilterCommand = new RelayCommand(async () =>
                {
                    vm.SalesManagementProberty.ReportStartDate = DateTime.Today;
                    vm.SalesManagementProberty.ReportEndDate = DateTime.Today;
                    await vm.LoadAllAsync();
                });
                SetMonthReportFilterCommand = new RelayCommand(async () =>
                {
                    vm.SalesManagementProberty.ReportStartDate = DateTime.Today.AddDays(-30);
                    vm.SalesManagementProberty.ReportEndDate = DateTime.Today;
                    await vm.LoadAllAsync();
                });
                ClearCategoryFilterCommand = new RelayCommand(() =>
                {
                    vm.SalesManagementProberty.SelectedProductCategoryFilter = "الكل (جميع الأقسام)";
                    vm.SalesManagementProberty.ProductSalesSearchText = string.Empty;
                });
            }
        }
    }
}
