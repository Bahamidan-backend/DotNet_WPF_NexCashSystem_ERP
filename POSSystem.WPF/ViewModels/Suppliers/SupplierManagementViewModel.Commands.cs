using System.Windows.Input;
using POSSystem.WPF.Models;
using POSSystem.WPF.Shered.Commands;

namespace POSSystem.WPF.ViewModels.Suppliers
{
    public partial class SupplierManagementViewModel
    {
        public class SupplierManagementCommands
        {
            public ICommand AddSupplierCommand { get; }
            public ICommand EditSupplierCommand { get; }
            public ICommand DeleteSupplierCommand { get; }
            public ICommand RecordPaymentCommand { get; }

            public SupplierManagementCommands(SupplierManagementViewModel vm)
            {
                AddSupplierCommand = new RelayCommand(() => vm.AddSupplier());
                EditSupplierCommand = new RelayCommand<Supplier>(vm.EditSupplier, s => s != null);
                DeleteSupplierCommand = new RelayCommand<Supplier>(vm.DeleteSupplier, s => s != null);
                RecordPaymentCommand = new RelayCommand(() => vm.RecordPayment());
            }
        }
    }
}
