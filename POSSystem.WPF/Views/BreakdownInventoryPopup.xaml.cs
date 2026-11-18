using System.Windows;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.ViewModels.AnimalBatches;
using POSSystem.WPF.ViewModels;

namespace POSSystem.WPF.Views
{
    public partial class BreakdownInventoryPopup : Window
    {
        public BreakdownInventoryPopup(IAnimalBatchService batchService, IUnitOfWork unitOfWork)
        {
            InitializeComponent();
            DataContext = new BreakdownInventoryViewModel(batchService, unitOfWork);
        }
    }
}
