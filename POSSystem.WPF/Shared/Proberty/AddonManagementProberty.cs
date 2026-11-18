using POSSystem.WPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.Shered.Proberty
{
    public class AddonManagementProberty:BaseModel
    {


        private readonly IAddonService _addonService;


        public Addon? _selectedAddon;
        public Category? _selectedCategory;
        public string _searchText = string.Empty;
        public bool _showLowStockOnly = false;
        public  ObservableCollection<Addon> _addons = new();
        public ObservableCollection<Category> _categories = new();

        //public OrderItem _orderItem;
        //public AddonViewModel AddonVM { get; set; }
        //public EditAddonViewModel EditAddonVM { get; set; }





        public AddonManagementProberty(IAddonService addonService)
        {
            _addonService = addonService;
        }





        public ObservableCollection<Addon> Addons
        {
            get => _addons;
            set
            {
                _addons = value;
                OnPropertyChanged(nameof(LowStockCount));
                OnPropertyChanged(nameof(TotalInventoryValue));
            }
        }
        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set
            {
                _categories = value;
                OnPropertyChanged();
            }
        }

        public Addon? SelectedAddon
        {
            get => _selectedAddon;
            set
            {
                _selectedAddon = value;
                OnPropertyChanged();
            }
        }
        public List<Addon> AllAddonsCache { get; set; } = new();

        public void ApplyAddonFilters()
        {
            if (AllAddonsCache == null) return;
            IEnumerable<Addon> filtered = AllAddonsCache;

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var q = _searchText.Trim();
                filtered = filtered.Where(a =>
                    (a.Name != null && a.Name.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (a.Description != null && a.Description.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (a.Category?.Name != null && a.Category.Name.Contains(q, StringComparison.OrdinalIgnoreCase)));
            }

            if (_selectedCategory != null && _selectedCategory.Id > 0)
            {
                filtered = filtered.Where(a => a.CategoryId == _selectedCategory.Id);
            }

            if (_showLowStockOnly)
            {
                filtered = filtered.Where(a => a.IsLowStock);
            }

            _addons.Clear();
            foreach (var item in filtered)
            {
                _addons.Add(item);
            }
            OnPropertyChanged(nameof(Addons));
            OnPropertyChanged(nameof(LowStockCount));
            OnPropertyChanged(nameof(TotalInventoryValue));
        }

        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged(nameof(SelectedCategory));
                ApplyAddonFilters();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    ApplyAddonFilters();
                }
            }
        }
        public bool ShowLowStockOnly
        {
            get => _showLowStockOnly;
            set
            {
                if (_showLowStockOnly != value)
                {
                    _showLowStockOnly = value;
                    OnPropertyChanged();
                    ApplyAddonFilters();
                }
            }
        }

        public int LowStockCount => Addons.Count(a => a.IsLowStock);
        public decimal TotalInventoryValue => Addons.Sum(a => a.TotalValue);

        private int _totalAddonsCount;
        public int TotalAddonsCount { get => _totalAddonsCount; set { _totalAddonsCount = value; OnPropertyChanged(); } }

        private decimal _totalAddonsStock;
        public decimal TotalAddonStock { get => _totalAddonsStock; set { _totalAddonsStock = value; OnPropertyChanged(); } }

        private decimal _totalPurchaseValue;
        public decimal TotalPurchaseValue { get => _totalPurchaseValue; set { _totalPurchaseValue = value; OnPropertyChanged(); } }

        private int _totalSoldAddons;
        public int TotalSoldAddons { get => _totalSoldAddons; set { _totalSoldAddons = value; OnPropertyChanged(); } }

        private decimal _totalSalesValue;
        public decimal TotalSalesValue { get => _totalSalesValue; set { _totalSalesValue = value; OnPropertyChanged(); } }

        private decimal _netProfit;
        public decimal NetProfit { get => _netProfit; set { _netProfit = value; OnPropertyChanged(); } }





    }
}
