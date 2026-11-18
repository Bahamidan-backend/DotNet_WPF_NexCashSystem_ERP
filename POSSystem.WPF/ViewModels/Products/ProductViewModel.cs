using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using POSSystem.WPF.Models;
using POSSystem.WPF.Data;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Helpers;

namespace POSSystem.WPF.ViewModels.Products
{
    public partial class ProductViewModel : BaseViewModel
    {
        public ProductViewModel()
        {
            _categories = new ObservableCollection<Category>();
            _kitchens = new ObservableCollection<POSSystem.WPF.Models.Kitchen>();
            _warehouses = new ObservableCollection<Warehouse>();
            _availableProducts = new ObservableCollection<WarehouseFromProduct>();
            
            var sp = POSSystem.WPF.App.ServiceProvider;
            _categoryService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.ICategoryService>(sp);
            _kitchenService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IKitchenService>(sp);
            _warehouseService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IWarehouseService>(sp);
            _animalBatchService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.IAnimalBatchService>(sp);
            _availableBatches = new ObservableCollection<AnimalBatch>();
            
            InitializeComponentTypes();
            POSSystem.WPF.Helpers.TaskExtensions.RunSafe(async () =>
            {
                await LoadCategoriesAsync();
                await LoadKitchensAsync();
                await LoadWarehousesAsync();
            });
        }

        private void InitializeComponentTypes()
        {
            foreach (var kvp in AnimalComponentTypeNames)
            {
                if (kvp.Key == ComponentType.None) continue;
                
                var wrapper = new ComponentSelectionWrapper { Type = kvp.Key, Name = kvp.Value };
                wrapper.PropertyChanged += (s, e) => 
                {
                    if (e.PropertyName == nameof(ComponentSelectionWrapper.IsSelected))
                    {
                        UpdateFlagFromSelections();
                    }
                };
                AvailableComponentTypes.Add(wrapper);
            }
        }

        private void UpdateSizeVisibility()
        {
            IsHabaVisible = false;
            IsNafarVisible = false;
            IsKabeerVisible = false;
            IsBasePriceVisible = true;

            if (_selectedCategory != null)
            {
                var portion = _selectedCategory.portionSize;
                if (portion == Category.PortionSize.Piece) { IsHabaVisible = true; IsBasePriceVisible = false; }
                else if (portion == Category.PortionSize.Large) { IsKabeerVisible = true; IsBasePriceVisible = false; }
                else { IsNafarVisible = true; IsBasePriceVisible = false; } // Default Single (نفر / نص نفر)
            }
        }

        private void SyncSelectionsWithFlag()
        {
            if (_animalComponentType == null) return;
            foreach (var wrapper in AvailableComponentTypes)
            {
                wrapper.IsSelected = _animalComponentType.Value.HasFlag(wrapper.Type);
            }
        }

        private void UpdateFlagFromSelections()
        {
            ComponentType flag = ComponentType.None;
            foreach (var wrapper in AvailableComponentTypes.Where(x => x.IsSelected))
            {
                flag |= wrapper.Type;
            }
            
            if (_animalComponentType != flag)
            {
                _animalComponentType = flag;
                OnPropertyChanged(nameof(AnimalComponentType));
                LoadAvailableBatches();
                UpdateAutoQuantity();
            }
        }

        private void UpdateAutoQuantity()
        {
            if (!IsAnimalComponent || SelectedBatch == null || AnimalComponentType == null || AnimalComponentType == ComponentType.None) return;

            var batch = SelectedBatch;
            decimal minAvailable = decimal.MaxValue;
            bool foundAny = false;

            if (AnimalComponentType.Value.HasFlag(ComponentType.Head)) { minAvailable = Math.Min(minAvailable, batch.HeadCount); foundAny = true; }
            if (AnimalComponentType.Value.HasFlag(ComponentType.Back)) { minAvailable = Math.Min(minAvailable, batch.BackWeight); foundAny = true; }
            if (AnimalComponentType.Value.HasFlag(ComponentType.Liver)) { minAvailable = Math.Min(minAvailable, batch.LiverWeight); foundAny = true; }
            if (AnimalComponentType.Value.HasFlag(ComponentType.Meat)) { minAvailable = Math.Min(minAvailable, batch.RemainingWeight); foundAny = true; }

            if (!foundAny) return;

            StockQuantity = minAvailable.ToString(CultureInfo.InvariantCulture);
        }

        private async void LoadAvailableBatches()
        {
            try
            {
                AvailableBatches.Clear();
                if (!AnimalComponentType.HasValue) return;

                AnimalType animalType = AnimalType.Sheep;
                if (SelectedWarehouseFromProduct != null)
                {
                    string name = SelectedWarehouseFromProduct.Name?.ToLower() ?? "";
                    if (name.Contains("بقر") || name.Contains("cow")) animalType = AnimalType.Cow;
                    else if (name.Contains("جمل") || name.Contains("camel")) animalType = AnimalType.Camel;
                }

                var batches = await _animalBatchService.GetBatchesByTypeAsync(animalType);
                foreach (var batch in batches)
                {
                    bool hasAllComponents = true;
                    if (AnimalComponentType.Value.HasFlag(ComponentType.Head) && batch.HeadCount <= 0) hasAllComponents = false;
                    if (AnimalComponentType.Value.HasFlag(ComponentType.Back) && batch.BackWeight <= 0) hasAllComponents = false;
                    if (AnimalComponentType.Value.HasFlag(ComponentType.Liver) && batch.LiverWeight <= 0) hasAllComponents = false;
                    if (AnimalComponentType.Value.HasFlag(ComponentType.Meat) && batch.RemainingWeight <= 0) hasAllComponents = false;

                    if (hasAllComponents)
                    {
                        AvailableBatches.Add(batch);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading available batches: {ex.Message}");
            }
        }

        public Dictionary<ComponentType, string> AnimalComponentTypeNames => new Dictionary<ComponentType, string>
        {
            { ComponentType.Head, "رأس" },
            { ComponentType.Back, "ظهر" },
            { ComponentType.Liver, "كبدة" },
            { ComponentType.Meat, "لحم" },
            { ComponentType.FullAnimal, "ذبيحة كاملة" }
        };

        public void AddCurrentWarehouseSelection()
        {
            if (SelectedWarehouseFromProduct != null)
            {
                if (WarehouseMappings.Any(x => x.WarehouseProductId == SelectedWarehouseFromProduct.Id))
                    return;

                WarehouseMappings.Add(new WarehouseMappingItem
                {
                    WarehouseProductId = SelectedWarehouseFromProduct.Id,
                    Name = SelectedWarehouseFromProduct.Name,
                    Quantity = 1
                });
            }
        }

        public void RemoveMapping(WarehouseMappingItem item)
        {
            WarehouseMappings.Remove(item);
        }

        public async Task LoadCategoriesAsync(bool forceRefresh = false)
        {
            try
            {
                if (!forceRefresh && _categories.Any()) return;
                _categories.Clear();
                var categories = await _categoryService.GetAllCategoriesAsync();
                foreach (var category in categories)
                {
                    _categories.Add(category);
                }
                OnPropertyChanged(nameof(Categories));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading categories: {ex.Message}");
            }
        }

        public async Task LoadKitchensAsync(bool forceRefresh = false)
        {
            try
            {
                if (!forceRefresh && _kitchens.Any()) return;
                _kitchens.Clear();
                var kitchens = await _kitchenService.GetAllKitchensAsync();
                foreach (var kitchen in kitchens)
                {
                    _kitchens.Add(kitchen);
                }
                OnPropertyChanged(nameof(Kitchens));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading kitchens: {ex.Message}");
            }
        }

        public async Task LoadWarehousesAsync(bool forceRefresh = false)
        {
            try
            {
                if (!forceRefresh && _warehouses.Any()) return;
                _warehouses.Clear();
                var warehouses = await _warehouseService.GetAllWarehousesAsync();
                foreach (var warehouse in warehouses)
                {
                    _warehouses.Add(warehouse);
                }
                OnPropertyChanged(nameof(Warehouses));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading warehouses: {ex.Message}");
            }
        }
    }

    public class ComponentSelectionWrapper : BaseViewModel
    {
        public ComponentType Type { get; set; }
        public string Name { get; set; } = string.Empty;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    public class WarehouseMappingItem : BaseViewModel
    {
        public int WarehouseProductId { get; set; }
        public string Name { get; set; } = string.Empty;

        private decimal _quantity = 1;
        public decimal Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }
    }
}
