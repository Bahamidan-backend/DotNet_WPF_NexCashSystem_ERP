

namespace POSSystem.WPF.Views.Components
{
    public partial class OrdersView : UserControl
    {
        public OrdersView()
        {
            InitializeComponent();
        }

        public event RoutedEventHandler? BackToProductsClick;
        public event RoutedEventHandler? ViewOrderDetailsClick;
        public event RoutedEventHandler? ReprintOrderClick;
        public event RoutedEventHandler? CancelOrderClick;
        public event RoutedEventHandler? RefreshOrdersClick;

        private void BackToProducts_Click(object sender, RoutedEventArgs e)
        {
            BackToProductsClick?.Invoke(sender, e);
        }

        private void ViewOrderDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Order order)
            {
                ViewOrderDetailsClick?.Invoke(order, e);
            }
        }

        private void ReprintOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Order order)
            {
                ReprintOrderClick?.Invoke(order, e);
            }
        }

        private void CancelOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Order order)
            {
                CancelOrderClick?.Invoke(order, e);
            }
        }

        private void RefreshOrders_Click(object sender, RoutedEventArgs e)
        {
            RefreshOrdersClick?.Invoke(sender, e);
        }

        private void OrderCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is Order order)
            {
                ViewOrderDetailsClick?.Invoke(order, e);
            }
        }
    }
}
