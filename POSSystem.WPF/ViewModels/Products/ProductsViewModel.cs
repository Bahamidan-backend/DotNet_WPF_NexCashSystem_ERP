using POSSystem.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using POSSystem.WPF.Models;
using POSSystem.WPF.Services;
using POSSystem.WPF.Business;
using POSSystem.WPF.Shered.Proberty;
using static POSSystem.WPF.Models.Recipe;

namespace POSSystem.WPF.ViewModels.Products
{
    public partial class ProductsViewModel : BaseViewModel, ISearchable
    {
        private readonly System.Threading.SemaphoreSlim _dbSemaphore = new System.Threading.SemaphoreSlim(1, 1);
        public ProductsViewModel()
        {
            var sp = POSSystem.WPF.App.ServiceProvider;
            _unitOfWork = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Data.UnitOfWork.IUnitOfWork>(sp);
            _categoryService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.ICategoryService>(sp);
            _warehouseService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IWarehouseService>(sp);
            _productService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IProductService>(sp);
            _saleService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.ISaleService>(sp);
            _orderService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IOrderService>(sp);

            productViewModel = new ProductViewModel();
            productsProberty = new ProductsProberty();
            productsProberty.OnFilterChanged = () => FilterProducts();

            ServiceLocator.OrderService.OrderPlaced += OnOrderPlaced;
            ServiceLocator.ReturnOrderService.ReturnProcessed += OnReturnProcessed;
            ServiceLocator.InventoryChanged += OnInventoryChanged;

            FilterProducts(false);
            InitializeAsync().SafeFireAndForget();
        }

        private void OnOrderPlaced(object? sender, Order e)
        {
            _lastCacheUpdate = DateTime.MinValue;
            FilterProducts();
        }

        private void OnReturnProcessed(object? sender, EventArgs e)
        {
            _lastCacheUpdate = DateTime.MinValue;
            LoadProductsAsync().SafeFireAndForget();
        }

        private void OnInventoryChanged()
        {
            _lastCacheUpdate = DateTime.MinValue;
            LoadProductsAsync().SafeFireAndForget();
        }

        public override void Dispose()
        {
            if (_isDisposed) return;
            
            ServiceLocator.OrderService.OrderPlaced -= OnOrderPlaced;
            ServiceLocator.ReturnOrderService.ReturnProcessed -= OnReturnProcessed;
            ServiceLocator.InventoryChanged -= OnInventoryChanged;

            base.Dispose();
        }

        public async Task InitializeAsync()
        {
            try
            {
                await LoadProductsAsync();
                await LoadCategoriesAsync();
                await LoadWarehouseAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during initialization: {ex.Message}");
            }
        }

        public async Task LoadProductsAsync()
        {
            await _dbSemaphore.WaitAsync();
            try
            {
                _unitOfWork.ClearEntityTracking();
                var products = await _productService.GetActiveProductsAsync();
                
                // Initialize StockQuantity for UI display
                foreach(var product in products)
                {
                    if (product.PersonNumberFood > 0)
                    {
                        product.StockQuantity = product.AvailableQtyInPOS / product.PersonNumberFood;
                    }
                    else
                    {
                        product.StockQuantity = product.AvailableQtyInPOS;
                    }
                }
                
                _allProductsCache = products.ToList();
                FilterProducts(false); // Instant load without delay
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading products: {ex.Message}");
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        private async Task LoadCategoriesAsync()
        {
            await _dbSemaphore.WaitAsync();
            try
            {
                productsProberty._categories.Clear();
                var categories = await _categoryService.GetAllCategoriesAsync();
                foreach (var category in categories)
                {
                    if (category.IsSubCategory == false)
                    {
                        productsProberty._categories.Add(category);
                    }
                    if (!productsProberty._selectedCategories.ContainsKey(category.Name))
                    {
                        productsProberty._selectedCategories[category.Name] = false;
                    }
                }
                OnPropertyChanged(nameof(productsProberty.Categories));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        private async Task LoadWarehouseAsync()
        {
            await _dbSemaphore.WaitAsync();
            try
            {
                productsProberty._Warehouse.Clear();
                var warehouses = await _warehouseService.GetAllWarehousesAsync();
                foreach (var warehouse in warehouses)
                {
                    productsProberty._Warehouse.Add(warehouse);
                }
                OnPropertyChanged("Warehouse"); 
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading warehouses: {ex.Message}");
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        public async void RefreshCategoriesAsync()
        {
            await LoadCategoriesAsync();
            if (productViewModel != null)
            {
                await productViewModel.LoadCategoriesAsync(forceRefresh: true);
            }
            FilterProducts();
        }

        public void FilterProducts(bool debounceSummary = true)
        {
            var filtered = _allProductsCache.Where(p => !p.IsAnimalComponent).AsEnumerable();

            string search = productsProberty._searchText?.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                filtered = filtered.Where(p =>
                    (p.Name != null && p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (p.Description != null && p.Description.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (p.CategoryName != null && p.CategoryName.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            if (!productsProberty._allCategoriesSelected)
            {
                var selectedCategories = productsProberty._selectedCategories
                    .Where(kvp => kvp.Value)
                    .Select(kvp => kvp.Key)
                    .ToHashSet();

                if (selectedCategories.Any())
                {
                    filtered = filtered.Where(p => selectedCategories.Contains(p.CategoryName));
                }
            }

            if (!productsProberty._allStockSelected)
            {
                if (productsProberty._lowStockSelected && productsProberty._outOfStockSelected)
                    filtered = filtered.Where(p => (p.TotalValue <= 10 && p.TotalValue >= 0) || (p.StockQuantity <= 10 && p.StockQuantity >= 0));
                else if (productsProberty._lowStockSelected)
                    filtered = filtered.Where(p => (p.TotalValue > 0 && p.TotalValue <= 10) || (p.StockQuantity > 0 && p.StockQuantity <= 10));
                else if (productsProberty._outOfStockSelected)
                    filtered = filtered.Where(p => p.TotalValue <= 0 || p.AvailableQtyInPOS <= 0 || p.StockQuantity <= 0);
            }

            var resultList = filtered.ToList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                productsProberty.Products = new ObservableCollection<Product>(resultList);
            });

            CalculateSummary(debounceSummary);
        }

        private async void CalculateSummary(bool debounce = true)
        {
            if (_saleService == null || _warehouseService == null) return;

            _summaryCts?.Cancel();
            _summaryCts = new System.Threading.CancellationTokenSource();
            var token = _summaryCts.Token;

            try
            {
                if (debounce) await Task.Delay(400, token);

                var products = _allProductsCache?.ToList() ?? new List<Product>();
                
                if (!products.Any())
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        productsProberty.TotalProductsCount = 0;
                        productsProberty.TotalStockQuantity = 0;
                        productsProberty.TotalStockValue = 0;
                        productsProberty.DailySales = 0;
                        productsProberty.TotalNetProfit = 0;
                    });
                    return;
                }

                bool forceRefreshCaches = DateTime.Now - _lastCacheUpdate > _cacheExpiration || _warehouseCache == null || _todaySalesCache == null;

                if (forceRefreshCaches)
                {
                    await _dbSemaphore.WaitAsync(token);
                    try
                    {
                        _warehouseCache = _warehouseService.GetWarehouseFromProductAll().ToList();
                        var todayOrders = (await _orderService.GetTodayOrdersAsync()).ToList();
                        _todaySalesCache = todayOrders.Select(o => new Sale 
                        { 
                            Id = o.Id,
                            TotalAmount = o.Total,
                            FinalAmount = o.Total,
                            CreatedAt = o.CreatedAt,
                            SaleItems = o.OrderItems.Select(oi => new SaleItem 
                            {
                                ProductId = oi.ProductId ?? 0,
                                Quantity = oi.Quantity,
                                UnitPrice = oi.UnitPrice,
                                TotalPrice = oi.TotalPrice,
                                Size = oi.Size
                            }).ToList()
                        }).ToList();

                        _lastCacheUpdate = DateTime.Now;
                    }
                    finally
                    {
                        _dbSemaphore.Release();
                    }
                }

                var warehouseList = _warehouseCache ?? new List<WarehouseFromProduct>();
                var salesList = _todaySalesCache ?? new List<Sale>();

                var summary = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();

                    var warehouseLookupByName = warehouseList
                        .Where(w => !string.IsNullOrEmpty(w.Name))
                        .ToLookup(w => w.Name.Trim(), StringComparer.OrdinalIgnoreCase);
                    var warehouseLookupById = warehouseList.ToLookup(w => w.Id);

                    decimal totalStockQty = products.Sum(p => p.TotalValue);
                    decimal totalCostValue = 0;

                    foreach (var product in products)
                    {
                        decimal roundedQty = Math.Round(product.StockQuantity, 4);
                        if (roundedQty <= 0) continue;

                        var wp = warehouseLookupByName[product.Name?.Trim() ?? ""].FirstOrDefault() 
                                 ?? warehouseLookupByName[product.TypeWarehoseFromProduct?.Trim() ?? ""].FirstOrDefault();

                        if (wp != null)
                        {
                            totalCostValue += roundedQty * wp.Price;
                        }
                        else if (product.BasePrice > 0)
                        {
                            totalCostValue += roundedQty * product.BasePrice;
                        }
                        else
                        {
                            totalCostValue += roundedQty * product.PersonNumberFood;
                        }
                    }

                    decimal todayRevenue = 0;
                    decimal todayCOGS = 0;

                    foreach (var sale in salesList.Where(s => s.SaleItems != null))
                    {
                        foreach (var item in sale.SaleItems)
                        {
                            todayRevenue += item.TotalPrice;

                            var wpCost = warehouseLookupById[item.ProductId].FirstOrDefault();
                            if (wpCost != null)
                            {
                                todayCOGS += item.Quantity * wpCost.Price;
                            }
                            else
                            {
                                var matchingProd = products.FirstOrDefault(p => p.Id == item.ProductId);
                                if (matchingProd != null)
                                {
                                    todayCOGS += item.Quantity * (matchingProd.BasePrice > 0 ? matchingProd.BasePrice : matchingProd.PersonNumberFood);
                                }
                            }
                        }
                    }

                    return new
                    {
                        Count = products.Count,
                        StockQty = totalStockQty,
                        CostValue = totalCostValue,
                        Revenue = todayRevenue,
                        Profit = todayRevenue - todayCOGS
                    };
                }, token);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    productsProberty.TotalProductsCount = summary.Count;
                    productsProberty.TotalStockQuantity = summary.StockQty;
                    productsProberty.TotalStockValue = Math.Round(summary.CostValue, 2);
                    productsProberty.DailySales = Math.Round(summary.Revenue, 2);
                    productsProberty.TotalNetProfit = Math.Round(summary.Profit, 2);
                    
                    OnPropertyChanged(nameof(productsProberty));
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating summary: {ex.Message}");
            }
        }

        public void SelectAllCategories()
        {
            LoadCategoriesAsync().SafeFireAndForget(); 
            productsProberty.AllCategoriesSelected = true;
            foreach (var key in productsProberty._selectedCategories.Keys.ToList())
            {
                productsProberty._selectedCategories[key] = false;
            }
            FilterProducts();
        }

        public void ToggleCategorySelection(string categoryName, bool isSelected)
        {
            if (productsProberty._selectedCategories.ContainsKey(categoryName))
            {
                productsProberty._selectedCategories[categoryName] = isSelected;

                if (isSelected)
                {
                    productsProberty.AllCategoriesSelected = false;
                }
                else
                {
                    if (!productsProberty._selectedCategories.Any(kvp => kvp.Value))
                    {
                        productsProberty.AllCategoriesSelected = true;
                    }
                }

                FilterProducts();
            }
        }

        public void SelectAllStock()
        {
            productsProberty.AllStockSelected = true;
            productsProberty.LowStockSelected = false;
            productsProberty.OutOfStockSelected = false;
            FilterProducts();
        }

        public void ApplySearch(string query)
        {
            productsProberty.SearchText = query;
            FilterProducts();
        }

        public void ClearAllFilters()
        {
            productsProberty.SearchText = "";
            SelectAllCategories();
            SelectAllStock();
        }

        public async Task<bool> DeleteProduct(Product product)
        {
            if (product == null) return false;

            bool hasRecipe = await _unitOfWork.Recipes.AnyAsync(r => r.ProductId == product.Id);
            if (hasRecipe)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    MessageBox.Show("لا يمكن حذف المنتج لأنه يحتوي على وصفات. يرجى حذف الوصفات أولاً.", "حذف غير مسموح", MessageBoxButton.OK, MessageBoxImage.Warning));
                return false;
            }

            bool isSuccess = false;
            await _dbSemaphore.WaitAsync();
            try
            {
                _unitOfWork.ClearEntityTracking();
                WarehouseFromProduct source = null;

                if (int.TryParse(product.TypeWarehoseFromProduct, out int warehouseProductId))
                {
                    var all = _warehouseService.GetWarehouseFromProductAll().ToList();
                    source = all.FirstOrDefault(x => x.Id == warehouseProductId);
                }

                if (source == null && !string.IsNullOrWhiteSpace(product.TypeWarehoseFromProduct))
                {
                    var all = _warehouseService.GetWarehouseFromProductAll().ToList();
                    source = all.FirstOrDefault(x => 
                        x.Name != null && x.Name.Trim().Equals(product.TypeWarehoseFromProduct.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                if (source != null)
                {
                    // Use fresh DbContext to avoid EF tracking cache issues when restoring stock
                    using (var context = new Data.POSDbContext())
                    {
                        var freshSource = context.warehouseFromProducts.Find(source.Id);
                        if (freshSource != null)
                        {
                            freshSource.StockQuantity += product.StockQuantity;
                            await context.SaveChangesAsync();
                        }
                    }

                    var componentType = GetComponentTypeFromName(source.Name);
                    if (componentType != null)
                    {
                        try
                        {
                            var animalBatchService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.IAnimalBatchService>(POSSystem.WPF.App.ServiceProvider);
                            await animalBatchService.RestoreToAnyBatchAsync(componentType.Value, product.StockQuantity);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error syncing with animal batches during product deletion: {ex.Message}");
                        }
                    }
                }

                var success = await _productService.DeleteProduct(product);
                if (success > 0) 
                {
                    isSuccess = true;
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting product: {ex.Message}");
                isSuccess = false;
            }
            finally
            {
                _dbSemaphore.Release();
            }
            
            // Reload after releasing semaphore to avoid deadlock
            await LoadProductsAsync();
            if (isSuccess)
            {
                ServiceLocator.NotifyProductsChanged();
            }
            return isSuccess;
        }

        private ComponentType? GetComponentTypeFromName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            string lowerName = name.ToLower();
            if (lowerName.Contains("لحم")) return ComponentType.Meat;
            if (lowerName.Contains("رأس")) return ComponentType.Head;
            if (lowerName.Contains("كبدة")) return ComponentType.Liver;
            if (lowerName.Contains("ظهر")) return ComponentType.Back;

            return null;
        }

        public async Task AddProduct(Product product)
        {
            if (product == null) return;

            await _dbSemaphore.WaitAsync();
            try
            {
                await _productService.CreateProductAsync(product);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"حدث خطأ أثناء حفظ المنتج في قاعدة البيانات: {ex.Message}\n{ex.InnerException?.Message}", "خطأ في الحفظ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Error adding product: {ex.Message}");
                throw; // Rethrow so AddProductControl knows it failed!
            }
            finally
            {
                _dbSemaphore.Release();
            }
            await LoadProductsAsync();
            ServiceLocator.NotifyProductsChanged();
        }

        public async Task UpdateProduct(Product product)
        {
             if (product == null) return;

            await _dbSemaphore.WaitAsync();
            try
            {
                await _productService.UpdateProduct(product);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"حدث خطأ أثناء حفظ التعديل في قاعدة البيانات: {ex.Message}\n{ex.InnerException?.Message}", "خطأ في الحفظ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Error updating product: {ex.Message}");
            }
            finally
            {
                _dbSemaphore.Release();
            }
            await LoadProductsAsync();
            ServiceLocator.NotifyProductsChanged();
        }

        public async Task<int> RefillAllOutOfStock()
        {
            await _dbSemaphore.WaitAsync();
            int count = 0;
            try
            {
                var result = await _productService.RefillAllOutOfStockAsync();
                count = result.Value;
                if (!string.IsNullOrEmpty(result.Message))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        MessageBox.Show(result.Message, result.Title ?? "رسالة", MessageBoxButton.OK,
                            result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RefillAllOutOfStock: {ex.Message}");
            }
            finally
            {
                _dbSemaphore.Release();
            }

            if (count > 0)
            {
                await LoadProductsAsync();
            }
            return count;
        }
    }
}
