using POSSystem.WPF.ViewModels.Settings;
using System.Windows.Controls;

namespace POSSystem.WPF.Views.AdminPages
{
    public partial class SettingsView : UserControl
    {
        private SettingsViewModel _viewModel;

        public SettingsView()
        {
            InitializeComponent();
            _viewModel = new SettingsViewModel(POSSystem.WPF.Views.AdminDashboardWindow._currentUser);
            DataContext = _viewModel;
        }


    }
}
