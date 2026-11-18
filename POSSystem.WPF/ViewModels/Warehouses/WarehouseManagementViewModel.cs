using POSSystem.WPF.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows;
using POSSystem.WPF.Models;
using POSSystem.WPF.Data;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Shered.Proberty;

namespace POSSystem.WPF.ViewModels.Warehouses
{
    public partial class WarehouseManagementViewModel : BaseViewModel, POSSystem.WPF.Services.ISearchable
    {
        public WarehouseManagementViewModel()
        {
            var sp = POSSystem.WPF.App.ServiceProvider;
            _unitOfWork = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Data.UnitOfWork.IUnitOfWork>(sp);
            _warehouseService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IWarehouseService>(sp);
            _productService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IProductService>(sp);
            _categoryService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.ICategoryService>(sp);
            Commands = new WarehouseCommands(this);
            warehouseManagementProberty = new WarehouseManagementProberty();
            InitializeAsync().SafeFireAndForget();

            ServiceLocator.InventoryChanged += async () =>
            {
                try
                {
                    await LoadWarehousesAsync();
                    await LoadLowStockProductsAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"InventoryChanged error: {ex.Message}");
                }
            };
        }

        private async Task InitializeAsync()
        {
            await LoadWarehousesAsync();
            await LoadCategoriesAsync();
        }


        public async Task LoadWarehousesAsync()
        {
            try
            {
                warehouseManagementProberty.IsLoading = true;
                var warehouses = await _warehouseService.GetAllWarehousesAsync();
                warehouseManagementProberty.Warehouses.Clear();
                foreach (var w in warehouses) warehouseManagementProberty.Warehouses.Add(w);

                RefreshInventoryStatistics();
            }
            catch (Exception ex) { MessageBox.Show($"Error loading warehouses: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            finally { warehouseManagementProberty.IsLoading = false; }
        }

        public void ExecuteAddWarehouse() { }
        public void ExecuteEditWarehouse() { if (SelectedWarehouse == null) MessageBox.Show("يرجى اختيار مخزن أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); }

        public async Task LoadCategoriesAsync()
        {
            try { warehouseManagementProberty._categories.Clear(); foreach (var c in await _categoryService.GetActiveCategoriesAsync()) warehouseManagementProberty._categories.Add(c); OnPropertyChanged(nameof(warehouseManagementProberty.Categories)); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}"); }
        }

        public bool CanExecuteEditWarehouse() => SelectedWarehouse != null;
        public bool CanExecuteDeleteWarehouse() => SelectedWarehouse != null;

        public void ApplySearch(string query)
        {
            warehouseManagementProberty.SearchText = query;
            ExecuteSearch();
        }

        public async void ExecuteSearch()
        {
            try
            {
                var warehouses = await _warehouseService.GetAllWarehousesAsync();
                var searchText = warehouseManagementProberty.SearchText?.Trim();
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    warehouseManagementProberty.Warehouses.Clear();
                    foreach (var w in warehouses) warehouseManagementProberty.Warehouses.Add(w);
                    await LoadLowStockProductsAsync();
                    return;
                }
                var filtered = warehouses.Where(w =>
                    (w.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (w.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (w.Location?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (w.ManagerName?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
                warehouseManagementProberty.Warehouses.Clear();
                foreach (var w in filtered) warehouseManagementProberty.Warehouses.Add(w);

                // Also filter low stock products by search text
                var allLow = _warehouseService.GetWarehouseFromProductAll()
                    .Where(p => p.StockQuantity <= (p.MinimumStockLevel > 0 ? p.MinimumStockLevel : 5m) &&
                                ((p.Name != null && p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                                 (p.Description != null && p.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                                 (p.Warehouse?.Name != null && p.Warehouse.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))))
                    .ToList();
                warehouseManagementProberty.LowStockProducts = new ObservableCollection<WarehouseFromProduct>(allLow);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}"); }
        }

        public void ExecuteRefresh() => LoadWarehousesAsync().SafeFireAndForget();
        public void ExecuteViewProducts() { }
        public bool CanExecuteViewProducts() => SelectedWarehouse != null;

        public async Task<bool> SaveWarehouseAsync(Warehouse warehouse)
        {
            try { if (warehouse.Id == 0) await _warehouseService.CreateWarehouseAsync(warehouse); else await _warehouseService.UpdateWarehouseAsync(warehouse); LoadWarehousesAsync().SafeFireAndForget(); ServiceLocator.NotifyInventoryChanged(); return true; }
            catch (Exception ex) { MessageBox.Show($"Error saving warehouse: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return false; }
        }

        public async Task<Warehouse> GetWarehouseByType(int warehouseByid)
        {
            var find = await _warehouseService.GetWarehouseByIdAsync(warehouseByid);
            if (find == null) return null;
            var all = await _warehouseService.GetAllWarehousesAsync();
            return all.FirstOrDefault(item => item.Id == find.Id);
        }

        public async Task<bool> Deletewarehouse(Warehouse warehouse) 
        { 
            int result = await _warehouseService.DeleteWarehouseAsync(warehouse); 
            if (result == -1)
            {
                MessageBox.Show("لا يمكن حذف المخزن لوجود منتجات بداخله. يرجى نقل أو حذف المنتجات أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            LoadWarehousesAsync().SafeFireAndForget(); 
            ServiceLocator.NotifyInventoryChanged();
            return true;
        }

        public async Task LoadLowStockProductsAsync()
        {
            try
            {
                _unitOfWork.ClearEntityTracking();
                var lowStock = _warehouseService.GetWarehouseFromProductAll()
                    .Where(p => p.StockQuantity <= (p.MinimumStockLevel > 0 ? p.MinimumStockLevel : 5m))
                    .ToList();
                warehouseManagementProberty.LowStockProducts = new ObservableCollection<WarehouseFromProduct>(lowStock);
                OnPropertyChanged(nameof(warehouseManagementProberty.LowStockCount));
            }
            catch (Exception ex) { if (!ex.Message.Contains("no such column") && !ex.Message.Contains("no such table")) MessageBox.Show($"خطأ في تحميل المنتجات منخفضة المخزون: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); warehouseManagementProberty.LowStockProducts = new ObservableCollection<WarehouseFromProduct>(); }
        }

        private void RefreshInventoryStatistics()
        {
            _unitOfWork.ClearEntityTracking();
            warehouseManagementProberty._unitOfWork?.ClearEntityTracking();

            OnPropertyChanged(nameof(warehouseManagementProberty.TotalWarehouses));
            OnPropertyChanged(nameof(warehouseManagementProberty.WarehousesWithLowStock));
            OnPropertyChanged(nameof(warehouseManagementProberty.TotalInventoryValue));
            OnPropertyChanged(nameof(warehouseManagementProberty.TotalProduct));
            OnPropertyChanged(nameof(warehouseManagementProberty.LowStockCount));
            OnPropertyChanged(nameof(warehouseManagementProberty));
        }
    }
}
