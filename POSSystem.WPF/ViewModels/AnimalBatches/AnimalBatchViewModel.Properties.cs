using System.Collections.ObjectModel;
using System.Linq;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.ViewModels.AnimalBatches
{
    public partial class AnimalBatchViewModel
    {
        private AnimalBatch _selectedBatch;
        private ObservableCollection<AnimalBatch> _batches;
        private int _selectedAnimalTypeId;
        private bool _showOnlyFullAnimals;

        public AnimalBatch SelectedBatch
        {
            get => _selectedBatch;
            set => SetProperty(ref _selectedBatch, value);
        }

        public ObservableCollection<AnimalBatch> Batches
        {
            get => _batches;
            set => SetProperty(ref _batches, value);
        }

        public int SelectedAnimalTypeId
        {
            get => _selectedAnimalTypeId;
            set
            {
                SetProperty(ref _selectedAnimalTypeId, value);
                OnPropertyChanged(nameof(SelectedAnimalType));
            }
        }

        public bool ShowOnlyFullAnimals
        {
            get => _showOnlyFullAnimals;
            set
            {
                if (SetProperty(ref _showOnlyFullAnimals, value))
                {
                    if (value)
                    {
                        FilterBatches();
                    }
                }
            }
        }

        public AnimalType SelectedAnimalType => (AnimalType)SelectedAnimalTypeId;

        public int TotalBatches => Batches?.Count ?? 0;
        public int FullAnimalsCount => Batches?.Count(b => b.IsFullAnimal) ?? 0;
        public int BrokenBatchesCount => TotalBatches - FullAnimalsCount;
    }
}
