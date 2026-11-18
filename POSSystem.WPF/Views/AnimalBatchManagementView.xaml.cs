using System.Windows.Controls;
using System.Windows;
using POSSystem.WPF.Business;
using POSSystem.WPF.ViewModels.AnimalBatches;
using POSSystem.WPF.ViewModels;

namespace POSSystem.WPF.Views
{
    public partial class AnimalBatchManagementView : UserControl
    {
        private readonly UnitOfWork _localUnitOfWork;

        public AnimalBatchManagementView()
        {
            InitializeComponent();
            // Use local UnitOfWork to avoid concurrency issues with other ViewModels sharing ServiceLocator.UnitOfWork
            _localUnitOfWork = new UnitOfWork(new POSDbContext());
            var localBatchService = new AnimalBatchService(_localUnitOfWork);
            DataContext = new AnimalBatchManagementViewModel(localBatchService, _localUnitOfWork);
        }

        private void BatchRowActionsMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;
            if (button.ContextMenu is null) return;

            // Open the menu on normal left click.
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            button.ContextMenu.IsOpen = true;
        }
    }
}
