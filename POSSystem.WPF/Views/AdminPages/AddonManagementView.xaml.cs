using POSSystem.WPF.ViewModels.Addons;
using POSSystem.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Views.AdminPages
{
    public partial class AddonManagementView : UserControl
    {
        private AddonManagementViewModel _viewModel;
        private OrderItem _OrderItem ;


        public AddonManagementView()
        {
            InitializeComponent();
            _OrderItem = new OrderItem();

            _viewModel = new AddonManagementViewModel(_OrderItem);
            DataContext = _viewModel;
        }

        private void AddAddonButton_Click(object sender, RoutedEventArgs e)
        {
            var addEditAddonControl = new POSSystem.WPF.Views.Popups.AddEditAddonControl(_viewModel);
            var adminWindow = Window.GetWindow(this) as AdminDashboardWindow;
            adminWindow?.ShowOverlay(addEditAddonControl);
        }

        private void EditAddonButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Addon addon)
            {
                var addEditAddonControl = new POSSystem.WPF.Views.Popups.AddEditAddonControl(_viewModel, addon);
                var adminWindow = Window.GetWindow(this) as AdminDashboardWindow;
                adminWindow?.ShowOverlay(addEditAddonControl);
            }
        }
    }
}
