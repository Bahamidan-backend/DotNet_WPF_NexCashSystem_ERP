using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using POSSystem.WPF.Models;
using POSSystem.WPF.Models.Helper;

namespace POSSystem.WPF.ViewModels.Addons
{
    public partial class AddonOrderDtilsViewModel
    {
        private readonly IAddonService _addonService;
        private readonly ICategoryService _categoryService;
        public readonly OrderItem _orderItem;
        public ObservableCollection<SizeOption> Sizes { get; set; }

        private ObservableCollection<Addon> _addons;
        private ObservableCollection<Category> _categories = new();
        private string _Name;
        private string _Description;

        public IEnumerable<Addon> SelectedAddons => Addons.Where(a => a.IsActive);
        public int LowStockCount => Addons.Count(a => a.IsLowStock);
        public decimal TotalInventoryValue => Addons.Sum(a => a.TotalValue);
        public decimal TotalAmount => Addons.Where(x => x.IsActive == true).Sum(a => a.Price);
        public decimal OrderTotalPrice => (_orderItem?.UnitPrice ?? 0) * (_orderItem?.Quantity ?? 1) + TotalAmount;

        public ObservableCollection<Addon> Addons
        {
            get => _addons;
            set
            {
                if (_addons != null)
                    _addons.CollectionChanged -= Addons_CollectionChanged;

                _addons = value;

                if (_addons != null)
                {
                    _addons.CollectionChanged += Addons_CollectionChanged;

                    foreach (var addon in _addons)
                        addon.PropertyChanged += Addon_PropertyChanged;
                }

                OnPropertyChanged();
                RecalculateTotal();
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

        public string Name
        {
            get => _Name;
            set
            {
                _Name = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _Description;
            set
            {
                _Description = value;
                OnPropertyChanged();
            }
        }
    }
}
