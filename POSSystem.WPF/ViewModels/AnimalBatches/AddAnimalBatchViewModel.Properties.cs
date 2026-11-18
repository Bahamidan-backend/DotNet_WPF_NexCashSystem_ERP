using POSSystem.WPF.Helpers;
using System.Collections.ObjectModel;
using POSSystem.WPF.Models;
using POSSystem.WPF.Data.UnitOfWork;

namespace POSSystem.WPF.ViewModels.AnimalBatches
{
    public partial class AddAnimalBatchViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private int _selectedAnimalTypeIndex;
        private decimal _totalWeight;
        private int _headCount = 0; 
        private decimal _backWeight = 0;
        private decimal _liverWeight = 0;
        private decimal _portionsToAdd = 0; 
        private int _selectedComponentId = 4; 
        private decimal _quantityInput = 0;

        private Warehouse? _selectedWarehouse;
        private WarehouseFromProduct? _selectedProduct;
        private ObservableCollection<WarehouseFromProduct> _products = new();
        private Supplier? _selectedSupplier;
        private bool _isWeightMode = true;
        private int _portionsCount = 8;

        public ObservableCollection<Warehouse> Warehouses { get; }
        public ObservableCollection<Supplier> Suppliers { get; }

        public ObservableCollection<WarehouseFromProduct> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        public int SelectedAnimalTypeIndex
        {
            get => _selectedAnimalTypeIndex;
            set => SetProperty(ref _selectedAnimalTypeIndex, value);
        }

        public decimal TotalWeight
        {
            get => _totalWeight;
            set => SetProperty(ref _totalWeight, value);
        }

        public int HeadCount
        {
            get => _headCount;
            set => SetProperty(ref _headCount, value);
        }

        public decimal BackWeight
        {
            get => _backWeight;
            set => SetProperty(ref _backWeight, value);
        }

        public decimal LiverWeight
        {
            get => _liverWeight;
            set => SetProperty(ref _liverWeight, value);
        }

        public bool IsWeightMode
        {
            get => _isWeightMode;
            set 
            {
                if (SetProperty(ref _isWeightMode, value))
                {
                    OnPropertyChanged(nameof(IsPortionMode));
                }
            }
        }

        public bool IsPortionMode
        {
            get => !_isWeightMode;
            set => IsWeightMode = !value;
        }

        public decimal PortionsToAdd
        {
            get => _portionsToAdd;
            set => SetProperty(ref _portionsToAdd, value);
        }

        public int PortionsCount
        {
            get => _portionsCount;
            set => SetProperty(ref _portionsCount, value);
        }

        public int SelectedComponentId
        {
            get => _selectedComponentId;
            set
            {
                if (SetProperty(ref _selectedComponentId, value))
                {
                    OnPropertyChanged(nameof(QuantityLabel));
                    IsWeightMode = SelectedComponentId != 5;
                }
            }
        }

        public decimal QuantityInput
        {
            get => _quantityInput;
            set => SetProperty(ref _quantityInput, value);
        }

        public string QuantityLabel
        {
            get
            {
                return SelectedComponentId switch
                {
                    1 => "عدد الرؤوس (حبة)",
                    2 => "وزن الظهر (كجم)",
                    3 => "وزن الكبدة (كجم)",
                    4 => "الوزن (للكيلو)",
                    5 => "عدد الأثمان (للأجزاء)",
                    _ => "الكمية / الوزن"
                };
            }
        }

        public List<dynamic> ComponentTypes => new List<dynamic>
        {
            new { Id = 4, Name = "لحم (بالكيلو)" },
            new { Id = 5, Name = "لحم (بالأثمان)" },
            new { Id = 1, Name = "طلي (رأس)" },
            new { Id = 3, Name = "كبدة" },
            new { Id = 2, Name = "ظهر" }
        };

        public Warehouse? SelectedWarehouse
        {
            get => _selectedWarehouse;
            set
            {
                if (SetProperty(ref _selectedWarehouse, value))
                {
                    if (value != null)
                    {
                        LoadProductsForWarehouseAsync(value.Id).SafeFireAndForget();
                    }
                    else
                    {
                        Products.Clear();
                    }
                }
            }
        }

        public WarehouseFromProduct? SelectedProduct
        {
            get => _selectedProduct;
            set => SetProperty(ref _selectedProduct, value);
        }

        public Supplier? SelectedSupplier
        {
            get => _selectedSupplier;
            set => SetProperty(ref _selectedSupplier, value);
        }
    }
}
