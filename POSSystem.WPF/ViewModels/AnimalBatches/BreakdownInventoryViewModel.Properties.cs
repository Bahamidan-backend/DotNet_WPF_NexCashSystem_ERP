using System.Collections.ObjectModel;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.ViewModels.AnimalBatches
{
    public partial class BreakdownInventoryViewModel
    {
        private readonly IAnimalBatchService _batchService;
        private readonly IUnitOfWork _unitOfWork;
        
        private int _selectedWarehouseId;
        private int _selectedTargetWarehouseId;
        private int _selectedProductId;
        private int _count = 1;

        private bool _isWeightMode = true;
        private int _portionsCount = 8;
        private decimal _averageWeight = 20;
        private decimal _averageLiverWeight = 1.0m;
        private decimal _averageBackWeight = 2.0m;

        private string _selectedProductStockText = "الرصيد المتوفر: 0";
        private int? _selectedSupplierId;

        public ObservableCollection<Warehouse> Warehouses { get; }
        public ObservableCollection<WarehouseFromProduct> AvailableProducts { get; }
        public ObservableCollection<Supplier> Suppliers { get; }

        public int SelectedWarehouseId
        {
            get => _selectedWarehouseId;
            set
            {
                if (SetProperty(ref _selectedWarehouseId, value))
                {
                    LoadProducts();
                }
            }
        }

        public int SelectedTargetWarehouseId
        {
            get => _selectedTargetWarehouseId;
            set => SetProperty(ref _selectedTargetWarehouseId, value);
        }

        public int SelectedProductId
        {
            get => _selectedProductId;
            set
            {
                if (SetProperty(ref _selectedProductId, value))
                {
                    UpdateStockText();
                }
            }
        }

        public int Count
        {
            get => _count;
            set => SetProperty(ref _count, value);
        }

        public int? SelectedSupplierId
        {
            get => _selectedSupplierId;
            set => SetProperty(ref _selectedSupplierId, value);
        }

        public bool IsWeightMode
        {
            get => _isWeightMode;
            set => SetProperty(ref _isWeightMode, value);
        }

        public int PortionsCount
        {
            get => _portionsCount;
            set => SetProperty(ref _portionsCount, value);
        }

        public decimal AverageWeight
        {
            get => _averageWeight;
            set => SetProperty(ref _averageWeight, value);
        }

        public decimal AverageLiverWeight
        {
            get => _averageLiverWeight;
            set => SetProperty(ref _averageLiverWeight, value);
        }

        public decimal AverageBackWeight
        {
            get => _averageBackWeight;
            set => SetProperty(ref _averageBackWeight, value);
        }

        public string SelectedProductStockText
        {
            get => _selectedProductStockText;
            set => SetProperty(ref _selectedProductStockText, value);
        }
    }
}
