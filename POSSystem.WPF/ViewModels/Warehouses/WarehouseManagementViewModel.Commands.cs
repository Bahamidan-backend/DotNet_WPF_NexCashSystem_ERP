using System.Windows.Input;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Shered.Commands;

namespace POSSystem.WPF.ViewModels.Warehouses
{
    public partial class WarehouseManagementViewModel
    {
        public class WarehouseCommands
        {
            public ICommand AddWarehouseCommand { get; }
            public ICommand EditWarehouseCommand { get; }
            public ICommand DeleteWarehouseCommand { get; }
            public ICommand SearchCommand { get; }
            public ICommand RefreshCommand { get; }
            public ICommand ViewProductsCommand { get; }
            public ICommand LoadLowStockProductsCommand { get; }

            public WarehouseCommands(WarehouseManagementViewModel vm)
            {
                AddWarehouseCommand = new RelayCommand(vm.ExecuteAddWarehouse);
                EditWarehouseCommand = new RelayCommand(vm.ExecuteEditWarehouse, vm.CanExecuteEditWarehouse);
                DeleteWarehouseCommand = new RelayCommand(() => vm.Deletewarehouse(vm.SelectedWarehouse).SafeFireAndForget(), () => vm.SelectedWarehouse != null);
                SearchCommand = new RelayCommand(vm.ExecuteSearch);
                RefreshCommand = new RelayCommand(vm.ExecuteRefresh);
                ViewProductsCommand = new RelayCommand(vm.ExecuteViewProducts, vm.CanExecuteViewProducts);
                LoadLowStockProductsCommand = new RelayCommand(async () => await vm.LoadLowStockProductsAsync());
            }
        }
    }
}
