using POSSystem.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using POSSystem.WPF.ViewModels.Orders;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Models;
using static POSSystem.WPF.Models.Category;
using POSSystem.WPF.Services;
using POSSystem.WPF.Shered.Commands;
using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.StaticFunction;
using POSSystem.WPF.Data;
using POSSystem.WPF.Data.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

namespace POSSystem.WPF.ViewModels.Cashier
{
    public partial class CashierViewModel : BaseViewModel
    {
        public event EventHandler? ShowReturnOrdersRequested;
        public OrdersViewModel OrdersVM => _ordersViewModel;
        private IEnumerable<Discount> _activeDiscounts = new List<Discount>();

        public CashierViewModel(User user)
        {
            _currentUser = user;
            _navigationService = NavigationService.Instance;
            
            var sp = POSSystem.WPF.App.ServiceProvider;
            _unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            _productService = sp.GetRequiredService<IProductService>();
            _orderService = sp.GetRequiredService<IOrderService>();
            _categoryService = sp.GetRequiredService<ICategoryService>();
            _shiftService = sp.GetRequiredService<IShiftService>();
            _employeeService = sp.GetRequiredService<IEmployeeService>();
            _addonService = sp.GetRequiredService<IAddonService>();
            _discountsServices = sp.GetRequiredService<IDiscountsServices>();
            _returnOrderService = sp.GetRequiredService<IReturnOrderService>();
            _heldOrderService = sp.GetRequiredService<IHeldOrderService>();

            _ordersViewModel = new OrdersViewModel(user);
            
            _categories = new ObservableCollection<Category>();
            _selectedCategories = new Dictionary<string, bool>();
            _filteredProducts = new ObservableCollection<Product>();
            _allProducts = new List<Product>();
            
            Commands = new CashierCommands(this);
            cashierProberty = new CashierProberty();

            InitializeAsync().SafeFireAndForget();

            _returnOrderService.ReturnProcessed += OnReturnProcessed;
            POSSystem.WPF.Business.ServiceLocator.ProductsChanged += OnProductsChanged;
            POSSystem.WPF.Business.ServiceLocator.InventoryChanged += OnProductsChanged;
            POSSystem.WPF.Business.ServiceLocator.AddonsChanged += OnProductsChanged;
            POSSystem.WPF.Business.ServiceLocator.CategoriesChanged += OnCategoriesChanged;
            POSSystem.WPF.Business.ServiceLocator.DiscountsChanged += OnDiscountsChanged;
            POSSystem.WPF.Business.ServiceLocator.UserPermissionsChanged += OnUserPermissionsChanged;
            SettingsService.OnSettingsChanged += OnSystemSettingsChanged;
        }

        private void OnProductsChanged()
        {
            Application.Current?.Dispatcher?.InvokeAsync(async () => {
                await LoadProductsAsync();
                FilterProducts();
            });
        }

        private void OnCategoriesChanged()
        {
            Application.Current?.Dispatcher?.InvokeAsync(async () => {
                await LoadCategoriesAsync();
                await LoadProductsAsync();
                FilterProducts();
            });
        }

        private void OnUserPermissionsChanged(User updatedUser)
        {
            if (updatedUser == null || _currentUser == null || updatedUser.Id != _currentUser.Id) return;
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                _currentUser.Name = updatedUser.Name;
                _currentUser.JobTitle = updatedUser.JobTitle;
                _currentUser.Role = updatedUser.Role;
                _currentUser.RoleTemplateName = updatedUser.RoleTemplateName;
                _currentUser.PermissionsJson = updatedUser.PermissionsJson;
                OnPropertyChanged(nameof(CanAccessAdminPanel));
                OnPropertyChanged(nameof(CanEditCartItemPrice));
                OnPropertyChanged(nameof(IsCartPriceReadOnly));
                OnPropertyChanged(nameof(RoleBadgeDisplayText));
                OnPropertyChanged(nameof(WelcomeMessage));
            });
        }

        private void RefreshAvailableDiscountsList()
        {
            if (cashierProberty == null) return;
            var currentCode = cashierProberty.DiscountCode;
            cashierProberty.AvailableDiscounts.Clear();
            if (_activeDiscounts != null)
            {
                foreach (var d in _activeDiscounts.Where(x => x != null && x.IsActive))
                {
                    cashierProberty.AvailableDiscounts.Add(d);
                }
            }
            if (!string.IsNullOrWhiteSpace(currentCode))
            {
                cashierProberty.SelectedDiscount = cashierProberty.AvailableDiscounts
                    .FirstOrDefault(d => string.Equals(d.Code, currentCode, StringComparison.OrdinalIgnoreCase));
            }
        }

        private void OnDiscountsChanged()
        {
            Application.Current?.Dispatcher?.InvokeAsync(async () => {
                _unitOfWork.ClearEntityTracking();
                _activeDiscounts = await _discountsServices.GetAllDiscounts();
                RefreshAvailableDiscountsList();
            });
        }

        private void OnSystemSettingsChanged()
        {
            Application.Current?.Dispatcher?.Invoke(() => {
                settingsProberty = SettingsService.LoadSettings();
                FilterProducts();
                OnPropertyChanged(nameof(cashierProberty));
            });
        }

        public async Task RefreshAllCashierDataAsync()
        {
            try
            {
                _unitOfWork.ClearEntityTracking();
                var freshUser = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                    Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(_unitOfWork.GetContext().Users),
                    u => u.Id == _currentUser.Id);
                if (freshUser != null)
                {
                    OnUserPermissionsChanged(freshUser);
                }
                settingsProberty = SettingsService.LoadSettings();
                await LoadCategoriesAsync();
                await LoadProductsAsync();
                _activeDiscounts = await _discountsServices.GetAllDiscounts();
                Application.Current?.Dispatcher?.Invoke(RefreshAvailableDiscountsList);
                await LoadDeliveryDrivers();
                await LoadDeliveryCustomersAsync();
                FilterProducts();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing cashier data: {ex.Message}");
            }
        }

        private void OnReturnProcessed(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(async () => {
                await LoadProductsAsync();
            });
        }

        public override void Dispose()
        {
            if (_isDisposed) return;
            
            POSSystem.WPF.Business.ServiceLocator.ProductsChanged -= OnProductsChanged;
            POSSystem.WPF.Business.ServiceLocator.InventoryChanged -= OnProductsChanged;
            POSSystem.WPF.Business.ServiceLocator.AddonsChanged -= OnProductsChanged;
            POSSystem.WPF.Business.ServiceLocator.CategoriesChanged -= OnCategoriesChanged;
            POSSystem.WPF.Business.ServiceLocator.DiscountsChanged -= OnDiscountsChanged;
            POSSystem.WPF.Business.ServiceLocator.UserPermissionsChanged -= OnUserPermissionsChanged;
            SettingsService.OnSettingsChanged -= OnSystemSettingsChanged;

            if (_returnOrderService != null)
            {
                _returnOrderService.ReturnProcessed -= OnReturnProcessed;
            }

            if (_ordersViewModel != null)
            {
                _ordersViewModel.Dispose();
            }

            base.Dispose();
        }

        public async Task InitializeAsync()
        {
            try 
            {
                await LoadProductsAsync();
                await LoadCategoriesAsync();
                await SequnceNumber();
                await LoadDeliveryDrivers();
                await LoadDeliveryCustomersAsync();
                _activeDiscounts = await _discountsServices.GetAllDiscounts();
                Application.Current?.Dispatcher?.Invoke(RefreshAvailableDiscountsList);
                _ordersViewModel.RefreshOrders();
                await CheckActiveShiftAsync();
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (!HasActiveShift)
                    {
                        MessageBox.Show("يجب بدء مناوبة قبل استخدام النظام. يرجى فتح إدارة المناوبة وبدء مناوبة جديدة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                });

                FilterProducts();
                var loadedSettings = SettingsService.LoadSettings();
                cashierProberty.PrintCustomerThisOrder = loadedSettings?.PrintCustomerReceipt ?? true;
                cashierProberty.PrintKitchenThisOrder = loadedSettings?.PrintKitchenReceipt ?? true;

                await LoadHeldOrdersAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during initialization: {ex.Message}");
            }
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsProberty.SeparateProductSizes))
            {
                FilterProducts();
            }
        }

        public void SetOrderType(string orderType)
        {
            cashierProberty.OrderType = orderType;
            if (orderType == "delivery" && string.IsNullOrWhiteSpace(cashierProberty.SelectedDeliveryDriver) && cashierProberty.DeliveryDrivers.Count > 0)
            {
                cashierProberty.SelectedDeliveryDriver = cashierProberty.DeliveryDrivers[0];
            }
        }
        public void SetPaymentMethod(string paymentMethod) => cashierProberty.PaymentMethod = paymentMethod;
        public void TogglePrintCustomer() => cashierProberty.PrintCustomerThisOrder = !cashierProberty.PrintCustomerThisOrder;
        public void TogglePrintKitchen() => cashierProberty.PrintKitchenThisOrder = !cashierProberty.PrintKitchenThisOrder;

        public void PerformSearch() => FilterProducts();

        public void OnCategorySelected(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName) || categoryName == "جميع اصناف" || categoryName == "الكل") { SelectAllCategories(); return; }
            foreach (var key in _selectedCategories.Keys.ToList()) _selectedCategories[key] = false;
            if (!_selectedCategories.ContainsKey(categoryName)) _selectedCategories[categoryName] = false;
            _selectedCategories[categoryName] = true;
            cashierProberty.AllCategoriesSelected = false;
            FilterProducts();
        }

        public void FilterProducts()
        {
            IEnumerable<Product> filtered = _allProducts.Where(p => p.IsActive && !p.IsAnimalComponent);
            if (!string.IsNullOrWhiteSpace(_searchText)) filtered = filtered.Where(p => p.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) || (p.Description != null && p.Description.Contains(_searchText, StringComparison.OrdinalIgnoreCase)) || (p.CategoryName != null && p.CategoryName.Contains(_searchText, StringComparison.OrdinalIgnoreCase)));
            if (!cashierProberty.AllCategoriesSelected) { var selectedCats = _selectedCategories.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList(); if (selectedCats.Any()) filtered = filtered.Where(p => selectedCats.Contains(p.CategoryName)); }
            
            // OPTIMIZATION: Pre-calculate quantities to avoid O(N*M) lookups inside the loop, preventing UI lag
            var orderItemQtyByProduct = cashierProberty.OrderItems
                .GroupBy(i => i.ProductId ?? 0)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

            var orderItemQtyByProductAndSize = cashierProberty.OrderItems
                .Where(i => !string.IsNullOrEmpty(i.Size))
                .GroupBy(i => new { ProductId = i.ProductId ?? 0, Size = NormalizeSizeName(i.Size) })
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

            var displayList = new List<Product>();
            foreach (var p in filtered)
            {
                if (this.settingsProberty != null && this.settingsProberty.SeparateProductSizes && p.ProductSizes != null && p.ProductSizes.Any())
                {
                    foreach (var size in p.ProductSizes)
                    {
                        var key = new { ProductId = p.Id, Size = size.SizeName };
                        int qtyInOrder = orderItemQtyByProductAndSize.TryGetValue(key, out var q) ? q : 0;
                        
                        displayList.Add(new Product { Id = p.Id, Name = $"{p.Name} - {size.SizeName}", Price = size.Price, ImagePath = p.ImagePath, CategoryName = p.CategoryName, CategoryId = p.CategoryId, Category = p.Category, KitchenId = p.KitchenId, Kitchen = p.Kitchen, IsAddon = p.IsAddon, VirtualSizeName = size.SizeName, TotalValue = p.TotalValue, StockQuantity = p.StockQuantity, TypeWarehoseFromProduct = p.TypeWarehoseFromProduct, Recipe = p.Recipe, ParentProduct = p, ProductSizes = new List<ProductSize>(), Quantity = qtyInOrder, IsInOrder = qtyInOrder > 0 });
                    }
                }
                else 
                { 
                    int qty = p.IsAddon
                        ? cashierProberty.OrderItems.Where(i => i.AddonId == p.Id).Sum(i => i.Quantity)
                        : (orderItemQtyByProduct.TryGetValue(p.Id, out var q) ? q : 0);
                    p.Quantity = qty; 
                    p.IsInOrder = qty > 0; 
                    displayList.Add(p); 
                }
            }
            Products = new ObservableCollection<Product>(displayList);
        }

        public void SelectAllCategories() { cashierProberty.AllCategoriesSelected = true; foreach (var key in _selectedCategories.Keys.ToList()) _selectedCategories[key] = false; FilterProducts(); }
        public void ToggleCategorySelection(string categoryName, bool isSelected) { if (_selectedCategories.ContainsKey(categoryName)) { _selectedCategories[categoryName] = isSelected; cashierProberty.AllCategoriesSelected = !_selectedCategories.Any(kvp => kvp.Value); FilterProducts(); } }
        private void UpdateCategoryProductCounts() { if (_categories == null || _categories.Count == 0) return; foreach (var category in _categories) category.ProductCount = _allProducts.Count(p => p.CategoryName == category.Name); }

        public async Task LoadDeliveryDrivers()
        {
            try 
            { 
                var drivers = await _employeeService.GetUserByjobnamenameAsync("توصيل"); 
                cashierProberty._deliveryDrivers.Clear(); 
                foreach (var d in drivers) cashierProberty._deliveryDrivers.Add(d.Name); 
                if (cashierProberty._deliveryDrivers.Count > 0 && string.IsNullOrWhiteSpace(cashierProberty.SelectedDeliveryDriver))
                {
                    cashierProberty.SelectedDeliveryDriver = cashierProberty._deliveryDrivers[0];
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error loading drivers: {ex.Message}"); }
        }

        public async Task LoadCategoriesAsync()
        {
            try
            {
                _categories.Clear(); var categories = await _categoryService.GetActiveCategoriesAsync();
                foreach (var c in categories) { if (!c.IsSubCategory) _categories.Add(c); if (!_selectedCategories.ContainsKey(c.Name)) _selectedCategories[c.Name] = false; }
                // Note 4: Removed "إضافات" category so all items are managed strictly via Products
                UpdateCategoryProductCounts(); OnPropertyChanged(nameof(Categories));
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}"); }
        }

        public async Task LoadProductsAsync()
        {
            try 
            {
                _unitOfWork.ClearEntityTracking();
                _allProducts.Clear();
                var products = await _productService.GetActiveProductsAsync();
                _allProducts.AddRange(products);
                // Note 4: Addons section removed; relying exclusively on Products
                FilterProducts();
                UpdateCategoryProductCounts();
            }
            catch(Exception ex) { System.Diagnostics.Debug.WriteLine($"Error loading products: {ex.Message}"); }
        }

        public async void RefreshCategoriesAsync() => await LoadCategoriesAsync();
        public void NavigateToReturnOrders() => ShowReturnOrdersRequested?.Invoke(this, EventArgs.Empty);

        public async Task LoadHeldOrdersAsync()
        {
            try
            {
                var savedOrders = await _heldOrderService.GetAllHeldOrdersAsync();
                var allAddons = (await _addonService.GetActiveAddonsAsync())?.ToList();
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _heldOrders.Clear();
                    foreach (var order in savedOrders)
                    {
                        order.DeserializeItems(_allProducts, allAddons);
                        _heldOrders.Add(order);
                    }
                });
            }
            catch (Exception ex)
            {
                POSSystem.WPF.Services.Logging.AppLogger.LogError("Error loading persistent held orders into Cashier", ex);
            }
        }

        public async Task LoadDeliveryCustomersAsync()
        {
            try
            {
                var context = _unitOfWork.GetContext();
                var customers = await context.Customers.AsNoTracking().Where(c => c.IsActive).ToListAsync();
                var deliveries = await context.Deliveries.AsNoTracking()
                    .Where(d => d.DeliveryCustomerName != null && d.DeliveryCustomerName != "")
                    .GroupBy(d => new { d.DeliveryCustomerName, d.DeliveryPhone, d.DeliveryAddress })
                    .Select(g => new Delivery
                    {
                        DeliveryCustomerName = g.Key.DeliveryCustomerName,
                        DeliveryPhone = g.Key.DeliveryPhone,
                        DeliveryAddress = g.Key.DeliveryAddress
                    })
                    .ToListAsync();

                bool anyAdded = false;
                foreach (var d in deliveries)
                {
                    var existing = customers.FirstOrDefault(c => 
                        (!string.IsNullOrWhiteSpace(d.DeliveryPhone) && c.PhoneNumber == d.DeliveryPhone) ||
                        string.Equals(c.Name, d.DeliveryCustomerName, StringComparison.OrdinalIgnoreCase));

                    if (existing == null)
                    {
                        var newCust = new Customer
                        {
                            Name = d.DeliveryCustomerName!.Trim(),
                            PhoneNumber = d.DeliveryPhone ?? string.Empty,
                            Address = d.DeliveryAddress ?? string.Empty,
                            IsActive = true,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };
                        await _unitOfWork.Customers.AddAsync(newCust);
                        customers.Add(newCust);
                        anyAdded = true;
                    }
                    else if (string.IsNullOrWhiteSpace(existing.Address) && !string.IsNullOrWhiteSpace(d.DeliveryAddress))
                    {
                        existing.Address = d.DeliveryAddress;
                        _unitOfWork.Customers.Update(existing);
                        anyAdded = true;
                    }
                }

                if (anyAdded)
                {
                    await _unitOfWork.SaveChangesAsync();
                }

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    cashierProberty.DeliveryCustomers.Clear();
                    foreach (var c in customers.OrderBy(c => c.Name))
                    {
                        cashierProberty.DeliveryCustomers.Add(c);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading delivery customers: {ex.Message}");
            }
        }
    }
}
