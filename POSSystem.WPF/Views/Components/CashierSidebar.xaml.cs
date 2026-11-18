
using System.Windows;
using System.Windows.Controls;

namespace POSSystem.WPF.Views.Components
{
    public partial class CashierSidebar : UserControl
    {
        public CashierSidebar()
        {
            InitializeComponent();
        }

        public event RoutedEventHandler? NavigationButtonClick;

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset all buttons to normal style
            DashboardButton.Style = FindResource("NavigationButtonStyle") as Style;
            OrdersButton.Style = FindResource("NavigationButtonStyle") as Style;
            ReturnOrdersButton.Style = FindResource("NavigationButtonStyle") as Style;
            CashBoxReportButton.Style = FindResource("NavigationButtonStyle") as Style;
            SettingsButton.Style = FindResource("NavigationButtonStyle") as Style;

            // Set clicked button to active style
            if (sender is Button clickedButton)
            {
                clickedButton.Style = FindResource("ActiveNavigationButtonStyle") as Style;
            }

            NavigationButtonClick?.Invoke(sender, e);
        }
    }
}
