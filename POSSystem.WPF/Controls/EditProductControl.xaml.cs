using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;
using POSSystem.WPF.ViewModels.Products;
using static POSSystem.WPF.Models.Recipe;

namespace POSSystem.WPF.Controls
{
    public partial class EditProductControl : UserControl
    {
        //ViewModel
        private ProductsViewModel _viewModel = null!;
        private ProductsViewModel _parentViewModel = null!;
        //Service
        private IWarehouseService _warehouseService;

        private Product _originalProduct = null!;
        private bool _isDataLoading = false;

        // Event to signal close/cancel to the parent
        public event EventHandler CloseRequested;


        public EditProductControl()
        {
            InitializeComponent();
        }

        public async Task InitializeAsync(Product product, ProductsViewModel parentViewModel)
        {
            try
            {
                _isDataLoading = true;
                _parentViewModel = parentViewModel;
                _originalProduct = product;

                _viewModel = _parentViewModel;
                // ServiceLocator does not expose WarehouseService directly, create one from the shared UnitOfWork
                // Assuming ServiceLocator.UnitOfWork is available as in logic
                if (ServiceLocator.UnitOfWork != null)
                {
                     _warehouseService = new Business.Services.WarehouseService(ServiceLocator.UnitOfWork);
                }
                else
                {
                     // Fallback mechanism if ServiceLocator isn't ready or compatible way
                     // This depends on how the original code was handling it.
                     // The original code used: new Business.Services.WarehouseService(ServiceLocator.UnitOfWork);
                     // We will keep it but assuming it works.
                     _warehouseService = new Business.Services.WarehouseService(ServiceLocator.UnitOfWork); 
                }

                DataContext = _viewModel;
                await LoidDataProductAsync(product);
                LoadAvailableUnit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تهيئة نافذة تعديل المنتج: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                CloseControl();
            }
            finally
            {
                _isDataLoading = false;
            }
        }

        private async Task LoidDataProductAsync(Product product)
        {
            _viewModel.productViewModel.Name = product.Name;
            _viewModel.productViewModel.Description = product.Description;
            _viewModel.productViewModel.Price = product.Price;
            _viewModel.productViewModel.LastCookedQty = product.LastCookedQty.ToString(CultureInfo.InvariantCulture);
            _viewModel.productViewModel.ImagePath = product.ImagePath;
            UpdateImagePreview(product.ImagePath);

            // correct assignment for category
            await _viewModel.productViewModel.LoadCategoriesAsync(forceRefresh: true);
            await _viewModel.productViewModel.LoadKitchensAsync(forceRefresh: true);
            await _viewModel.productViewModel.LoadWarehousesAsync(forceRefresh: true);
            int? targetKitchenId = product.KitchenId ?? product.Category?.KitchenId;
            if (targetKitchenId.HasValue && targetKitchenId.Value > 0)
            {
                var kMatch = _viewModel.productViewModel.Kitchens.FirstOrDefault(k => k.Id == targetKitchenId.Value);
                _viewModel.productViewModel.SelectedKitchen = kMatch;
                if (KitchenComboBox != null) KitchenComboBox.SelectedItem = kMatch;
            }
            else
            {
                _viewModel.productViewModel.SelectedKitchen = null;
                if (KitchenComboBox != null) KitchenComboBox.SelectedItem = null;
            }
            if (product.CategoryId > 0)
            {
                var category = _viewModel.productViewModel.Categories.FirstOrDefault(c => c.Id == product.CategoryId);
                if (category != null)
                {
                    _viewModel.productViewModel.SelectedCategory = category;
                    CategoryComboBox.SelectedItem = category;
                }
            }
            
            // Reset all size prices first
            _viewModel.productViewModel.PriceHaba = "0";
            _viewModel.productViewModel.PriceNusHaba = "0";
            _viewModel.productViewModel.PriceRobHaba = "0";
            _viewModel.productViewModel.PriceNafar = "0";
            _viewModel.productViewModel.PriceNusNafar = "0";
            _viewModel.productViewModel.PriceRobNafar = "0";
            _viewModel.productViewModel.PriceKabeer = "0";
            _viewModel.productViewModel.PriceWasat = "0";
            _viewModel.productViewModel.PriceSagheer = "0";

            // Always fetch latest ProductSizes from DB so edits are never stale or empty
            var sizesList = product.ProductSizes?.ToList() ?? new System.Collections.Generic.List<ProductSize>();
            try
            {
                var dbSizes = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                    Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(
                        ServiceLocator.UnitOfWork.GetContext().ProductSizes.Where(ps => ps.ProductId == product.Id)));
                if (dbSizes.Count > 0)
                {
                    sizesList = dbSizes;
                    product.ProductSizes = dbSizes;
                }
            }
            catch { }

            if (sizesList.Any())
            {
                foreach (var s in sizesList)
                {
                    string sizeName = s.SizeName?.Trim() ?? string.Empty;
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
            else if (product.Price > 0)
            {
                if (_viewModel.productViewModel.IsNafarVisible)
                    _viewModel.productViewModel.PriceNafar = product.Price.ToString(CultureInfo.InvariantCulture);
                else if (_viewModel.productViewModel.IsHabaVisible)
                    _viewModel.productViewModel.PriceHaba = product.Price.ToString(CultureInfo.InvariantCulture);
                else if (_viewModel.productViewModel.IsKabeerVisible)
                    _viewModel.productViewModel.PriceKabeer = product.Price.ToString(CultureInfo.InvariantCulture);
            }

            // Force update visibility based on the product's actual category if SelectedCategory is not set yet
            if (_viewModel.productViewModel.SelectedCategory == null && product.Category != null)
            {
                _viewModel.productViewModel.SelectedCategory = product.Category;
            }
            
            _viewModel.productViewModel.IsActive = product.IsActive;
            _viewModel.productViewModel.YieldRate = product.YieldRate.ToString(CultureInfo.InvariantCulture);
            _viewModel.productViewModel.PortionUnits = product.portionUnit != 0 ? product.portionUnit : PortionUnit.Piece;
            _viewModel.productViewModel.TypeWarehoseFromProduct = product.TypeWarehoseFromProduct;
            if (UntrackedStockCheckBox != null)
            {
                UntrackedStockCheckBox.IsChecked = product.IsUntrackedStock;
                if (UnitSelectionPanel != null)
                    UnitSelectionPanel.Visibility = product.IsUntrackedStock ? Visibility.Collapsed : Visibility.Visible;
            }

            // Populate warehouse dropdown and select correct warehouse and product
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
                               // Warehouse dropdowns removed from UI
                           }
                        }
 
                     // LOAD EXISTING RECIPES (MAPPINGS)
                     var allRecipes = await ServiceLocator.UnitOfWork.Recipes.GetRecipesAllProducts(); 
                     var productRecipes = allRecipes.Where(r => r.ProductId == product.Id).ToList();
                     _viewModel.productViewModel.WarehouseMappings.Clear();
                     foreach (var recipe in productRecipes)
                     {
                         var wp = warehouseFromProductAll.FirstOrDefault(x => x.Name.Trim().Equals(recipe.Name.Trim(), StringComparison.OrdinalIgnoreCase));
                         if (wp != null)
                         {
                             _viewModel.productViewModel.WarehouseMappings.Add(new WarehouseMappingItem
                             {
                                 WarehouseProductId = wp.Id,
                                 Name = wp.Name,
                                 Quantity = recipe.Quantity
                             });
                         }
                     }
                }
                else
                {
                     // Fallback if product name match fails
                }
            }
            }
            catch (Exception ex)
            {
                // Don't block popup; log for diagnosis
                Debug.WriteLine($"Error populating warehouse list in EditProductPopup: {ex.Message}");
            }

        }

        public void LoadAvailableUnit()
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
                    Debug.WriteLine($"Error loading image preview: {ex.Message}");
                }
            }

            ProductImagePreview.Source = null;
            ProductImagePreview.Visibility = Visibility.Collapsed;
            NoImagePlaceholder.Visibility = Visibility.Visible;
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isDataLoading) return;
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

            // Stock validation removed because stock is updated via InjectStockButton

            bool isUntracked = UntrackedStockCheckBox?.IsChecked == true;
            if (!isUntracked && string.IsNullOrWhiteSpace(_viewModel.productViewModel.PortionUnits.ToString()))
            {
                MessageBox.Show("يرجى اختيار وحدة قياس للمنتج", "خطأ في التحقق",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                InvontryTypeComb.Focus();
                return;
            }

            if (_viewModel.productViewModel.SelectedCategory == null)
            {
                 MessageBox.Show("يرجى اختيار فئة للمنتج", "خطأ في التحقق",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CategoryComboBox.Focus();
                return;
            }

            try
            {
                // The old inventory deduction logic is removed. Inventory is now managed via InjectStockButton_Click.
                
                // Base properties update
                _originalProduct.Name = _viewModel.productViewModel.Name;
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

                _originalProduct.ImagePath = _viewModel.productViewModel.ImagePath;
                _originalProduct.CategoryName = _viewModel.productViewModel.SelectedCategory?.Name;
                _originalProduct.CategoryId = _viewModel.productViewModel.SelectedCategory?.Id ?? 0;
                _originalProduct.KitchenId = (_viewModel.productViewModel.SelectedKitchen ?? KitchenComboBox?.SelectedItem as POSSystem.WPF.Models.Kitchen)?.Id;
                _originalProduct.IsAnimalComponent = false;
                _originalProduct.AnimalComponentType = null;
                
                if (InvontryTypeComb.SelectedValue != null)
                {
                    _originalProduct.portionUnit = (PortionUnit)InvontryTypeComb.SelectedValue;
                }
                else if (_originalProduct.portionUnit == 0)
                {
                    _originalProduct.portionUnit = PortionUnit.Piece;
                }

                if (isUntracked)
                {
                    _originalProduct.TypeWarehoseFromProduct = Product.UntrackedStockMarker;
                }
                else if (string.Equals(_originalProduct.TypeWarehoseFromProduct, Product.UntrackedStockMarker, StringComparison.OrdinalIgnoreCase))
                {
                    _originalProduct.TypeWarehoseFromProduct = string.Empty;
                }

                if (_originalProduct.ProductSizes == null) _originalProduct.ProductSizes = new List<ProductSize>();
                _originalProduct.ProductSizes.Clear();
                
                // Add Sizes
                if (_viewModel.productViewModel.IsHabaVisible || (pHaba > 0 || pNusHaba > 0 || pRobHaba > 0))
                {
                    if (pHaba > 0) _originalProduct.ProductSizes.Add(new ProductSize { Name = "حبة", SizeName = "حبة", Price = pHaba, BasePrice = pHaba, IsActive = true });
                    if (pNusHaba > 0) _originalProduct.ProductSizes.Add(new ProductSize { Name = "نص حبة", SizeName = "نص حبة", Price = pNusHaba, BasePrice = pNusHaba, IsActive = true });
                    if (pRobHaba > 0) _originalProduct.ProductSizes.Add(new ProductSize { Name = "ربع حبة", SizeName = "ربع حبة", Price = pRobHaba, BasePrice = pRobHaba, IsActive = true });
                }
                else if (_viewModel.productViewModel.IsNafarVisible || (pNafar > 0 || pNusNafar > 0 || pRobNafar > 0))
                {
                    string label1 = "نفر";
                    string label2 = "نص نفر";
                    string label3 = "نفر ونص";

                    if (_originalProduct.portionUnit == PortionUnit.Carcass || InvontryTypeComb.SelectedValue?.ToString() == "ذبيحة")
                    {
                        label1 = "ذبيحة";
                        label2 = "نص ذبيحة";
                        label3 = "ذبيحة ونص";
                    }

                    if (pNafar > 0) _originalProduct.ProductSizes.Add(new ProductSize { Name = label1, SizeName = label1, Price = pNafar, BasePrice = pNafar, IsActive = true });
                    if (pNusNafar > 0) _originalProduct.ProductSizes.Add(new ProductSize { Name = label2, SizeName = label2, Price = pNusNafar, BasePrice = pNusNafar, IsActive = true });
                    if (pRobNafar > 0) _originalProduct.ProductSizes.Add(new ProductSize { Name = label3, SizeName = label3, Price = pRobNafar, BasePrice = pRobNafar, IsActive = true });
                }
                else if (_viewModel.productViewModel.IsKabeerVisible || (pKabeer > 0 || pWasat > 0 || pSagheer > 0))
                {
                    if (pKabeer > 0) _originalProduct.ProductSizes.Add(new ProductSize { Name = "كبير", SizeName = "كبير", Price = pKabeer, BasePrice = pKabeer, IsActive = true });
                    if (pWasat > 0) _originalProduct.ProductSizes.Add(new ProductSize { Name = "وسط", SizeName = "وسط", Price = pWasat, BasePrice = pWasat, IsActive = true });
                    if (pSagheer > 0) _originalProduct.ProductSizes.Add(new ProductSize { Name = "صغير", SizeName = "صغير", Price = pSagheer, BasePrice = pSagheer, IsActive = true });
                }

                // Update parent (now handles DB update and refresh)
                await _parentViewModel.UpdateProduct(_originalProduct);

                MessageBox.Show("تم تحديث المنتج بنجاح!", "تم التحديث",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                CloseControl();
                    
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحديث المنتج: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }






        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("هل أنت متأكد من إلغاء تعديل المنتج؟",
                "تأكيد الإلغاء", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                CloseControl();
            }
        }



        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isDataLoading) return;
            if (sender is ComboBox comboBox && comboBox.SelectedItem is Category category)
            {
                _viewModel.productViewModel.SelectedCategory = category;
                string catName = category.Name?.Trim() ?? string.Empty;
                if (UntrackedStockCheckBox != null &&
                    (catName.Contains("ايدام") || catName.Contains("إيدام") || catName.Contains("سلط") || catName.Contains("مقبلات")))
                {
                    UntrackedStockCheckBox.IsChecked = true;
                }
            }
        }

        private void UntrackedStockCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (UnitSelectionPanel != null && UntrackedStockCheckBox != null)
            {
                UnitSelectionPanel.Visibility = UntrackedStockCheckBox.IsChecked == true ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private async void KitchenComboBox_DropDownOpened(object sender, EventArgs e)
        {
            try
            {
                var currentKitchenId = (KitchenComboBox.SelectedItem as POSSystem.WPF.Models.Kitchen)?.Id ?? _viewModel.productViewModel.SelectedKitchen?.Id;
                await _viewModel.productViewModel.LoadKitchensAsync(forceRefresh: true);
                if (currentKitchenId.HasValue && currentKitchenId.Value > 0)
                {
                    var match = _viewModel.productViewModel.Kitchens.FirstOrDefault(k => k.Id == currentKitchenId.Value);
                    if (match != null)
                    {
                        KitchenComboBox.SelectedItem = match;
                        _viewModel.productViewModel.SelectedKitchen = match;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reloading kitchens on drop down: {ex.Message}");
            }
        }

        private async void CategoryComboBox_DropDownOpened(object sender, EventArgs e)
        {
            try
            {
                var currentSelectedId = (CategoryComboBox.SelectedItem as Category)?.Id ?? _viewModel.productViewModel.SelectedCategory?.Id;
                await _viewModel.productViewModel.LoadCategoriesAsync(forceRefresh: true);
                if (currentSelectedId.HasValue && currentSelectedId.Value > 0)
                {
                    var match = _viewModel.productViewModel.Categories.FirstOrDefault(c => c.Id == currentSelectedId.Value);
                    if (match != null)
                    {
                        CategoryComboBox.SelectedItem = match;
                        _viewModel.productViewModel.SelectedCategory = match;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reloading categories on drop down: {ex.Message}");
            }
        }


        
        private static string NormalizeDigits(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value
                .Replace('٠', '0')
                .Replace('١', '1')
                .Replace('٢', '2')
                .Replace('٣', '3')
                .Replace('٤', '4')
                .Replace('٥', '5')
                .Replace('٦', '6')
                .Replace('٧', '7')
                .Replace('٨', '8')
                .Replace('٩', '9')
                .Replace('٫', '.')
                .Replace(',', '.');
        }

        private decimal ParsePrice(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            string normalized = NormalizeDigits(value.Trim());
            if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }
            return 0;
        }


        private void CloseControl()
        {
             CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void SelectPortsUnitButton_Click(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedValue is object unit)
            {
                if (unit.ToString() == "حبة")
                {
                    // YieldRateTextBox removed
                }
                else
                {
                    // YieldRateTextBox removed
                }

                if (unit.ToString() == "ذبيحة")
                {
                    if (NafarLabel1 != null) NafarLabel1.Text = "سعر ذبيحة";
                    if (NafarLabel2 != null) NafarLabel2.Text = "سعر نص ذبيحة";
                    
                }
                else
                {
                    if (NafarLabel1 != null) NafarLabel1.Text = "سعر النفر";
                    if (NafarLabel2 != null) NafarLabel2.Text = "سعر نص نفر";
                    
                }
            }
        }

        private ComponentType? GetComponentTypeFromName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (name.Contains("رأس")) return ComponentType.Head;
            if (name.Contains("ظهر")) return ComponentType.Back;
            if (name.Contains("كبد")) return ComponentType.Liver;
            if (name.Contains("ذبيحة") || name.Contains("لحم")) return ComponentType.Meat;
            return null;
        }


    }
}
