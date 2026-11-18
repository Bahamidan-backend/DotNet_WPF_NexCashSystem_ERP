using System.Collections.ObjectModel;
using System.Linq;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.ViewModels.AnimalBatches
{
    public partial class AnimalBatchViewModel : BaseViewModel
    {
        public void FilterBatches()
        {
            if (ShowOnlyFullAnimals && Batches != null)
            {
                var filtered = new ObservableCollection<AnimalBatch>(
                    Batches.Where(b => b.IsFullAnimal)
                );
                // In real implementation, you'd update a filtered view
            }
        }
    }
}
