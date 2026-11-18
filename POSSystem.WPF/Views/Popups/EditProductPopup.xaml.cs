using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;


using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Controls.Primitives;
using static POSSystem.WPF.Models.Recipe;
using POSSystem.WPF.ViewModels.Products;
using POSSystem.WPF.Models;
using System.Windows;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Business;
using System;
using Microsoft.Win32;

namespace POSSystem.WPF.Views.Popups
{
    public partial class EditProductPopup : Window
    {
        //ViewModel
        private ProductsViewModel _viewModel = null!;
        private ProductsViewModel _parentViewModel = null!;
        //Service
        private IWarehouseService _warehouseService;

        private Product _originalProduct = null!;
        private bool _isDataLoading = false;


        public EditProductPopup(Product product, ProductsViewModel parentViewModel)
        {
            try
            {
                InitializeComponent();
                _parentViewModel = parentViewModel;
                _originalProduct = product;


                _viewModel = _parentViewModel;
                // ServiceLocator does not expose WarehouseService directly, create one from the shared UnitOfWork
                _warehouseService = new Business.Services.WarehouseService(POSSystem.WPF.App.ServiceProvider.GetRequiredService<IUnitOfWork>());

                DataContext = _viewModel;
                Loaded += async (s, e) => {
                    try {
                        await _viewModel.productViewModel.LoadCategoriesAsync(forceRefresh: true);
                        await _viewModel.productViewModel.LoadWarehousesAsync(forceRefresh: true);
                        LoidDataProduct(product);
                        _ = LoadAvailableUnit();
                    } catch (Exception ex) {
                        System.Diagnostics.Debug.WriteLine($"Error initializing EditProductPopup: {ex.Message}");
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تهيئة نافذة تعديل المنتج: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }





        private void LoidDataProduct(Product product)
        {
            try
            {
                _viewModel.productViewModel.IsLoading = true;
                _viewModel.productViewModel.Name = product.Name;
                _viewModel.productViewModel.Description = product.Description;
                _viewModel.productViewModel.Price = product.Price;
                _viewModel.productViewModel.StockQuantity = product.StockQuantity.ToString(CultureInfo.InvariantCulture);
                _viewModel.productViewModel.ImagePath = product.ImagePath;
            UpdateImagePreview(product.ImagePath);

                // correct assignment for category
                if (product.CategoryId > 0)
                {
                    var category = _viewModel.productViewModel.Categories.FirstOrDefault(c => c.Id == product.CategoryId);
                    if (category != null)
                    {
                        _viewModel.productViewModel.SelectedCategory = category;
                        CategoryComboBox.SelectedItem = category;
                    }
                }

                // Load Size Prices
                if (product.ProductSizes != null && product.ProductSizes.Any())
                {
                    foreach (var s in product.ProductSizes)
                    {
                        string sizeName = s.SizeName?.Trim();
                        if (sizeName == "حبة") _viewModel.productViewModel.PriceHaba = s.Price.ToString(CultureInfo.InvariantCulture);
                        else if (sizeName == "نص حبة") _viewModel.productViewModel.PriceNusHaba = s.Price.ToString(CultureInfo.InvariantCulture);
                        else if (sizeName == "ربع حبة") _viewModel.productViewModel.PriceRobHaba = s.Price.ToString(CultureInfo.InvariantCulture);
                        
                        else if (sizeName == "نفر" || sizeName == "ذبيحة") _viewModel.productViewModel.PriceNafar = s.Price.ToString(CultureInfo.InvariantCulture);
                        else if (sizeName == "نص نفر" || sizeName == "نص ذبيحة") _viewModel.productViewModel.PriceNusNafar = s.Price.ToString(CultureInfo.InvariantCulture);
                        else if (sizeName == "نفر ونص" || sizeName == "ذبيحة ونص" || sizeName == "ربع نفر" || sizeName == "ربع ذبيحة") _viewModel.productViewModel.PriceRobNafar = s.Price.ToString(CultureInfo.InvariantCulture);
                        
                        else if (sizeName == "كبير") _viewModel.productViewModel.PriceKabeer = s.Price.ToString(CultureInfo.InvariantCulture);
                        else if (sizeName == "وسط") _viewModel.productViewModel.PriceWasat = s.Price.ToString(CultureInfo.InvariantCulture);
                        else if (sizeName == "صغير") _viewModel.productViewModel.PriceSagheer = s.Price.ToString(CultureInfo.InvariantCulture);
                    }
                }

                // Force update visibility based on the product's actual category if SelectedCategory is not set yet
                if (_viewModel.productViewModel.SelectedCategory == null && product.Category != null)
                {
                    _viewModel.productViewModel.SelectedCategory = product.Category;
                }
                
                _viewModel.productViewModel.IsActive = product.IsActive;
                _viewModel.productViewModel.PersonNumberFood = product.PersonNumberFood.ToString(CultureInfo.InvariantCulture);
                _viewModel.productViewModel.PortionUnits = product.portionUnit;
                _viewModel.productViewModel.TypeWarehoseFromProduct = product.TypeWarehoseFromProduct;

                // Populate warehouse dropdown and select correct warehouse and product
                _isDataLoading = true;
                try
                {
                    var warehouseFromProductAll = _warehouseService.GetWarehouseFromProductAll();
                    
                    if (!string.IsNullOrWhiteSpace(product.TypeWarehoseFromProduct))
                    {
                        // Find the specific warehouse product based on the saved name or ID (handling different save versions)
                        var selectedProductFromWarehouse = warehouseFromProductAll.FirstOrDefault(x => 
                            x.Name == product.TypeWarehoseFromProduct || 
                            x.Id.ToString() == product.TypeWarehoseFromProduct);
                        
                        if (selectedProductFromWarehouse != null)
                        {
                            _viewModel.productViewModel.SelectedWarehouseFromProduct = selectedProductFromWarehouse;
                            
                            // Find the warehouse that owns this product
                            if (selectedProductFromWarehouse.WarehouseId.HasValue)
                            {
                            var warehouse = _viewModel.productViewModel.Warehouses.FirstOrDefault(w => w.Id == selectedProductFromWarehouse.WarehouseId.Value);
                            if (warehouse != null)
                            {
                                _viewModel.productViewModel.SelectedWarehouse = warehouse;
                                WarehouseComboBox.SelectedItem = warehouse;
                                
                                // Filter the products dropdown based on this warehouse
                                var filteredProducts = warehouseFromProductAll.Where(x => x.WarehouseId == warehouse.Id).ToList();
                                WarehoseForProductDrobDaown.ItemsSource = filteredProducts;
                                WarehoseForProductDrobDaown.SelectedItem = selectedProductFromWarehouse;
                            }
                            }
                        }
                        else
                        {
                            // Fallback if product name match fails but we have category? 
                            // For now just show all
                            WarehoseForProductDrobDaown.ItemsSource = warehouseFromProductAll;
                        }

                    }
                    else
                    {
                        // fallback: populate list with all warehouses
                        WarehoseForProductDrobDaown.ItemsSource = warehouseFromProductAll;
                    }
                }
                finally
                {
                    _isDataLoading = false;
                }
            }
            catch (Exception ex)
            {
                // Don't block popup; log for diagnosis
                Debug.WriteLine($"Error populating warehouse list in EditProductPopup: {ex.Message}");
            }
            finally
            {
                _viewModel.productViewModel.IsLoading = false;
            }
        }

        public async Task LoadAvailableUnit()
        {
            try
            {
                InvontryTypeComb.ItemsSource = PortionUnitHelper.DisplayNames;
                InvontryTypeComb.DisplayMemberPath = "Value"; // النص
                InvontryTypeComb.SelectedValuePath = "Key";   // القيمة (enum)
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الفئات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "اختر صورة المنتج",
                Filter = "ملفات الصور|*.jpg;*.jpeg;*.png;*.bmp;*.gif|جميع الملفات|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _viewModel.productViewModel.ImagePath = openFileDialog.FileName;
                UpdateImagePreview(openFileDialog.FileName);
                MessageBox.Show($"تم اختيار الصورة: {System.IO.Path.GetFileName(openFileDialog.FileName)}",
                    "تم اختيار الصورة", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RemoveImageButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.productViewModel.ImagePath = string.Empty;
            UpdateImagePreview(string.Empty);
        }

        private void UpdateImagePreview(string? path)
        {
            if (ProductImagePreview == null || NoImagePlaceholder == null) return;

            if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
            {
                try
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    ProductImagePreview.Source = bitmap;
                    ProductImagePreview.Visibility = Visibility.Visible;
                    NoImagePlaceholder.Visibility = Visibility.Collapsed;
                    return;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading image preview: {ex.Message}");
                }
            }

            ProductImagePreview.Source = null;
            ProductImagePreview.Visibility = Visibility.Collapsed;
            NoImagePlaceholder.Visibility = Visibility.Visible;
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(_viewModel.productViewModel.Name))
            {
                MessageBox.Show("يرجى إدخال اسم المنتج", "خطأ في التحقق",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            // Parse all prices
            decimal pHaba = ParsePrice(_viewModel.productViewModel.PriceHaba);
            decimal pNusHaba = ParsePrice(_viewModel.productViewModel.PriceNusHaba);
            decimal pRobHaba = ParsePrice(_viewModel.productViewModel.PriceRobHaba);
            
            decimal pNafar = ParsePrice(_viewModel.productViewModel.PriceNafar);
            decimal pNusNafar = ParsePrice(_viewModel.productViewModel.PriceNusNafar);
            decimal pRobNafar = ParsePrice(_viewModel.productViewModel.PriceRobNafar);
            
            decimal pKabeer = ParsePrice(_viewModel.productViewModel.PriceKabeer);
            decimal pWasat = ParsePrice(_viewModel.productViewModel.PriceWasat);
            decimal pSagheer = ParsePrice(_viewModel.productViewModel.PriceSagheer);

            bool hasPrice = false;
            if (_viewModel.productViewModel.IsBasePriceVisible)
            {
                hasPrice = _viewModel.productViewModel.Price > 0;
            }
            else if (_viewModel.productViewModel.IsHabaVisible)
            {
                hasPrice = pHaba > 0 || pNusHaba > 0 || pRobHaba > 0;
            }
            else if (_viewModel.productViewModel.IsNafarVisible)
            {
                hasPrice = pNafar > 0 || pNusNafar > 0 || pRobNafar > 0;
            }
            else if (_viewModel.productViewModel.IsKabeerVisible)
            {
                hasPrice = pKabeer > 0 || pWasat > 0 || pSagheer > 0;
            }

            if (!hasPrice)
            {
                MessageBox.Show("يرجى إدخال سعر صحيح للمنتج", "خطأ في التحقق",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string normalizedStock = _viewModel.productViewModel.StockQuantity.Replace(',', '.');
            if (!decimal.TryParse(normalizedStock, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedStock) || parsedStock < 0)
            {
                MessageBox.Show("يرجى إدخال كمية مخزون صحيحة (مثال: 0.5)", "خطأ في التحقق",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                StockTextBox.Focus();
                return;
            }

            string normalizedNafar = _viewModel.productViewModel.PersonNumberFood.Replace(',', '.');
            if (!decimal.TryParse(normalizedNafar, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedNafar))
            {
                MessageBox.Show("يرجى إدخال قيمة صحيحة للنفر (مثال: 1.5)", "خطأ في التحقق",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PersonNumberFoodTextBox.Focus();
                return;
            }

            if (InvontryTypeComb.SelectedValue == null)
            {
                MessageBox.Show("يرجى اختيار وحدة قياس للمنتج", "خطأ في التحقق",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                InvontryTypeComb.Focus();
                return;
            }

            if (_viewModel.productViewModel.SelectedWarehouseFromProduct == null)
            {
                 MessageBox.Show("يرجى اختيار منتج من المخزن", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Existing Warehouse Deduction logic
                var warehouseFromProductAll = _warehouseService.GetWarehouseFromProductAll();
                var selectedWarehouseProduct = _viewModel.productViewModel.SelectedWarehouseFromProduct;
                
                var TypeWarehoseFromProductQunttity = warehouseFromProductAll.FirstOrDefault(x => x.Name == selectedWarehouseProduct.Name);
                
                if (TypeWarehoseFromProductQunttity == null)
                {
                    MessageBox.Show("المنتج المحدد من المخزن غير موجود.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Logic to handle stock update using fresh DbContext to avoid EF tracking cache issues
                using (var context = new Data.POSDbContext())
                {
                    if (_originalProduct.TypeWarehoseFromProduct == TypeWarehoseFromProductQunttity.Name || _originalProduct.TypeWarehoseFromProduct == TypeWarehoseFromProductQunttity.Id.ToString())
                    {
                        // Same warehouse product: calculate difference
                        decimal difference = parsedStock - _originalProduct.StockQuantity;
                        if (difference != 0)
                        {
                            var freshWp = await context.warehouseFromProducts.FindAsync(TypeWarehoseFromProductQunttity.Id);
                            if (freshWp != null)
                            {
                                freshWp.StockQuantity -= difference;
                                await context.SaveChangesAsync();
                            }
                        }
                    }
                    else
                    {
                        // Changed warehouse product: refund to old, deduct from new
                        var oldWarehouseProduct = await context.warehouseFromProducts.FirstOrDefaultAsync(x => 
                            x.Name == _originalProduct.TypeWarehoseFromProduct || 
                            x.Id.ToString() == _originalProduct.TypeWarehoseFromProduct);
                        if (oldWarehouseProduct != null)
                        {
                            oldWarehouseProduct.StockQuantity += _originalProduct.StockQuantity;
                        }

                        var newWarehouseProduct = await context.warehouseFromProducts.FindAsync(TypeWarehoseFromProductQunttity.Id);
                        if (newWarehouseProduct != null)
                        {
                            newWarehouseProduct.StockQuantity -= parsedStock;
                        }
                        
                        await context.SaveChangesAsync();
                    }
                }

                _originalProduct.IsAnimalComponent = false;
                _originalProduct.AnimalComponentType = null;
                _originalProduct.AnimalBatch = null; // Prevent EF conflict
                _originalProduct.TypeWarehoseFromProduct = TypeWarehoseFromProductQunttity.Name;

                // Common Field Updates
                decimal basePriceToSave = _viewModel.productViewModel.Price;
                if (!_viewModel.productViewModel.IsBasePriceVisible)
                {
                    if (_viewModel.productViewModel.IsHabaVisible) basePriceToSave = pHaba > 0 ? pHaba : (pNusHaba > 0 ? pNusHaba : pRobHaba);
                    else if (_viewModel.productViewModel.IsNafarVisible) basePriceToSave = pNafar > 0 ? pNafar : (pNusNafar > 0 ? pNusNafar : pRobNafar);
                    else if (_viewModel.productViewModel.IsKabeerVisible) basePriceToSave = pKabeer > 0 ? pKabeer : (pWasat > 0 ? pWasat : pSagheer);
                }

                _originalProduct.Name = _viewModel.productViewModel.Name;
                _originalProduct.Description = _viewModel.productViewModel.Description;
                _originalProduct.Price = basePriceToSave;
                _originalProduct.BasePrice = basePriceToSave;
                _originalProduct.ImagePath = _viewModel.productViewModel.ImagePath;
                _originalProduct.CategoryName = _viewModel.productViewModel.SelectedCategory?.Name;
                _originalProduct.CategoryId = _viewModel.productViewModel.SelectedCategory?.Id ?? 0;
                // _originalProduct.Category is purposefully NOT updated here to prevent cross-DbContext tracking errors. It is re-assigned safely in the ViewModel.
                _originalProduct.IsActive = _viewModel.productViewModel.IsActive;
                _originalProduct.UpdatedAt = DateTime.Now;
                _originalProduct.PersonNumberFood = parsedNafar;
                _originalProduct.portionUnit = _viewModel.productViewModel.PortionUnits;
                _originalProduct.StockQuantity = parsedStock;
                _originalProduct.TotalValue = parsedNafar > 0 ? parsedNafar * parsedStock : parsedStock;

                // Update Sizes Safely Without Clearing Collection
                if (_originalProduct.ProductSizes == null) _originalProduct.ProductSizes = new List<ProductSize>();
                
                var newSizes = new Dictionary<string, decimal>();
                if (_viewModel.productViewModel.IsHabaVisible)
                {
                    if (pHaba > 0) newSizes["حبة"] = pHaba;
                    if (pNusHaba > 0) newSizes["نص حبة"] = pNusHaba;
                    if (pRobHaba > 0) newSizes["ربع حبة"] = pRobHaba;
                }
                else if (_viewModel.productViewModel.IsNafarVisible)
                {
                    if (pNafar > 0) newSizes["نفر"] = pNafar;
                    if (pNusNafar > 0) newSizes["نص نفر"] = pNusNafar;
                    if (pRobNafar > 0) newSizes["نفر ونص"] = pRobNafar;
                }
                else if (_viewModel.productViewModel.IsKabeerVisible)
                {
                    if (pKabeer > 0) newSizes["كبير"] = pKabeer;
                    if (pWasat > 0) newSizes["وسط"] = pWasat;
                    if (pSagheer > 0) newSizes["صغير"] = pSagheer;
                }

                // Remove sizes that are no longer present
                var sizesToRemove = _originalProduct.ProductSizes.Where(s => !newSizes.ContainsKey(s.SizeName)).ToList();
                foreach(var s in sizesToRemove)
                {
                    _originalProduct.ProductSizes.Remove(s);
                }

                // Update or Add sizes
                foreach (var kvp in newSizes)
                {
                    var existingSize = _originalProduct.ProductSizes.FirstOrDefault(s => s.SizeName == kvp.Key);
                    if (existingSize != null)
                    {
                        existingSize.Price = kvp.Value;
                    }
                    else
                    {
                        _originalProduct.ProductSizes.Add(new ProductSize { SizeName = kvp.Key, Price = kvp.Value });
                    }
                }

                // Update in parent view model and DB
                await _parentViewModel.UpdateProduct(_originalProduct);

                MessageBox.Show("تم تحديث المنتج بنجاح!", "تم التحديث", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحديث المنتج: {ex.Message}\n\n{ex.InnerException?.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void WarehoseForProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isDataLoading) return;
            if (sender is ComboBox comboBox && comboBox.SelectedItem is WarehouseFromProduct warehouseFromProduct)

            {

                _viewModel.productViewModel.SelectedWarehouseFromProduct = warehouseFromProduct;

            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("هل أنت متأكد من إلغاء تعديل المنتج؟",
                "تأكيد الإلغاء", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }



        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isDataLoading) return;
            if (sender is ComboBox comboBox && comboBox.SelectedItem is Category category)
            {
                _viewModel.productViewModel.SelectedCategory = category;
            }
        }

        private void WarehouseComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isDataLoading) return;
            if (sender is ComboBox comboBox && comboBox.SelectedItem is Warehouse warehouse)
            {
                _viewModel.productViewModel.SelectedWarehouse = warehouse;
                
                // Filter products based on selected warehouse
                 try
                {
                    var warehouseFromProductAll = _warehouseService.GetWarehouseFromProductAll();
                    var filtered = warehouseFromProductAll.Where(x => x.WarehouseId == warehouse.Id).ToList();
                     WarehoseForProductDrobDaown.ItemsSource = filtered;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error filtering products: {ex.Message}");
                }
            }
        }
        private decimal ParsePrice(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            string normalized = value.Replace(',', '.');
            if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }
            return 0;
        }
    }
}
