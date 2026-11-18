
using System.Windows;
using System.Windows.Controls;
using POSSystem.WPF.Business;
using POSSystem.WPF.ViewModels.AnimalBatches;
using POSSystem.WPF.ViewModels;

namespace POSSystem.WPF.Views
{
    public partial class AddAnimalBatchPopup : UserControl
    {
        public AddAnimalBatchPopup()
        {
            InitializeComponent();
            DataContext = new AddAnimalBatchViewModel(ServiceLocator.UnitOfWork);
        }
    }
}
