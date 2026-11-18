using POSSystem.WPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.Shered.Proberty
{
    public class ProductsProberty:BaseModel
    {


        public ObservableCollection<Product> _allProducts;
        public ObservableCollection<Product> _filteredProducts;
        public ObservableCollection<Category> _categories;
        public ObservableCollection<Warehouse> _Warehouse;
        public Dictionary<string, bool> _selectedCategories;
        public string _searchText = "";
        public bool _allCategoriesSelected = true;
        public bool _allStockSelected = true;
        public bool _lowStockSelected = false;
        public bool _outOfStockSelected = false;
        public ObservableCollection<string> _sizeUnit;
        
        private int _totalProductsCount;
        private decimal _totalStockQuantity;
        private decimal _totalStockValue;
        private decimal _totalSoldQuantity;
        private decimal _totalNetProfit;


        public ProductsProberty()
        {
            _allProducts = new ObservableCollection<Product>();
            _filteredProducts = new ObservableCollection<Product>();
            _categories = new ObservableCollection<Category>();
            _Warehouse = new ObservableCollection<Warehouse>();
            _selectedCategories = new Dictionary<string, bool>();
        }





        public ObservableCollection<Product> Products
        {
            get => _filteredProducts;
            set
            {
                _filteredProducts = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<Category> Categories => _categories;
        public ObservableCollection<string> SizeUnit => _sizeUnit;
        public ObservableCollection<Warehouse> Warehouse => _Warehouse;

        public Action? OnFilterChanged { get; set; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    OnFilterChanged?.Invoke();
                }
            }
        }

        public bool AllCategoriesSelected
        {
            get => _allCategoriesSelected;
            set
            {
                _allCategoriesSelected = value;
                OnPropertyChanged();
                OnFilterChanged?.Invoke();
            }
        }

        public bool AllStockSelected
        {
            get => _allStockSelected;
            set
            {
                if (_allStockSelected != value)
                {
                    _allStockSelected = value;
                    if (value)
                    {
                        _lowStockSelected = false;
                        _outOfStockSelected = false;
                        OnPropertyChanged(nameof(LowStockSelected));
                        OnPropertyChanged(nameof(OutOfStockSelected));
                    }
                    OnPropertyChanged(nameof(AllStockSelected));
                    OnFilterChanged?.Invoke();
                }
            }
        }

        public bool LowStockSelected
        {
            get => _lowStockSelected;
            set
            {
                if (_lowStockSelected != value)
                {
                    _lowStockSelected = value;
                    if (value)
                    {
                        _allStockSelected = false;
                        OnPropertyChanged(nameof(AllStockSelected));
                    }
                    else if (!_outOfStockSelected)
                    {
                        _allStockSelected = true;
                        OnPropertyChanged(nameof(AllStockSelected));
                    }
                    OnPropertyChanged(nameof(LowStockSelected));
                    OnFilterChanged?.Invoke();
                }
            }
        }

        public bool OutOfStockSelected
        {
            get => _outOfStockSelected;
            set
            {
                if (_outOfStockSelected != value)
                {
                    _outOfStockSelected = value;
                    if (value)
                    {
                        _allStockSelected = false;
                        OnPropertyChanged(nameof(AllStockSelected));
                    }
                    else if (!_lowStockSelected)
                    {
                        _allStockSelected = true;
                        OnPropertyChanged(nameof(AllStockSelected));
                    }
                    OnPropertyChanged(nameof(OutOfStockSelected));
                    OnFilterChanged?.Invoke();
                }
            }
        }

        public int TotalProductsCount
        {
            get => _totalProductsCount;
            set => SetProperty(ref _totalProductsCount, value);
        }

        public decimal TotalStockQuantity
        {
            get => _totalStockQuantity;
            set => SetProperty(ref _totalStockQuantity, value);
        }

        public decimal TotalStockValue
        {
            get => _totalStockValue;
            set => SetProperty(ref _totalStockValue, value);
        }

        public decimal DailySales
        {
            get => _totalSoldQuantity;
            set => SetProperty(ref _totalSoldQuantity, value);
        }

        public decimal TotalNetProfit
        {
            get => _totalNetProfit;
            set => SetProperty(ref _totalNetProfit, value);
        }






    }
}
