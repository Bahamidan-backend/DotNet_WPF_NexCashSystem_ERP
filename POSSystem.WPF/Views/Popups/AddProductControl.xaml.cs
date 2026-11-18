using Microsoft.Win32;
using POSSystem.WPF.Models;
using POSSystem.WPF.Services;
using POSSystem.WPF.ViewModels.Products;
using POSSystem.WPF.Views.AdminPages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using static POSSystem.WPF.Models.Recipe;

namespace POSSystem.WPF.Views.Popups
{
    public partial class AddProductControl : UserControl
    {
        private ProductViewModel _viewModel = null!;
        private ProductsViewModel _parentViewModel = null!;
        private ProductService _productService;
        private WarehouseService _warehouseService;
        private UnitOfWork unitOfWork;
        public ObservableCollection<Product> _Product;

        private readonly IServiceScope _scope;

        public AddProductControl(ProductsViewModel parentViewModel)
        {
            try
            {
                InitializeComponent();
                _parentViewModel = parentViewModel;
                _viewModel = new ProductViewModel();
                DataContext = _viewModel;
                _scope = App.ServiceProvider.CreateScope();
                var context = _scope.ServiceProvider.GetRequiredService<POSDbContext>();
                unitOfWork = new UnitOfWork(context);
                _warehouseService = new WarehouseService(unitOfWork);
                _productService = new ProductService(unitOfWork);
                _Product = new ObservableCollection<Product>();
                Unloaded += (_, _) => 
                {
                    unitOfWork.Dispose();
                    _scope.Dispose();
                };

                _ = LoadAvailableUnit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تهيئة نافذة إضافة المنتج: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                CloseOverlay();
            }
        }

        public async Task LoadAvailableUnit()
        {
            try
            {
                InvontryTypeComb.ItemsSource = PortionUnitHelper.DisplayNames;
                InvontryTypeComb.DisplayMemberPath = "Value";
                InvontryTypeComb.SelectedValuePath = "Key";
                if (InvontryTypeComb.SelectedValue == null)
                    InvontryTypeComb.SelectedValue = PortionUnit.Bag;
                await _viewModel.LoadKitchensAsync(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void KitchenComboBox_DropDownOpened(object sender, EventArgs e)
        {
            var currentId = (_viewModel.SelectedKitchen ?? KitchenComboBox.SelectedItem as POSSystem.WPF.Models.Kitchen)?.Id;
            await _viewModel.LoadKitchensAsync(true);
            if (currentId.HasValue)
            {
                var match = _viewModel.Kitchens.FirstOrDefault(k => k.Id == currentId.Value);
                if (match != null)
                {
                    _viewModel.SelectedKitchen = match;
                    KitchenComboBox.SelectedItem = match;
                }
            }
        }

        private void AddKitchenButton_Click(object sender, RoutedEventArgs e)
        {
            QuickAddKitchenBorder.Visibility = Visibility.Visible;
            QuickKitchenNameTextBox.Focus();
        }

        private void CancelQuickAddKitchen_Click(object sender, RoutedEventArgs e)
        {
            QuickAddKitchenBorder.Visibility = Visibility.Collapsed;
            QuickKitchenNameTextBox.Text = string.Empty;
            QuickPrinterNameTextBox.Text = string.Empty;
        }

        private async void SaveQuickAddKitchen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = QuickKitchenNameTextBox.Text.Trim();
                string printerName = QuickPrinterNameTextBox.Text.Trim();

                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("يرجى إدخال اسم المطبخ", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    QuickKitchenNameTextBox.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(printerName))
                {
                    MessageBox.Show("يرجى إدخال اسم الطابعة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    QuickPrinterNameTextBox.Focus();
                    return;
                }

                var kitchen = new POSSystem.WPF.Models.Kitchen
                {
                    Name = name,
                    PrinterName = printerName,
                    IsActive = true,
                    IsPrinterEnabled = true,
                    CreatedAt = DateTime.Now
                };

                await App.ServiceProvider.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IKitchenService>().CreateKitchenAsync(kitchen);
                await _viewModel.LoadKitchensAsync(true);
                _viewModel.SelectedKitchen = _viewModel.Kitchens.FirstOrDefault(k => k.Name == name);
                KitchenComboBox.SelectedItem = _viewModel.SelectedKitchen;

                QuickAddKitchenBorder.Visibility = Visibility.Collapsed;
                QuickKitchenNameTextBox.Text = string.Empty;
                QuickPrinterNameTextBox.Text = string.Empty;
                MessageBox.Show("تم إضافة المطبخ بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء إضافة المطبخ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
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
                _viewModel.ImagePath = openFileDialog.FileName;
                UpdateImagePreview(openFileDialog.FileName);
                MessageBox.Show($"تم اختيار الصورة: {System.IO.Path.GetFileName(openFileDialog.FileName)}",
                    "تم اختيار الصورة", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RemoveImageButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ImagePath = string.Empty;
            UpdateImagePreview(string.Empty);
        }

        private void UpdateImagePreview(string? path)
        {
            if (ProductImagePreview == null || NoImagePlaceholder == null) return;

            if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
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

        public async void LoidForDateProduct()
        {
            try
            {
                _Product.Clear();
                var GetAlLDate = await _productService.GetAllProductsAsync();
                foreach (var Products in GetAlLDate)
                {
                    _Product.Add(Products);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل منتجات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_viewModel.Name))
            {
                MessageBox.Show("يرجى إدخال اسم المنتج", "خطأ في التحقق",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            decimal pHaba = ParsePrice(_viewModel.PriceHaba);
            decimal pNusHaba = ParsePrice(_viewModel.PriceNusHaba);
            decimal pRobHaba = ParsePrice(_viewModel.PriceRobHaba);

            decimal pNafar = ParsePrice(_viewModel.PriceNafar);
            decimal pNusNafar = ParsePrice(_viewModel.PriceNusNafar);
            decimal pNafarWaNus = ParsePrice(_viewModel.PriceRobNafar);

            decimal pKabeer = ParsePrice(_viewModel.PriceKabeer);
            decimal pWasat = ParsePrice(_viewModel.PriceWasat);
            decimal pSagheer = ParsePrice(_viewModel.PriceSagheer);

            bool hasPrice = false;
            if (_viewModel.IsBasePriceVisible)
            {
                hasPrice = _viewModel.Price > 0;
            }
            else if (_viewModel.IsHabaVisible)
            {
                hasPrice = pHaba > 0 || pNusHaba > 0 || pRobHaba > 0;
            }
            else if (_viewModel.IsNafarVisible)
            {
                hasPrice = pNafar > 0 || pNusNafar > 0 || pNafarWaNus > 0;
            }
            else if (_viewModel.IsKabeerVisible)
            {
                hasPrice = pKabeer > 0 || pWasat > 0 || pSagheer > 0;
            }

            if (!hasPrice)
            {
                MessageBox.Show("يرجى إدخال سعر صحيح للمنتج", "خطأ في التحقق",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal parsedStock = 0;
            decimal parsedNafar = 1;

            if (_viewModel.SelectedCategory == null)
            {
                MessageBox.Show("يرجى اختيار فئة للمنتج", "خطأ في التحقق",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CategoryDrobDaown.Focus();
                return;
            }

            bool isUntracked = UntrackedStockCheckBox?.IsChecked == true;
            if (!isUntracked && InvontryTypeComb.SelectedValue == null)
            {
                MessageBox.Show("يرجى اختيار وحدة قياس للمنتج", "خطأ في التحقق",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                InvontryTypeComb.Focus();
                return;
            }

            try
            {
                var selectedUnit = InvontryTypeComb.SelectedValue is PortionUnit pu ? pu : PortionUnit.Piece;

                decimal basePriceToSave = _viewModel.Price;
                if (!_viewModel.IsBasePriceVisible)
                {
                    if (_viewModel.IsHabaVisible) basePriceToSave = pHaba > 0 ? pHaba : (pNusHaba > 0 ? pNusHaba : pRobHaba);
                    else if (_viewModel.IsNafarVisible) basePriceToSave = pNafar > 0 ? pNafar : (pNusNafar > 0 ? pNusNafar : pNafarWaNus);
                    else if (_viewModel.IsKabeerVisible) basePriceToSave = pKabeer > 0 ? pKabeer : (pWasat > 0 ? pWasat : pSagheer);
                }

                var selectedKitchen = _viewModel.SelectedKitchen ?? (KitchenComboBox.SelectedItem as POSSystem.WPF.Models.Kitchen);

                var newProduct = new Product
                {
                    Name = _viewModel.Name,
                    Description = _viewModel.Description,
                    Price = basePriceToSave,
                    BasePrice = basePriceToSave,
                    PersonNumberFood = parsedNafar,
                    StockQuantity = parsedStock,
                    ImagePath = _viewModel.ImagePath,
                    CategoryName = _viewModel.SelectedCategory.Name,
                    CategoryId = _viewModel.SelectedCategory.Id,
                    KitchenId = selectedKitchen?.Id,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    portionUnit = selectedUnit,
                    TypeWarehoseFromProduct = isUntracked ? Product.UntrackedStockMarker : (_viewModel.SelectedWarehouseFromProduct?.Name ?? string.Empty),
                    IsAnimalComponent = false,
                    ProductSizes = new List<ProductSize>()
                };

                // Add Sizes:
                // - Haba: حبة / نص حبة / ربع حبة
                // - Nafar: نفر / نص نفر / نفر ونص
                if (_viewModel.IsHabaVisible || (pHaba > 0 || pNusHaba > 0 || pRobHaba > 0))
                {
                    if (pHaba > 0) newProduct.ProductSizes.Add(new ProductSize { Name = "حبة", SizeName = "حبة", Price = pHaba, BasePrice = pHaba, IsActive = true });
                    if (pNusHaba > 0) newProduct.ProductSizes.Add(new ProductSize { Name = "نص حبة", SizeName = "نص حبة", Price = pNusHaba, BasePrice = pNusHaba, IsActive = true });
                    if (pRobHaba > 0) newProduct.ProductSizes.Add(new ProductSize { Name = "ربع حبة", SizeName = "ربع حبة", Price = pRobHaba, BasePrice = pRobHaba, IsActive = true });
                }
                else if (_viewModel.IsNafarVisible || (pNafar > 0 || pNusNafar > 0 || pNafarWaNus > 0))
                {
                    string label1 = "نفر";
                    string label2 = "نص نفر";
                    string label3 = "نفر ونص";

                    if (selectedUnit == PortionUnit.Carcass || selectedUnit.ToString() == "ذبيحة")
                    {
                        label1 = "ذبيحة";
                        label2 = "نص ذبيحة";
                        label3 = "ذبيحة ونص";
                    }

                    if (pNafar > 0) newProduct.ProductSizes.Add(new ProductSize { Name = label1, SizeName = label1, Price = pNafar, BasePrice = pNafar, IsActive = true });
                    if (pNusNafar > 0) newProduct.ProductSizes.Add(new ProductSize { Name = label2, SizeName = label2, Price = pNusNafar, BasePrice = pNusNafar, IsActive = true });
                    if (pNafarWaNus > 0) newProduct.ProductSizes.Add(new ProductSize { Name = label3, SizeName = label3, Price = pNafarWaNus, BasePrice = pNafarWaNus, IsActive = true });
                }
                else if (_viewModel.IsKabeerVisible || (pKabeer > 0 || pWasat > 0 || pSagheer > 0))
                {
                    if (pKabeer > 0) newProduct.ProductSizes.Add(new ProductSize { Name = "كبير", SizeName = "كبير", Price = pKabeer, BasePrice = pKabeer, IsActive = true });
                    if (pWasat > 0) newProduct.ProductSizes.Add(new ProductSize { Name = "وسط", SizeName = "وسط", Price = pWasat, BasePrice = pWasat, IsActive = true });
                    if (pSagheer > 0) newProduct.ProductSizes.Add(new ProductSize { Name = "صغير", SizeName = "صغير", Price = pSagheer, BasePrice = pSagheer, IsActive = true });
                }

                newProduct.TotalValue = newProduct.PersonNumberFood * newProduct.StockQuantity;

                await _parentViewModel.AddProduct(newProduct);

                MessageBox.Show("تم إضافة المنتج بنجاح!", "تم الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseOverlay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء حفظ المنتج: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("هل أنت متأكد من إلغاء إضافة المنتج؟",
                "تأكيد الإلغاء", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                CloseOverlay();
            }
        }

        private void CloseOverlay()
        {
            var window = Window.GetWindow(this) as AdminDashboardWindow;
            window?.CloseOverlay();
        }

        private static string NormalizeDigits(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value
                .Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
                .Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9')
                .Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4')
                .Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9')
                .Replace('٬', '.').Replace('٫', '.').Replace(",", "");
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

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is Category category)
            {
                _viewModel.SelectedCategory = category;
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

        private void SelectPortsUnitButton_Click(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedValue is object unit)
            {
                if (unit.ToString() == "ذبيحة")
                {
                    if (NafarLabel1 != null) NafarLabel1.Text = "سعر ذبيحة";
                    if (NafarLabel2 != null) NafarLabel2.Text = "سعر نص ذبيحة";
                    if (NafarLabel3 != null) NafarLabel3.Text = "سعر ذبيحة ونص";
                }
                else
                {
                    if (NafarLabel1 != null) NafarLabel1.Text = "سعر النفر";
                    if (NafarLabel2 != null) NafarLabel2.Text = "سعر نص نفر";
                    if (NafarLabel3 != null) NafarLabel3.Text = "سعر نفر ونص";
                }
            }
        }

        private void DescriptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private async void ManageCategoriesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var manageCategoriesControl = new POSSystem.WPF.Views.Popups.ManageCategoriesControl();
                var window = new Window
                {
                    Title = "إدارة الفئات",
                    Content = manageCategoriesControl,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,
                    WindowStyle = WindowStyle.ToolWindow
                };

                window.ShowDialog();
                await _viewModel.LoadCategoriesAsync(true);
                _parentViewModel.RefreshCategoriesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح نافذة إدارة الفئات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
