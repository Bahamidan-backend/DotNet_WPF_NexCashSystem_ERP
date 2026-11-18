using System;
using System.Windows.Controls;
using POSSystem.WPF.Views.AdminPages;

namespace POSSystem.WPF.Views.Popups
{
    public partial class BankDepositFormControl : UserControl
    {
        private readonly Action _close;

        public BankDepositFormControl(FinancialOverviewViewModel viewModel, Action closeOverlay)
        {
            InitializeComponent();
            DataContext = viewModel;
            _close = closeOverlay;
        }

        private void CloseButton_Click(object sender, System.Windows.RoutedEventArgs e) => _close();

        private async void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is FinancialOverviewViewModel vm && await vm.TrySaveDepositAsync())
                _close();
        }
    }
}
