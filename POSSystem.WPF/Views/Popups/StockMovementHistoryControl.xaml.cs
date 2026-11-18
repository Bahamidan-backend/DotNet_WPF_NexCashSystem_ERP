using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Views.Popups
{
    public partial class StockMovementHistoryControl : UserControl
    {
        private readonly IStockMovementService _stockMovementService;
        private readonly int? _warehouseId;
        private readonly int? _productId;
        private readonly string _productName;

        public StockMovementHistoryControl(int? warehouseId = null, int? productId = null, string productName = "")
        {
            InitializeComponent();
            _stockMovementService = POSSystem.WPF.App.ServiceProvider.GetRequiredService<IStockMovementService>();
            _warehouseId = warehouseId;
            _productId = productId;
            _productName = productName ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(_productName))
            {
                SubtitleTextBlock.Text = $"المادة: {_productName}";
            }

            Loaded += async (_, _) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                List<StockMovement> movements = new();

                if (_productId.HasValue && _productId.Value > 0)
                {
                    var result = await _stockMovementService.GetMovementsByProductAsync(_productId.Value);
                    movements = result.ToList();
                }
                else if (_warehouseId.HasValue && _warehouseId.Value > 0)
                {
                    var result = await _stockMovementService.GetMovementsByWarehouseAsync(
                        _warehouseId.Value,
                        DateTime.Today.AddDays(-30),
                        DateTime.Today.AddDays(1));
                    movements = result.ToList();
                }

                MovementsDataGrid.ItemsSource = movements;
                CountTextBlock.Text = $"إجمالي الحركات: {movements.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء تحميل سجل الحركات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
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
