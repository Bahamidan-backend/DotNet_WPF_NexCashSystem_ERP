using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.ViewModels.AnimalBatches
{
    public partial class AnimalBatchManagementViewModel
    {
        private readonly IAnimalBatchService _batchService;
        private readonly IUnitOfWork _unitOfWork;
        private ObservableCollection<AnimalBatch> _batches;
        private List<AnimalBatch> _allBatches = new();
        private int _selectedAnimalTypeId;
        private bool _showOnlyFullAnimals;
        private AnimalBatch _selectedBatch;

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
                FilterBatches();
            }
        }

        public bool ShowOnlyFullAnimals
        {
            get => _showOnlyFullAnimals;
            set
            {
                SetProperty(ref _showOnlyFullAnimals, value);
                FilterBatches();
            }
        }

        public AnimalBatch SelectedBatch
        {
            get => _selectedBatch;
            set => SetProperty(ref _selectedBatch, value);
        }

        public int TotalBatches => _allBatches.Count;
        public int FullAnimalsCount => _allBatches.Count(b => b.IsFullAnimal);
        public int BrokenBatchesCount => _allBatches.Count(b => !b.IsFullAnimal);

        public List<dynamic> AnimalTypes => new List<dynamic>
        {
            new { Id = 0, Name = "الكل" },
            new { Id = 1, Name = "خروف" },
            new { Id = 2, Name = "بقرة" },
            new { Id = 3, Name = "جمل" }
        };
    }
}
