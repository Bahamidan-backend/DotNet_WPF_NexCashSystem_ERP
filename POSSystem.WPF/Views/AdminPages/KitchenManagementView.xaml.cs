using System.Windows;
using System.Windows.Controls;
using POSSystem.WPF.Models;
using POSSystem.WPF.ViewModels.Kitchen;

namespace POSSystem.WPF.Views.AdminPages
{
    public partial class KitchenManagementView : UserControl
    {
        private readonly KitchenManagementViewModel _viewModel;

        public KitchenManagementView()
        {
            InitializeComponent();
            _viewModel = new KitchenManagementViewModel();
            DataContext = _viewModel;
        }

        private void AddKitchenButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.KitchenProberty.ResetNewKitchen();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Kitchen kitchen)
            {
                _viewModel.KitchenProberty.SetEditMode(kitchen);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Kitchen kitchen)
            {
                _viewModel.DeleteCommand.Execute(kitchen);
            }
        }
    }
}
