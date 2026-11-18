using System.Windows;
using System.Windows.Controls;
using POSSystem.WPF.ViewModels.Cashier;
using POSSystem.WPF.ViewModels;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Views.Popups
{
    public partial class HeldOrdersPopup : Window
    {
        private readonly CashierViewModel _viewModel;
        public HeldOrder? SelectedOrder { get; private set; }

        public HeldOrdersPopup(CashierViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is HeldOrder order)
            {
                SelectedOrder = order;
                DialogResult = true;
                Close();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is HeldOrder order)
            {
                var confirm = MessageBox.Show(
                    $"هل أنت متأكد من حذف الطلب المعلق الخاص بـ '{order.CustomerName}'؟",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    _viewModel.DeleteHeldOrder(order);
                    if (_viewModel.HeldOrders == null || _viewModel.HeldOrders.Count == 0)
                    {
                        DialogResult = false;
                        Close();
                    }
                }
            }
        }
    }
}
