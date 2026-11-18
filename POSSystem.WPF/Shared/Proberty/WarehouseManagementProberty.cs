using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;
using POSSystem.WPF.Shered.Commands;
using POSSystem.WPF.ViewModels.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static POSSystem.WPF.Models.Recipe;

namespace POSSystem.WPF.Shered.Proberty
{
    public class WarehouseManagementProberty:BaseModel
    {
        public readonly IWarehouseService _warehouseService;
        public readonly IPurchaseService _purchaseService;
        public ObservableCollection<Warehouse> _warehouses;
        public UnitOfWork? _unitOfWork;
        public string _searchText = string.Empty;
        public bool _isLoading = false;
        public ObservableCollection<Category> _categories;
        public PortionUnit _portionUnits;

        // Purchase management properties
        public ObservableCollection<Purchase> _purchases = new();
        public ObservableCollection<WarehouseFromProduct> _lowStockProducts = new();
        public DateTime _startDate = DateTime.Today.AddMonths(-1);
        public DateTime _endDate = DateTime.Today;
        public Purchase? _selectedPurchase;
        public string _purchaseSearchText = "";


        private string _name = string.Empty;
        private int _Id;
        private string _description = string.Empty;
        private decimal _price = 0;
        private int _stockQuantity = 0;
        private Category? _selectedCategory;
        private bool _isActive = true;


        public WarehouseManagementProberty()
        {
            _unitOfWork = new UnitOfWork(new Data.POSDbContext());
            _warehouseService = new Business.Services.WarehouseService(_unitOfWork);
            _purchaseService = new PurchaseService(_unitOfWork);
            _warehouses = new ObservableCollection<Warehouse>();
            _categories = new ObservableCollection<Category>();


            


            // Initialize collections
            Purchases = new ObservableCollection<Purchase>();
            LowStockProducts = new ObservableCollection<WarehouseFromProduct>();


        }




        public ObservableCollection<Category> Categories => _categories;






        public ObservableCollection<Warehouse> Warehouses
        {
            get => _warehouses;
            set => SetProperty(ref _warehouses, value);
        }

        public PortionUnit PortionUnits
        {
            get => _portionUnits;
            set => SetProperty(ref _portionUnits, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }





        public new string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public new string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public decimal Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        public new int Id
        {
            get => _Id;
            set => SetProperty(ref _Id, value);
        }

        public int StockQuantity
        {
            get => _stockQuantity;
            set => SetProperty(ref _stockQuantity, value);
        }

        private decimal _minimumStockLevel = 5m;
        public decimal MinimumStockLevel
        {
            get => _minimumStockLevel;
            set => SetProperty(ref _minimumStockLevel, value);
        }



        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }


        public new bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }


        public IEnumerable<WarehouseFromProduct> GetWarehouseFromProductTotal()
        {

            return _warehouseService.GetWarehouseFromProductAll();
        }

        // Computed properties
        public int TotalWarehouses => Warehouses.Count;
        public int ActiveWarehouses => Warehouses.Count(w => w.IsActive);
        public int WarehousesWithLowStock => Warehouses.Count(w => w.LowStockProducts > 0);
        public decimal TotalInventoryValue => GetWarehouseFromProductTotal().Sum(x => x.TotalValue);
        public int TotalProduct => _unitOfWork.warehouseFromProduct.GetCountWarehouseFromProduct();

        public int CountToProduct => _unitOfWork.warehouseFromProduct.GetCountforProductInsaedWarehouseFromProduct();












        // Purchase management properties
        public ObservableCollection<Purchase> Purchases
        {
            get => _purchases;
            set => SetProperty(ref _purchases, value);
        }

        public ObservableCollection<WarehouseFromProduct> LowStockProducts
        {
            get => _lowStockProducts;
            set => SetProperty(ref _lowStockProducts, value);
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                    LoadPurchasesAsync().SafeFireAndForget();
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                    LoadPurchasesAsync().SafeFireAndForget();
            }
        }

        public Purchase? SelectedPurchase
        {
            get => _selectedPurchase;
            set => SetProperty(ref _selectedPurchase, value);
        }

        public string PurchaseSearchText
        {
            get => _purchaseSearchText;
            set => SetProperty(ref _purchaseSearchText, value);
        }

        public decimal TotalPurchases => Purchases.Sum(p => p.TotalAmount);
        public int LowStockCount => LowStockProducts.Count;



        public async Task LoadPurchasesAsync()
        {
            try
            {
                var purchases = await _purchaseService.GetByDateRangeAsync(StartDate, EndDate);
                Purchases = new ObservableCollection<Purchase>(purchases);
                OnPropertyChanged(nameof(TotalPurchases));
            }
            catch (Exception ex)
            {
                // Only show error if it's not a table/column missing error
                if (!ex.Message.Contains("no such column") && !ex.Message.Contains("no such table"))
                {
                    MessageBox.Show($"خطأ في تحميل المشتريات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                Purchases = new ObservableCollection<Purchase>();
            }
        }


    }
}
