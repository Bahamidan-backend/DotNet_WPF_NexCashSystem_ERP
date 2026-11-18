using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using POSSystem.WPF.ViewModels.Cashier;

namespace POSSystem.WPF.Views.Popups
{
    public partial class ShiftManagementPopup : Window
    {
        public ShiftManagementPopup(int userId, bool hasPendingOrders = false)
        {
            InitializeComponent();
            var viewModel = new ShiftManagementViewModel(userId, hasPendingOrders);
            viewModel.CloseRequested += (s, e) => Close();
            DataContext = viewModel;
        }

        private void NumberValidationTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void PersonalWithdrawal_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ShiftManagementViewModel viewModel)
            {
                viewModel.shiftManagementProberty.IsPersonalWithdrawal = true;
            }
        }

        private void RestaurantWithdrawal_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ShiftManagementViewModel viewModel)
            {
                viewModel.shiftManagementProberty.IsRestaurantWithdrawal = true;
            }
        }
    
        private void AddCustomWalletOnShift_Click(object sender, RoutedEventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox(
                "أدخل اسم المحفظة أو الحساب البنكي الجديد لإضافته إلى النظام وبطاقات الحسابات المالية:",
                "إضافة محفظة جديدة",
                "");

            if (!string.IsNullOrWhiteSpace(input))
            {
                var trimmed = input.Trim();
                POSSystem.WPF.Helpers.FinancialChannelHelper.AddCustomWallet(trimmed);
                if (DataContext is POSSystem.WPF.ViewModels.Cashier.ShiftManagementViewModel vm)
                {
                    vm.shiftManagementProberty.RefreshDepositBanksList();
                    vm.shiftManagementProberty.EndShiftBankName = trimmed;
                }
            }
        }

    }
}
