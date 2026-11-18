using System.Windows.Input;
using POSSystem.WPF.Shered.Commands;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.ViewModels.Purchases
{
    public partial class PurchaseManagementViewModel
    {
        public PurchaseManagementCommands Commands { get; }

        public class PurchaseManagementCommands
        {
            public ICommand SaveCommand { get; }
            public ICommand CancelCommand { get; }
            public ICommand AddToListCommand { get; }
            public ICommand RemoveFromListCommand { get; }
            public ICommand EditCartItemCommand { get; }
            public ICommand EditPurchaseCommand { get; }
            public ICommand ViewPurchaseDetailsCommand { get; }
            public ICommand LoadReportCommand { get; }
            public ICommand LoadReturnReportCommand { get; }
            public ICommand ViewVoucherCommand { get; }

            public PurchaseManagementCommands(PurchaseManagementViewModel vm)
            {
                SaveCommand = new RelayCommand(async () => await vm.ExecuteSaveAsync(), vm.CanExecuteSave);
                CancelCommand = new RelayCommand(vm.ExecuteCancel);
                AddToListCommand = new RelayCommand(vm.ExecuteAddToList, vm.CanExecuteAddToList);
                RemoveFromListCommand = new RelayCommand<PurchaseItem>(vm.ExecuteRemoveFromList);
                EditCartItemCommand = new RelayCommand<PurchaseItem>(vm.ExecuteEditCartItem);
                EditPurchaseCommand = new RelayCommand<Purchase>(async (p) => await vm.ExecuteEditPurchaseAsync(p));
                ViewPurchaseDetailsCommand = new RelayCommand<Purchase>(async (p) => await vm.ExecuteViewPurchaseDetailsAsync(p));
                LoadReportCommand = new RelayCommand(async () => await vm.ExecuteLoadReportAsync());
                LoadReturnReportCommand = new RelayCommand(async () => await vm.ExecuteLoadReturnReportAsync());
                ViewVoucherCommand = new RelayCommand<Purchase>(async (p) => await vm.ExecuteViewVoucherAsync(p));
            }
        }
    }
}
