using System.Threading.Tasks;
using POSSystem.WPF.ViewModels.Orders;
using System.Windows.Input;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Shered.Commands
{
   
        public class OrdersCommands
        {
            public ICommand RefreshOrdersCommand { get; }
            public ICommand ViewOrderDetailsCommand { get; }
            public ICommand ReprintOrderCommand { get; }
            public ICommand CancelOrderCommand { get; }
            public ICommand CompleteOrderCommand { get; }

            public OrdersCommands(OrdersViewModel vm)
            {
                RefreshOrdersCommand =
                    new RelayCommand(() => vm.RefreshOrders());

                ViewOrderDetailsCommand =
                    new RelayCommand<Order>(order => vm.ViewOrderDetails(order));

                ReprintOrderCommand =
                    new RelayCommand<Order>(order => vm.ReprintOrder(order));

                CancelOrderCommand =
                    new RelayCommand<Order>(order => vm.CancelOrder(order));

                CompleteOrderCommand =
                    new RelayCommand<Order>(order => vm.CompleteOrder(order));
            }
        }
    
}
//mapy your Cansel this