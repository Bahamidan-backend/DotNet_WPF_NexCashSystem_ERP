using System.Windows.Input;
using POSSystem.WPF.Models;
using POSSystem.WPF.ViewModels.Cashier;
using POSSystem.WPF.ViewModels;

namespace POSSystem.WPF.Shered.Commands
{
    public class CashierCommands
    {
        public ICommand SetOrderTypeCommand { get; }
        public ICommand SetPaymentMethodCommand { get; }
        public ICommand TogglePrintCustomerCommand { get; }
        public ICommand TogglePrintKitchenCommand { get; }

        public ICommand AddProductToOrderCommand { get; }
        public ICommand RemoveProductFromOrderCommand { get; }
        public ICommand IncreaseQuantityCommand { get; }
        public ICommand DecreaseQuantityCommand { get; }

        public ICommand IncreaseProductQuantityCommand { get; }
        public ICommand DecreaseProductQuantityCommand { get; }

        public ICommand EditProductQuantityFromCashierCommand { get; }

        public ICommand PlaceOrderCommand { get; }
        public ICommand PlaceOrderCashCommand { get; }
        public ICommand PlaceOrderElectronicCommand { get; }
        public ICommand PlaceOrderCreditCommand { get; }
        public ICommand PlaceOrderOnCreditCommand { get; }
        public ICommand PlaceOrderSplitCommand { get; }

        public ICommand ApplyDiscountCommand { get; }
        public ICommand ClearDiscountCommand { get; }
        public ICommand CancelCurrentInvoiceCommand { get; }
        public ICommand ManageShiftCommand { get; }
        public ICommand LogoutCommand { get; }
        
        public ICommand HoldOrderCommand { get; }
        public ICommand ResumeOrderCommand { get; }
        public ICommand DeleteHeldOrderCommand { get; }
        public ICommand ShowHeldOrdersCommand { get; }

        public ICommand CategorySelectedCommand { get; }
        public ICommand FilterCommand { get; }

        // Navigation
        //public ICommand NavigateToDashboardCommand { get; }
        //public ICommand NavigateToOrdersCommand { get; }
        //public ICommand NavigateToCashBoxReportCommand { get; }
        //public ICommand NavigateToSettingsCommand { get; }
        public ICommand NavigateToReturnOrdersCommand { get; }

        public CashierCommands(CashierViewModel vm)
        {
            SetOrderTypeCommand = new RelayCommand<string>(vm.SetOrderType);
            SetPaymentMethodCommand = new RelayCommand<string>(vm.SetPaymentMethod);
            TogglePrintCustomerCommand = new RelayCommand(vm.TogglePrintCustomer);
            TogglePrintKitchenCommand = new RelayCommand(vm.TogglePrintKitchen);

            AddProductToOrderCommand = new RelayCommand<Product>(vm.AddProductToOrder);
            RemoveProductFromOrderCommand = new RelayCommand<OrderItem>(vm.RemoveProductFromOrder);
            IncreaseQuantityCommand = new RelayCommand<OrderItem>(vm.IncreaseQuantity);
            DecreaseQuantityCommand = new RelayCommand<OrderItem>(vm.DecreaseQuantity);

            IncreaseProductQuantityCommand = new RelayCommand<Product>(vm.IncreaseProductQuantity);
            DecreaseProductQuantityCommand = new RelayCommand<Product>(vm.DecreaseProductQuantity);
            EditProductQuantityFromCashierCommand = new RelayCommand<Product>(vm.EditProductQuantityFromCashier);

            PlaceOrderCommand = new RelayCommand(async () => await vm.PlaceOrder());
            PlaceOrderCashCommand = new RelayCommand(async () => await vm.PlaceOrderWithPayment(Models.PaymentMethod.Cash));
            PlaceOrderElectronicCommand = new RelayCommand(async () => await vm.PlaceOrderWithPayment(Models.PaymentMethod.MobilePayment));
            PlaceOrderCreditCommand = new RelayCommand(async () => await vm.PlaceOrderWithPayment(Models.PaymentMethod.CreditCard));
            PlaceOrderOnCreditCommand = new RelayCommand(async () => await vm.PlaceOrderWithPayment(Models.PaymentMethod.OnCredit));
            PlaceOrderSplitCommand = new RelayCommand(async () => await vm.PlaceOrderSplit());

            ApplyDiscountCommand = new RelayCommand(vm.ApplyDiscount);
            ClearDiscountCommand = new RelayCommand(vm.ClearDiscount);
            CancelCurrentInvoiceCommand = new RelayCommand(vm.CancelCurrentInvoice);
            ManageShiftCommand = new RelayCommand(vm.ManageShift);
            LogoutCommand = new RelayCommand(vm.Logout);

            HoldOrderCommand = new RelayCommand(vm.HoldOrder);
            ResumeOrderCommand = new RelayCommand<Models.HeldOrder>(vm.ResumeOrder);
            DeleteHeldOrderCommand = new RelayCommand<Models.HeldOrder>(vm.DeleteHeldOrder);
            ShowHeldOrdersCommand = new RelayCommand(vm.ShowHeldOrders);

            CategorySelectedCommand = new RelayCommand<string>(vm.OnCategorySelected);
            //FilterCommand = new RelayCommand(vm.PerformFilter);

            //NavigateToDashboardCommand = new RelayCommand(vm.NavigateToDashboard);
            //NavigateToOrdersCommand = new RelayCommand(vm.NavigateToOrders);
            //NavigateToCashBoxReportCommand = new RelayCommand(vm.NavigateToCashBoxReport);
            //NavigateToSettingsCommand = new RelayCommand(vm.NavigateToSettings);
            NavigateToReturnOrdersCommand = new RelayCommand(vm.NavigateToReturnOrders);
        }
    }

}
