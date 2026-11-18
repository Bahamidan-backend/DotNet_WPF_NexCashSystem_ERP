using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using POSSystem.WPF.Models;
using POSSystem.WPF.Models.Helper;

namespace POSSystem.WPF.ViewModels.Addons
{
    public partial class AddonOrderDtilsViewModel : BaseViewModel
    {
        public AddonOrderDtilsViewModel(OrderItem orderItem)
        {
            var sp = POSSystem.WPF.App.ServiceProvider;
            _addonService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IAddonService>(sp);
            _categoryService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.ICategoryService>(sp);

            _orderItem = orderItem;
            Sizes = new ObservableCollection<SizeOption>();
            Addons = new ObservableCollection<Addon>();

            Addons.CollectionChanged += (s, e) => RecalculateTotal();
            Addons.CollectionChanged += Addons_CollectionChanged;

            LoadData(_orderItem);
            InitializeSizeOptions(_orderItem);
        }

        public void TriggerTotalUpdate()
        {
            RecalculateTotal();
        }

        private void InitializeSizeOptions(OrderItem orderitem = null)
        {
            Sizes = new ObservableCollection<SizeOption>();
            bool hasFull = false, hasHalf = false, hasQuarter = false;

            if (orderitem?.Product?.ProductSizes != null && orderitem.Product.ProductSizes.Any())
            {
                foreach (var size in orderitem.Product.ProductSizes)
                {
                    if (size.SizeName == "نفر" || size.SizeName == "حبة") hasFull = true;
                    if (size.SizeName == "نص نفر" || size.SizeName == "نص حبة") hasHalf = true;
                    if (size.SizeName == "ربع نفر" || size.SizeName == "ربع حبة") hasQuarter = true;

                    Sizes.Add(new SizeOption 
                    { 
                        Name = size.SizeName, 
                        Tag = size.Price.ToString(), 
                        IsActive = (size.SizeName == orderitem.Size) 
                    });
                }
            }
            
            if ((hasFull || hasHalf) && !hasQuarter && orderitem != null && orderitem.Product != null)
            {
                string quarterName = hasHalf && orderitem.Product.ProductSizes.Any(s => s.SizeName == "نص حبة") ? "ربع حبة" : "ربع نفر";
                decimal quarterPrice = orderitem.Product.Price / 4;
                Sizes.Add(new SizeOption 
                { 
                    Name = quarterName, 
                    Tag = quarterPrice.ToString(), 
                    IsActive = (quarterName == orderitem.Size) 
                });
            }

            if (Sizes.Any() && !Sizes.Any(s => s.IsActive))
            {
                Sizes.First().IsActive = true;
            }

            RecalculateTotal();
        }

        public async Task RefreshCategories()
        {
            var categories = await _categoryService.GetActiveCategoriesAsync();
            Categories = new ObservableCollection<Category>(categories);
        }

        private async void LoadData(OrderItem orderitem = null)
        {
            try
            {
                var addons = await _addonService.GetAddonsForOrderAsync(_orderItem);
                Addons = new ObservableCollection<Addon>(addons);
                Name = orderitem.Name;
                Description = orderitem.Notes;
                RecalculateTotal();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
            }
            RecalculateTotal();
        }

        private void RecalculateTotal()
        {
            OnPropertyChanged(nameof(LowStockCount));
            OnPropertyChanged(nameof(TotalInventoryValue));
            OnPropertyChanged(nameof(TotalAmount)); 
            OnPropertyChanged(nameof(OrderTotalPrice)); 

            try
            {
                var addonsTotal = TotalAmount; 
                var baseTotal = _orderItem.UnitPrice * _orderItem.Quantity;
                _orderItem.TotalPrice = baseTotal + addonsTotal;
            }
            catch
            {
            }
        }

        private void Addons_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Addon addon in e.NewItems)
                    addon.PropertyChanged += Addon_PropertyChanged;
            }

            if (e.OldItems != null)
            {
                foreach (Addon addon in e.OldItems)
                    addon.PropertyChanged -= Addon_PropertyChanged;
            }

            RecalculateTotal();
        }

        private void Addon_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Addon.IsActive))
            {
                RecalculateTotal(); 
                OnPropertyChanged(nameof(SelectedAddons)); 
            }
        }
    }
}
