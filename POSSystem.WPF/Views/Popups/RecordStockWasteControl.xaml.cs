using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.WPF.Business;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Views.Popups
{
    public partial class RecordStockWasteControl : UserControl
    {
        public event EventHandler? CloseRequested;
        public event EventHandler? WasteRecorded;

        private readonly IWarehouseService _warehouseService;
        private readonly IStockWasteService _stockWasteService;

        private List<Warehouse> _warehouses = new();
        private List<WarehouseFromProduct> _warehouseProducts = new();
        private WarehouseFromProduct? _selectedProduct;
        private int? _preselectedWarehouseId;

        public RecordStockWasteControl(int? preselectedWarehouseId = null, int? preselectedProductId = null)
        {
            InitializeComponent();
            _warehouseService = POSSystem.WPF.App.ServiceProvider.GetRequiredService<IWarehouseService>();
            _stockWasteService = POSSystem.WPF.App.ServiceProvider.GetRequiredService<IStockWasteService>();
            _preselectedWarehouseId = preselectedWarehouseId;

            InitializeReasons();
            Loaded += async (s, e) => await LoadDataAsync(preselectedProductId);
        }

        private void InitializeReasons()
        {
            ReasonComboBox.ItemsSource = new[]
            {
                new { Value = WasteReason.CookingBurn, Text = WasteReasonHelper.GetArabicText(WasteReason.CookingBurn) },
                new { Value = WasteReason.Expired, Text = WasteReasonHelper.GetArabicText(WasteReason.Expired) },
                new { Value = WasteReason.PreparationLoss, Text = WasteReasonHelper.GetArabicText(WasteReason.PreparationLoss) },
                new { Value = WasteReason.SpillageOrBreakage, Text = WasteReasonHelper.GetArabicText(WasteReason.SpillageOrBreakage) },
                new { Value = WasteReason.CustomerReturn, Text = WasteReasonHelper.GetArabicText(WasteReason.CustomerReturn) },
                new { Value = WasteReason.GeneralWaste, Text = WasteReasonHelper.GetArabicText(WasteReason.GeneralWaste) }
            };
            ReasonComboBox.SelectedIndex = 0;
        }

        private async Task LoadDataAsync(int? preselectedProductId = null)
        {
            try
            {
                var allWarehouses = await _warehouseService.GetAllWarehousesAsync();
                _warehouses = allWarehouses.Where(w => w.IsActive).ToList();
                WarehouseComboBox.ItemsSource = _warehouses;

                if (_preselectedWarehouseId.HasValue && _warehouses.Any(w => w.Id == _preselectedWarehouseId.Value))
                {
                    WarehouseComboBox.SelectedValue = _preselectedWarehouseId.Value;
                }
                else if (_warehouses.Any())
                {
                    WarehouseComboBox.SelectedIndex = 0;
                }

                if (preselectedProductId.HasValue && _warehouseProducts.Any(p => p.Id == preselectedProductId.Value))
                {
                    ProductComboBox.SelectedValue = preselectedProductId.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل بيانات المستودعات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WarehouseComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (WarehouseComboBox.SelectedValue is int warehouseId)
            {
                try
                {
                    var allWhProducts = _warehouseService.GetWarehouseFromProductAll() ?? Enumerable.Empty<WarehouseFromProduct>();
                    _warehouseProducts = allWhProducts
                        .Where(p => p.WarehouseId == warehouseId && p.IsActive)
                        .OrderBy(p => p.Name)
                        .ToList();

                    ProductComboBox.ItemsSource = _warehouseProducts.Select(p => new
                    {
                        p.Id,
                        DisplayName = $"{p.Name} (المتوفر: {p.StockQuantity:N0} {(p.PortionUnit.HasValue ? PortionUnitHelper.GetDisplayName(p.PortionUnit.Value) : string.Empty)})"
                    }).ToList();

                    if (_warehouseProducts.Any())
                    {
                        ProductComboBox.SelectedIndex = 0;
                    }
                    else
                    {
                        _selectedProduct = null;
                        AvailableStockBorder.Visibility = Visibility.Collapsed;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في تحميل أصناف المستودع: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductComboBox.SelectedValue is int productId)
            {
                _selectedProduct = _warehouseProducts.FirstOrDefault(p => p.Id == productId);
                if (_selectedProduct != null)
                {
                    AvailableStockBorder.Visibility = Visibility.Visible;
                    AvailableStockText.Text = _selectedProduct.StockQuantity.ToString("N0");
                    PortionUnitText.Text = _selectedProduct.PortionUnit.HasValue 
                        ? PortionUnitHelper.GetDisplayName(_selectedProduct.PortionUnit.Value) 
                        : string.Empty;
                    UnitCostText.Text = _selectedProduct.Price.ToString("N0");
                    RecalculateTotalLoss();
                }
            }
            else
            {
                _selectedProduct = null;
                AvailableStockBorder.Visibility = Visibility.Collapsed;
                TotalLossText.Text = "0.00";
            }
        }

        private void QuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RecalculateTotalLoss();
        }

        private void RecalculateTotalLoss()
        {
            if (_selectedProduct != null && decimal.TryParse(QuantityTextBox.Text.Trim(), out decimal qty) && qty > 0)
            {
                decimal total = Math.Round(qty * _selectedProduct.Price, 2);
                TotalLossText.Text = total.ToString("N0");
            }
            else
            {
                TotalLossText.Text = "0.00";
            }
        }

        private void QuantityTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9.]+$");
        }

        private async void ConfirmWasteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduct == null)
            {
                MessageBox.Show("يرجى اختيار الصنف أو المادة التالفة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(QuantityTextBox.Text.Trim(), out decimal quantity) || quantity <= 0)
            {
                MessageBox.Show("يرجى إدخال كمية هدر صحيحة أكبر من الصفر.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (quantity > _selectedProduct.StockQuantity)
            {
                var unitName = _selectedProduct.PortionUnit.HasValue
                    ? PortionUnitHelper.GetDisplayName(_selectedProduct.PortionUnit.Value)
                    : string.Empty;

                MessageBox.Show(
                    $"لا يمكن تسجيل هدر أكبر من الرصيد المتوفر في المستودع!\n\n" +
                    $"• المادة: {_selectedProduct.Name}\n" +
                    $"• الرصيد المتوفر حالياً: {_selectedProduct.StockQuantity:N0} {unitName}\n" +
                    $"• كمية الهدر المدخلة: {quantity:N0} {unitName}\n\n" +
                    "يرجى تصحيح الكمية أو إجراء تسوية جرد للمستودع في حال وجود عجز غير مفسر.",
                    "تجاوز الرصيد المتوفر",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            WasteReason reason = WasteReason.GeneralWaste;
            if (ReasonComboBox.SelectedValue is WasteReason r)
            {
                reason = r;
            }

            string? notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();
            int? currentUserId = App.ServiceProvider != null ? (ServiceLocator.Context?.Users?.FirstOrDefault(u => u.IsActive)?.Id) : null;

            ConfirmWasteButton.IsEnabled = false;
            try
            {
                await _stockWasteService.RecordWasteAsync(
                    warehouseFromProductId: _selectedProduct.Id,
                    quantity: quantity,
                    reason: reason,
                    notes: notes,
                    userId: currentUserId);

                MessageBox.Show(
                    $"تم تسجيل الهدر وخصم المخزون بنجاح.\nالمادة: {_selectedProduct.Name}\nالكمية المهدورة: {quantity:N0}\nالخسارة المالية: {decimal.Parse(TotalLossText.Text):N0} ريال",
                    "نجاح العملية",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                WasteRecorded?.Invoke(this, EventArgs.Empty);
                CloseRequested?.Invoke(this, EventArgs.Empty);
                CloseOverlay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"فشل في تسجيل الهدر: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ConfirmWasteButton.IsEnabled = true;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
            CloseOverlay();
        }

        private void CloseOverlay()
        {
            var window = Window.GetWindow(this) as AdminDashboardWindow;
            window?.CloseOverlay();
        }
    }
}
