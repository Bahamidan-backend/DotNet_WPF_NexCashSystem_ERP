using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Views.Popups
{
    public partial class TransferStockControl : UserControl
    {
        private readonly IWarehouseService _warehouseService;
        private List<Warehouse> _allWarehouses = new();
        private List<WarehouseFromProduct> _allProducts = new();
        private int? _preselectedSourceWarehouseId;
        private int? _preselectedProductId;

        public class ProductTransferItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal AvailableQuantity { get; set; }
            public string PortionUnitText { get; set; } = string.Empty;
            public Recipe.PortionUnit? PortionUnit { get; set; }

            public string DisplayName => $"{Name} (المتاح: {AvailableQuantity:N0} {PortionUnitText})";
        }

        public TransferStockControl(int? sourceWarehouseId = null, int? productId = null)
        {
            InitializeComponent();
            _warehouseService = POSSystem.WPF.App.ServiceProvider.GetRequiredService<IWarehouseService>();
            _preselectedSourceWarehouseId = sourceWarehouseId;
            _preselectedProductId = productId;

            Loaded += async (_, _) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var warehouses = (await _warehouseService.GetAllWarehousesAsync())
                    .Where(w => w.IsActive)
                    .ToList();
                _allWarehouses = warehouses;

                _allProducts = _warehouseService.GetWarehouseFromProductAll().ToList();

                SourceWarehouseComboBox.ItemsSource = _allWarehouses;

                if (_preselectedSourceWarehouseId.HasValue && _allWarehouses.Any(w => w.Id == _preselectedSourceWarehouseId.Value))
                {
                    SourceWarehouseComboBox.SelectedValue = _preselectedSourceWarehouseId.Value;
                }
                else if (_allWarehouses.Count > 0)
                {
                    SourceWarehouseComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء تحميل بيانات المستودعات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SourceWarehouseComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SourceWarehouseComboBox.SelectedValue is not int sourceId) return;

            // Destination warehouses: all warehouses except the source warehouse
            var destinations = _allWarehouses.Where(w => w.Id != sourceId).ToList();
            DestinationWarehouseComboBox.ItemsSource = destinations;
            if (destinations.Count > 0)
            {
                DestinationWarehouseComboBox.SelectedIndex = 0;
            }

            // Products in source warehouse with stock
            var availableItems = _allProducts
                .Where(p => p.WarehouseId == sourceId)
                .Select(p => new ProductTransferItem
                {
                    Id = p.Id,
                    Name = p.Name ?? "صنف غير مسمى",
                    AvailableQuantity = p.StockQuantity,
                    PortionUnit = p.PortionUnit,
                    PortionUnitText = p.PortionUnit.HasValue ? PortionUnitHelper.GetDisplayName(p.PortionUnit.Value) : ""
                })
                .ToList();

            ProductComboBox.ItemsSource = availableItems;

            if (_preselectedProductId.HasValue && availableItems.Any(p => p.Id == _preselectedProductId.Value))
            {
                ProductComboBox.SelectedValue = _preselectedProductId.Value;
                _preselectedProductId = null; // Clear after first selection
            }
            else if (availableItems.Count > 0)
            {
                ProductComboBox.SelectedIndex = 0;
            }
            else
            {
                AvailableStockBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductComboBox.SelectedItem is ProductTransferItem item)
            {
                AvailableStockText.Text = item.AvailableQuantity.ToString("N0");
                PortionUnitText.Text = item.PortionUnitText;
                AvailableStockBorder.Visibility = Visibility.Visible;
            }
            else
            {
                AvailableStockBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void QuantityTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow numbers and decimal separator
            var regex = new Regex(@"^[0-9]*(\.[0-9]*)?$");
            var currentText = (sender as TextBox)?.Text ?? "";
            var prospectiveText = currentText.Insert((sender as TextBox)?.SelectionStart ?? 0, e.Text);
            e.Handled = !regex.IsMatch(prospectiveText);
        }

        private async void ConfirmTransferButton_Click(object sender, RoutedEventArgs e)
        {
            if (SourceWarehouseComboBox.SelectedValue is not int sourceId)
            {
                MessageBox.Show("يرجى اختيار المستودع المصدر.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DestinationWarehouseComboBox.SelectedValue is not int destId)
            {
                MessageBox.Show("يرجى اختيار المستودع الهدف.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (sourceId == destId)
            {
                MessageBox.Show("لا يمكن التحويل لنفس المستودع.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ProductComboBox.SelectedItem is not ProductTransferItem selectedProduct)
            {
                MessageBox.Show("يرجى اختيار المادة المراد تحويلها.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(QuantityTextBox.Text.Trim(), out decimal quantity) || quantity <= 0)
            {
                MessageBox.Show("يرجى إدخال كمية تحويل صحيحة أكبر من الصفر.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                QuantityTextBox.Focus();
                return;
            }

            if (quantity > selectedProduct.AvailableQuantity)
            {
                MessageBox.Show($"الكمية المطلوبة ({quantity:N0}) تتجاوز الرصيد المتوفر ({selectedProduct.AvailableQuantity:N0}).", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                QuantityTextBox.Focus();
                return;
            }

            try
            {
                ConfirmTransferButton.IsEnabled = false;

                var result = await _warehouseService.TransferStockAsync(
                    sourceWarehouseId: sourceId,
                    destinationWarehouseId: destId,
                    warehouseProductId: selectedProduct.Id,
                    quantity: quantity,
                    notes: NotesTextBox.Text?.Trim());

                if (result.Success)
                {
                    MessageBox.Show(result.Message ?? "تم تحويل المخزون بنجاح.", "تم التحويل", MessageBoxButton.OK, MessageBoxImage.Information);
                    CloseOverlay();
                }
                else
                {
                    MessageBox.Show(result.Message ?? "فشلت عملية التحويل.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ غير متوقع: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ConfirmTransferButton.IsEnabled = true;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseOverlay();
        }

        private void CloseOverlay()
        {
            var window = Window.GetWindow(this) as AdminDashboardWindow;
            window?.CloseOverlay();
        }
    }
}

