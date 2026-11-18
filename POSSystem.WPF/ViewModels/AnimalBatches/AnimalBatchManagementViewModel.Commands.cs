using System.Windows.Input;

namespace POSSystem.WPF.ViewModels.AnimalBatches
{
    public partial class AnimalBatchManagementViewModel
    {
        public ICommand LoadBatchesCommand { get; }
        public ICommand AddBatchCommand { get; }
        public ICommand ShowDetailsCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand BreakdownFromInventoryCommand { get; }
        public ICommand AddPartialCarcassCommand { get; }
        public ICommand DeleteBatchCommand { get; }
    }
}
