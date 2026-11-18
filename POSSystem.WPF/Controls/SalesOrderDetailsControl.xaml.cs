using POSSystem.WPF.Models;
using POSSystem.WPF.StaticFunction;
using System;
using System.Windows;
using System.Windows.Controls;

namespace POSSystem.WPF.Controls
{
    public partial class SalesOrderDetailsControl : UserControl
    {
        public Order Order { get; private set; } = null!;
        public event EventHandler? CloseRequested;

        public SalesOrderDetailsControl()
        {
            InitializeComponent();
        }

        public void Initialize(Order order)
        {
            Order = order;
            DataContext = order;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ReprintButton_Click(object sender, RoutedEventArgs e)
        {
            _ = Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                GloublStatic.PrintReceipt(Order);
            }));
            MessageBox.Show($"تم إعادة طباعة فاتورة {Order.OrderNumber}",
                "طباعة", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
