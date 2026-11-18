using POSSystem.WPF.ViewModels.Cashier;
using POSSystem.WPF.ViewModels;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Views.Components
{
    public partial class CashBoxReportView : UserControl
    {
        private CashBoxReportViewModel? _viewModel;

        public CashBoxReportView()
        {
            InitializeComponent();
        }

        public void SetUser(User user)
        {
            _viewModel = new CashBoxReportViewModel(user);
            DataContext = _viewModel;
        }

        public void RefreshReport()
        {
            _ = _viewModel?.LoadReportAsync();
        }

        public event RoutedEventHandler? BackToProductsClick;

        private void BackToProducts_Click(object sender, RoutedEventArgs e)
        {
            BackToProductsClick?.Invoke(sender, e);
        }
    }
}

