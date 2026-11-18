using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;
using POSSystem.WPF.Services;
using POSSystem.WPF.ViewModels.Cashier;

namespace POSSystem.WPF.Views.Popups
{
    public partial class CashierLeftoverDisposalPopup : UserControl
    {
        public event EventHandler? CloseRequested;

        private readonly CashierViewModel? _cashierViewModel;
        private readonly User? _currentUser;
        private List<ProductOptionItem> _productOptions = new();
        private List<CartDisposalPreviewItem> _cartPreviewItems = new();
        private bool _isCartMode = false;
        private string _selectedReasonTag = "StaffMeal";

        public class ProductOptionItem
        {
            public Product Product { get; set; } = null!;
            public string DisplayText { get; set; } = string.Empty;
            public override string ToString() => DisplayText;
        }

        public class CartDisposalPreviewItem
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public string? SizeName { get; set; }
            public decimal DeductionQty { get; set; }
            public string DisplayTitle { get; set; } = string.Empty;
            public string DeductionDisplay { get; set; } = string.Empty;
            public string RemainingAfterDisplay { get; set; } = string.Empty;
        }

        public CashierLeftoverDisposalPopup(CashierViewModel? cashierViewModel, User? currentUser)
        {
            InitializeComponent();
            _cashierViewModel = cashierViewModel;
            _currentUser = currentUser;
            _ = LoadDataAsync();
        }

        private static string FormatQty(decimal val)
        {
            return val.ToString("N2", CultureInfo.InvariantCulture).Replace('.', ',');
        }

        private static decimal GetPortionFactor(string? sizeName)
        {
            if (string.IsNullOrWhiteSpace(sizeName)) return 1.0m;
            var s = sizeName.Trim();
            if (s.Contains("ونص") || s.Contains("1.5")) return 1.5m;
            if (s.Contains("نص") || s.Contains("نصف") || s.Contains("0.5")) return 0.5m;
            if (s.Contains("ربع") || s.Contains("0.25")) return 0.25m;
            return 1.0m;
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var context = ServiceLocator.UnitOfWork.GetContext();
                var rawProducts = await context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Where(p => p.IsActive && !p.IsAnimalComponent)
                    .ToListAsync();

                var products = rawProducts
                    .Where(p => !p.IsUntrackedStock)
                    .OrderByDescending(p => p.AvailableQtyInPOS)
                    .ThenBy(p => p.Name)
                    .ToList();

                _productOptions = products.Select(p => new ProductOptionItem
                {
                    Product = p,
                    DisplayText = $"{p.Name}   —   (المتبقي حالياً: {FormatQty(p.AvailableQtyInPOS)})"
                }).ToList();

                ProductComboBox.ItemsSource = _productOptions;
                if (_productOptions.Any())
                    ProductComboBox.SelectedIndex = 0;

                // Check if cashier currently has items in the cart
                var cartItems = _cashierViewModel?.cashierProberty?.OrderItems?.ToList() ?? new List<OrderItem>();
                if (cartItems.Count > 0)
                {
                    _cartPreviewItems.Clear();
                    foreach (var item in cartItems)
                    {
                        int prodId = item.ProductId ?? item.Product?.Id ?? 0;
                        if (prodId <= 0) continue;

                        var dbProd = rawProducts.FirstOrDefault(p => p.Id == prodId) ?? item.Product;
                        if (dbProd != null && dbProd.IsUntrackedStock) continue;

                        decimal factor = GetPortionFactor(item.Size);
                        decimal deduction = item.Quantity * factor;
                        decimal currentPos = dbProd?.AvailableQtyInPOS ?? 0m;
                        decimal afterPos = Math.Max(0m, currentPos - deduction);
                        string sizeText = !string.IsNullOrWhiteSpace(item.Size) ? $" ({item.Size})" : "";

                        _cartPreviewItems.Add(new CartDisposalPreviewItem
                        {
                            ProductId = prodId,
                            ProductName = dbProd?.Name ?? item.Name,
                            SizeName = item.Size,
                            DeductionQty = deduction,
                            DisplayTitle = $"{dbProd?.Name ?? item.Name}{sizeText} × {item.Quantity}",
                            DeductionDisplay = $"صرف: -{FormatQty(deduction)}",
                            RemainingAfterDisplay = $"سيتبقى: {FormatQty(afterPos)}"
                        });
                    }

                    if (_cartPreviewItems.Count > 0)
                    {
                        CartPreviewItemsControl.ItemsSource = _cartPreviewItems;
                        SourceModeBorder.Visibility = Visibility.Visible;
                        SetMode(isCartMode: true);
                        SecretPinPasswordBox.Focus();
                        return;
                    }
                }

                SourceModeBorder.Visibility = Visibility.Collapsed;
                SetMode(isCartMode: false);
                SecretPinPasswordBox.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل بيانات الأصناف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetMode(bool isCartMode)
        {
            _isCartMode = isCartMode;
            if (isCartMode)
            {
                ModeCartBtn.Background = new SolidColorBrush(Color.FromRgb(0, 53, 140));
                ModeCartBtn.Foreground = Brushes.White;
                ModeDirectBtn.Background = Brushes.Transparent;
                ModeDirectBtn.Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85));

                CartItemsSection.Visibility = Visibility.Visible;
                DirectProductSection.Visibility = Visibility.Collapsed;
            }
            else
            {
                ModeDirectBtn.Background = new SolidColorBrush(Color.FromRgb(0, 53, 140));
                ModeDirectBtn.Foreground = Brushes.White;
                ModeCartBtn.Background = Brushes.Transparent;
                ModeCartBtn.Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85));

                CartItemsSection.Visibility = Visibility.Collapsed;
                DirectProductSection.Visibility = Visibility.Visible;
                UpdateDirectPreview();
            }
        }

        private void ModeCartBtn_Click(object sender, RoutedEventArgs e) => SetMode(true);
        private void ModeDirectBtn_Click(object sender, RoutedEventArgs e) => SetMode(false);

        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductComboBox.SelectedItem is ProductOptionItem opt)
            {
                if (opt.Product.AvailableQtyInPOS > 0)
                {
                    DisposeQtyTextBox.Text = opt.Product.AvailableQtyInPOS.ToString("0.##", CultureInfo.InvariantCulture);
                }
                else
                {
                    DisposeQtyTextBox.Text = "1";
                }
            }
            UpdateDirectPreview();
        }

        private void DisposeQtyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateDirectPreview();
        }

        private void QuickQtyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                DisposeQtyTextBox.Text = btn.Tag.ToString() ?? "1";
                UpdateDirectPreview();
            }
        }

        private void QuickFullRemainingBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ProductComboBox.SelectedItem is ProductOptionItem opt)
            {
                DisposeQtyTextBox.Text = Math.Max(0m, opt.Product.AvailableQtyInPOS).ToString("0.##", CultureInfo.InvariantCulture);
                UpdateDirectPreview();
            }
        }

        private void UpdateDirectPreview()
        {
            if (ProductComboBox?.SelectedItem is not ProductOptionItem opt) return;
            decimal currentPos = opt.Product.AvailableQtyInPOS;
            if (CurrentPosRemainingText != null)
                CurrentPosRemainingText.Text = FormatQty(currentPos);

            decimal qty = 0m;
            if (DisposeQtyTextBox != null)
            {
                decimal.TryParse(DisposeQtyTextBox.Text.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out qty);
            }

            if (AfterDisposalRemainingText != null)
            {
                decimal after = Math.Max(0m, currentPos - Math.Max(0m, qty));
                AfterDisposalRemainingText.Text = FormatQty(after);
                AfterDisposalRemainingText.Foreground = qty > currentPos
                    ? new SolidColorBrush(Color.FromRgb(220, 38, 38))
                    : new SolidColorBrush(Color.FromRgb(5, 150, 105));
            }
        }

        private void ReasonBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button clicked || clicked.Tag == null) return;
            _selectedReasonTag = clicked.Tag.ToString() ?? "StaffMeal";

            var allBtns = new[] { ReasonStaffBtn, ReasonCharityBtn, ReasonWasteBtn, ReasonOtherBtn };
            foreach (var btn in allBtns)
            {
                if (btn == null) continue;
                bool isSelected = btn == clicked;
                btn.Background = isSelected
                    ? new SolidColorBrush(Color.FromRgb(0, 53, 140))
                    : new SolidColorBrush(Color.FromRgb(248, 250, 252));
                btn.Foreground = isSelected
                    ? Brushes.White
                    : new SolidColorBrush(Color.FromRgb(51, 65, 85));
                btn.BorderBrush = isSelected
                    ? new SolidColorBrush(Color.FromRgb(0, 53, 140))
                    : new SolidColorBrush(Color.FromRgb(203, 213, 225));
            }
        }

        private void SecretPinPasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (PinErrorText != null)
                PinErrorText.Visibility = Visibility.Collapsed;

            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                ConfirmDisposalButton_Click(sender, new RoutedEventArgs());
            }
        }

        /// <summary>
        /// التحقق من كلمة سر المناوبة الموحدة (SettingsService.LoadSettings().ShiftPassword)
        /// كما يقبل أيضاً كلمة مرور المدير كخيار احتياطي.
        /// </summary>
        private async Task<(bool IsValid, string ApproverLabel)> VerifyShiftPasswordAsync(string enteredPassword)
        {
            try
            {
                var settings = SettingsService.LoadSettings();
                string shiftPassword = settings?.ShiftPassword ?? "1234";

                if (!string.IsNullOrEmpty(shiftPassword) && enteredPassword == shiftPassword)
                {
                    string cashierName = !string.IsNullOrWhiteSpace(_currentUser?.Name) ? _currentUser.Name : "الكاشير";
                    return (true, $"{cashierName} (بكلمة سر المناوبة)");
                }

                // Fallback: also allow Admin login password
                await using var context = new POSDbContext();
                var admins = await context.Users
                    .AsNoTracking()
                    .Where(u => u.IsActive && (u.Role == UserRole.Admin || u.RoleTemplateName == "مدير النظام"))
                    .ToListAsync();

                foreach (var admin in admins)
                {
                    if (PasswordHasher.VerifyPassword(enteredPassword, admin.Password))
                    {
                        return (true, admin.Name);
                    }
                }

                return (false, string.Empty);
            }
            catch
            {
                return (false, string.Empty);
            }
        }

        private async void ConfirmDisposalButton_Click(object sender, RoutedEventArgs e)
        {
            PinErrorText.Visibility = Visibility.Collapsed;

            string pin = SecretPinPasswordBox.Password?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(pin))
            {
                PinErrorText.Text = "⚠️ يرجى إدخال كلمة سر المناوبة لاعتماد عملية الصرف.";
                PinErrorText.Visibility = Visibility.Visible;
                SecretPinPasswordBox.Focus();
                return;
            }

            var (isValid, approverLabel) = await VerifyShiftPasswordAsync(pin);
            if (!isValid)
            {
                PinErrorText.Text = "❌ كلمة سر المناوبة غير صحيحة! يرجى التأكد والمحاولة مرة أخرى.";
                PinErrorText.Visibility = Visibility.Visible;
                SecretPinPasswordBox.SelectAll();
                SecretPinPasswordBox.Focus();
                return;
            }

            var itemsToDispose = new List<LeftoverDisposalItemDto>();

            if (_isCartMode && _cartPreviewItems.Count > 0)
            {
                foreach (var ci in _cartPreviewItems)
                {
                    itemsToDispose.Add(new LeftoverDisposalItemDto
                    {
                        ProductId = ci.ProductId,
                        ProductName = ci.ProductName,
                        QuantityToDispose = ci.DeductionQty,
                        SizeName = ci.SizeName
                    });
                }
            }
            else
            {
                if (ProductComboBox.SelectedItem is not ProductOptionItem opt)
                {
                    MessageBox.Show("يرجى اختيار الصنف المراد صرفه.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string qtyStr = DisposeQtyTextBox.Text.Trim().Replace(',', '.');
                if (!decimal.TryParse(qtyStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal qty) || qty <= 0)
                {
                    MessageBox.Show("يرجى إدخال كمية صحيحة أكبر من الصفر للصرف.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (qty > opt.Product.AvailableQtyInPOS)
                {
                    MessageBox.Show($"الكمية المدخلة ({FormatQty(qty)}) أكبر من المتبقي حالياً في الكاشير ({FormatQty(opt.Product.AvailableQtyInPOS)}).", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                itemsToDispose.Add(new LeftoverDisposalItemDto
                {
                    ProductId = opt.Product.Id,
                    ProductName = opt.Product.Name,
                    QuantityToDispose = qty
                });
            }

            var (wasteReason, categoryLabel) = _selectedReasonTag switch
            {
                "StaffMeal" => (WasteReason.StaffMeal, "غداء / إطعام العمال"),
                "Charity" => (WasteReason.Charity, "صدقة / تبرع خيري"),
                "Waste" => (WasteReason.GeneralWaste, "هدر / تالف نهاية اليوم"),
                _ => (WasteReason.GeneralWaste, "ضيافة / صرف آخر")
            };

            try
            {
                ConfirmDisposalButton.IsEnabled = false;
                await ServiceLocator.KitchenCookingBatchService.DisposeLeftoverPosStockAsync(
                    itemsToDispose,
                    wasteReason,
                    categoryLabel,
                    DisposalNotesTextBox.Text,
                    _currentUser?.Id,
                    approverLabel);

                // If disposed from cart, clear the cashier cart immediately
                if (_isCartMode && _cashierViewModel != null)
                {
                    _cashierViewModel.CancelCurrentInvoice();
                }

                if (_cashierViewModel != null)
                {
                    await _cashierViewModel.LoadProductsAsync();
                    _cashierViewModel.FilterProducts();
                }

                string summaryLines = string.Join("\n", itemsToDispose.Select(i => $"• {i.ProductName}: -{FormatQty(i.QuantityToDispose)}"));
                MessageBox.Show(
                    $"✅ تم اعتماد وصرف الكمية من الكاشير بنجاح!\n\n" +
                    $"نوع الصرف: {categoryLabel}\n" +
                    $"الأصناف المصروفة:\n{summaryLines}\n\n" +
                    $"تم الاعتماد بواسطة: {approverLabel}",
                    "تم صرف المتبقي بنجاح",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"تعذر إتمام عملية الصرف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                ConfirmDisposalButton.IsEnabled = true;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
