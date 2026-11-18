using System.Collections.ObjectModel;
using POSSystem.WPF.Models;
using POSSystem.WPF.Business.Services;

namespace POSSystem.WPF.ViewModels.Products
{
    public partial class ProductViewModel
    {
        private string _name = string.Empty;
        private string _description = string.Empty;
        private decimal _price = 0;
        private string _personNumberFood = "1";
        private string _stockQuantity = "0";
        private string _lastCookedQty = "0";
        private string _yieldRate = "0";
        private string _imagePath = string.Empty;
        private string _typeWarehoseFromProduct = string.Empty;
        private Category? _selectedCategory;
        private POSSystem.WPF.Models.Kitchen? _selectedKitchen;
        private Warehouse? _selectedWarehouse;
        private WarehouseFromProduct? _selectedWarehouseFromProduct;
        private Models.Recipe.PortionUnit _portionUnits;
        private bool _isActive = true;
        private ObservableCollection<Category> _categories;
        private ObservableCollection<POSSystem.WPF.Models.Kitchen> _kitchens;
        private ObservableCollection<Warehouse> _warehouses;
        private ObservableCollection<WarehouseFromProduct> _availableProducts;
        private POSSystem.WPF.Business.Services.interfase.ICategoryService _categoryService;
        private POSSystem.WPF.Business.Services.interfase.IKitchenService _kitchenService;
        private POSSystem.WPF.Business.Services.interfase.IWarehouseService _warehouseService;
        private readonly IAnimalBatchService _animalBatchService;

        private string _priceHaba = "0";
        private string _priceNusHaba = "0";
        private string _priceRobHaba = "0";
        private string _priceNafar = "0";
        private string _priceNusNafar = "0";
        private string _priceRobNafar = "0";
        private string _priceKabeer = "0";
        private string _priceWasat = "0";
        private string _priceSagheer = "0";

        private bool _isHabaVisible;
        private bool _isNafarVisible;
        private bool _isKabeerVisible;
        private bool _isBasePriceVisible = true;
        private bool _isLoading;

        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
        public string Name { get => _name; set => SetProperty(ref _name, value); }
        public string Description { get => _description; set => SetProperty(ref _description, value); }
        public decimal Price { get => _price; set => SetProperty(ref _price, value); }
        public string PersonNumberFood { get => _personNumberFood; set => SetProperty(ref _personNumberFood, value); }
        public string StockQuantity { get => _stockQuantity; set => SetProperty(ref _stockQuantity, value); }
        public string LastCookedQty { get => _lastCookedQty; set => SetProperty(ref _lastCookedQty, value); }
        public string YieldRate { get => _yieldRate; set => SetProperty(ref _yieldRate, value); }
        public string ImagePath { get => _imagePath; set => SetProperty(ref _imagePath, value); }
        public string TypeWarehoseFromProduct { get => _typeWarehoseFromProduct; set => SetProperty(ref _typeWarehoseFromProduct, value); }
        
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set { if (SetProperty(ref _selectedCategory, value)) UpdateSizeVisibility(); }
        }

        public POSSystem.WPF.Models.Kitchen? SelectedKitchen
        {
            get => _selectedKitchen;
            set => SetProperty(ref _selectedKitchen, value);
        }

        public string PriceHaba { get => _priceHaba; set => SetProperty(ref _priceHaba, value); }
        public string PriceNusHaba { get => _priceNusHaba; set => SetProperty(ref _priceNusHaba, value); }
        public string PriceRobHaba { get => _priceRobHaba; set => SetProperty(ref _priceRobHaba, value); }
        public string PriceNafar { get => _priceNafar; set => SetProperty(ref _priceNafar, value); }
        public string PriceNusNafar { get => _priceNusNafar; set => SetProperty(ref _priceNusNafar, value); }
        public string PriceRobNafar { get => _priceRobNafar; set => SetProperty(ref _priceRobNafar, value); }
        public string PriceKabeer { get => _priceKabeer; set => SetProperty(ref _priceKabeer, value); }
        public string PriceWasat { get => _priceWasat; set => SetProperty(ref _priceWasat, value); }
        public string PriceSagheer { get => _priceSagheer; set => SetProperty(ref _priceSagheer, value); }

        public bool IsHabaVisible { get => _isHabaVisible; set => SetProperty(ref _isHabaVisible, value); }
        public bool IsNafarVisible { get => _isNafarVisible; set => SetProperty(ref _isNafarVisible, value); }
        public bool IsKabeerVisible { get => _isKabeerVisible; set => SetProperty(ref _isKabeerVisible, value); }
        public bool IsBasePriceVisible { get => _isBasePriceVisible; set => SetProperty(ref _isBasePriceVisible, value); }

        public Warehouse? SelectedWarehouse { get => _selectedWarehouse; set => SetProperty(ref _selectedWarehouse, value); }
        public WarehouseFromProduct? SelectedWarehouseFromProduct { get => _selectedWarehouseFromProduct; set => SetProperty(ref _selectedWarehouseFromProduct, value); }
        public Models.Recipe.PortionUnit PortionUnits { get => _portionUnits; set => SetProperty(ref _portionUnits, value); }
        public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }

        private bool _isAnimalComponent;
        public bool IsAnimalComponent
        {
            get => _isAnimalComponent;
            set { if (SetProperty(ref _isAnimalComponent, value)) OnPropertyChanged(nameof(IsWarehouseVisible)); }
        }

        public bool IsWarehouseVisible => !IsAnimalComponent;
        public ObservableCollection<ComponentSelectionWrapper> AvailableComponentTypes { get; } = new();

        private ComponentType? _animalComponentType;
        public ComponentType? AnimalComponentType
        {
            get => _animalComponentType;
            set
            {
                if (SetProperty(ref _animalComponentType, value))
                {
                    SyncSelectionsWithFlag();
                    LoadAvailableBatches();
                    UpdateAutoQuantity();
                }
            }
        }

        private ObservableCollection<AnimalBatch> _availableBatches;
        public ObservableCollection<AnimalBatch> AvailableBatches => _availableBatches;

        private AnimalBatch? _selectedBatch;
        public AnimalBatch? SelectedBatch
        {
            get => _selectedBatch;
            set { if (SetProperty(ref _selectedBatch, value)) UpdateAutoQuantity(); }
        }

        public ObservableCollection<Category> Categories => _categories;
        public ObservableCollection<POSSystem.WPF.Models.Kitchen> Kitchens => _kitchens;
        public ObservableCollection<Warehouse> Warehouses => _warehouses;
        public ObservableCollection<WarehouseFromProduct> AvailableProducts => _availableProducts;
        public ObservableCollection<WarehouseMappingItem> WarehouseMappings { get; } = new();
    }
}
