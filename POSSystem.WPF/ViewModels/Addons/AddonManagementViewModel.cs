using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;
using POSSystem.WPF.Shered.Proberty;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace POSSystem.WPF.ViewModels.Addons
{
    public partial class AddonManagementViewModel : BaseViewModel, POSSystem.WPF.Services.ISearchable
    {
        public AddonManagementViewModel(OrderItem orderItem)
        {
            _orderItem = orderItem;
            
            var sp = POSSystem.WPF.App.ServiceProvider;
            _addonService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IAddonService>(sp);
            _categoryService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.ICategoryService>(sp);
            
            AddonVM = new AddonProberty();
            EditAddonVM = new EditAddonProberty();
            addonManagement = new AddonManagementProberty(_addonService);
            ServiceLocator.InventoryChanged += async () => { await LoadAddonsAsync(); await LoadStatisticsAsync(); };

            InitializeAsync().SafeFireAndForget();
        }

        public async Task InitializeAsync()
        {
            try
            {
                await LoadCategoriesAsync();
                await LoadAddonsAsync();
                await LoadStatisticsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during initialization: {ex.Message}");
            }
        }

        public void ApplySearch(string query)
        {
            addonManagement.SearchText = query ?? string.Empty;
        }

        public async Task RefreshCategoriesAsync()
        {
             await LoadCategoriesAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var categories = await _categoryService.GetAllCategoriesAsync();
                addonManagement.Categories.Clear();
                foreach (var category in categories)
                {
                    addonManagement.Categories.Add(category);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing categories: {ex.Message}");
            }
        }

        private async Task LoadAddonsAsync()
        {
            try
            {
                var addons = (await _addonService.GetAllAddonsAsync()).ToList();
                addonManagement.AllAddonsCache = addons;
                addonManagement.ApplyAddonFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading addons: {ex.Message}");
            }
        }

        public async Task LoadStatisticsAsync()
        {
            try
            {
                var stats = await _addonService.GetAddonStatisticsAsync();
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    addonManagement.TotalAddonsCount = stats.TotalAddonTypes;
                    addonManagement.TotalAddonStock = stats.TotalAddonStock;
                    addonManagement.TotalPurchaseValue = stats.TotalPurchaseValue;
                    addonManagement.TotalSoldAddons = stats.SoldAddonsCount;
                    addonManagement.TotalSalesValue = stats.TotalSoldValue;
                    addonManagement.NetProfit = stats.NetProfit;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading addon statistics: {ex.Message}");
            }
        }

    }
}
