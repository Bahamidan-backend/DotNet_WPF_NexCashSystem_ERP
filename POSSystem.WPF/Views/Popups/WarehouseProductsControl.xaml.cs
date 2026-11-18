using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using POSSystem.WPF.Business;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;
using POSSystem.WPF.Views.AdminPages;

namespace POSSystem.WPF.Views.Popups
{
    public partial class WarehouseProductsControl : UserControl
    {
        private readonly IWarehouseService _warehouseService;
        private readonly IUnitOfWork _unitOfWork;
        private ObservableCollection<WarehouseFromProduct> _warehouseFromProducts;
        private int _warehouseId;

        public WarehouseProductsControl(int warehouseId)
        {
            InitializeComponent();
            _warehouseId = warehouseId;

            _unitOfWork = POSSystem.WPF.App.ServiceProvider.GetRequiredService<IUnitOfWork>();
            _warehouseService = new WarehouseService(_unitOfWork);

            Loaded += (_, _) => ServiceLocator.InventoryChanged += OnInventoryChanged;
            Unloaded += (_, _) => ServiceLocator.InventoryChanged -= OnInventoryChanged;

            LoadProducts();
        }

        private void OnInventoryChanged()
        {
            Dispatcher.BeginInvoke(LoadProducts, DispatcherPriority.Background);
        }

        private async void LoadProducts()
        {
            try 
            {
                _unitOfWork.ClearEntityTracking();
                var productsFromDb = _warehouseService.GetWarehouseFromProductAll().ToList();
                var filteredProducts = productsFromDb.Where(x => x.WarehouseId == _warehouseId).ToList();
                _warehouseFromProducts = new ObservableCollection<WarehouseFromProduct>(filteredProducts);

                foreach (var p in _warehouseFromProducts)
                    p.IsSelected = false;
                if (SelectAllCheckBox != null)
                    SelectAllCheckBox.IsChecked = false;

                ProductsDataGrid.ItemsSource = _warehouseFromProducts;
            }
            catch(Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل المنتجات: {ex.Message}");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this) as AdminDashboardWindow;
            window?.CloseOverlay();
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            var productForm = new ProductFormInventoryControl(_warehouseId);
            var window = Window.GetWindow(this) as AdminDashboardWindow;
            window?.ShowOverlay(productForm);
        }


        private void EditProductButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = _warehouseFromProducts?.FirstOrDefault(p => p.IsSelected)
                ?? ProductsDataGrid.SelectedItem as WarehouseFromProduct;

            if (selected == null)
            {
                MessageBox.Show("الرجاء تحديد المنتج المراد تعديله أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var editForm = new ProductFormInventoryControl(_warehouseId, selected);
            var window = Window.GetWindow(this) as AdminDashboardWindow;
            window?.ShowOverlay(editForm);
        }

        private void EditRowProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is WarehouseFromProduct item)
            {
                var editForm = new ProductFormInventoryControl(_warehouseId, item);
                var window = Window.GetWindow(this) as AdminDashboardWindow;
                window?.ShowOverlay(editForm);
            }
        }

        private async void ProductsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (e.Row.Item is not WarehouseFromProduct item) return;

            _ = Dispatcher.BeginInvoke(async () =>
            {
                try
                {
                    var context = _unitOfWork.GetContext();
                    var dbItem = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                        context.warehouseFromProducts, x => x.Id == item.Id);
                    if (dbItem != null && !string.IsNullOrWhiteSpace(item.Name))
                    {
                        dbItem.Name = item.Name.Trim();
                        dbItem.MinimumStockLevel = item.MinimumStockLevel;
                        dbItem.UpdatedAt = DateTime.Now;
                        await _unitOfWork.SaveChangesAsync();
                        _unitOfWork.ClearEntityTracking();
                        ServiceLocator.NotifyInventoryChanged();
                    }
                }
                catch { }
            }, DispatcherPriority.Background);
        }

        private void TransferProductButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = _warehouseFromProducts?.FirstOrDefault(p => p.IsSelected) 
                ?? ProductsDataGrid.SelectedItem as WarehouseFromProduct;

            var transferControl = new TransferStockControl(_warehouseId, selected?.Id);
            var window = Window.GetWindow(this) as AdminDashboardWindow;
            window?.ShowOverlay(transferControl);
        }

        private void RecordWasteButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = _warehouseFromProducts?.FirstOrDefault(p => p.IsSelected) 
                ?? ProductsDataGrid.SelectedItem as WarehouseFromProduct;

            var wasteControl = new RecordStockWasteControl(_warehouseId, selected?.Id);
            var window = Window.GetWindow(this) as AdminDashboardWindow;
            if (window != null)
            {
                wasteControl.CloseRequested += (s, args) => window.CloseOverlay();
                wasteControl.WasteRecorded += (s, args) =>
                {
                    LoadProducts();
                    ServiceLocator.NotifyInventoryChanged();
                };
                window.ShowOverlay(wasteControl);
            }
        }

        private void ViewHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = _warehouseFromProducts?.FirstOrDefault(p => p.IsSelected) 
                ?? ProductsDataGrid.SelectedItem as WarehouseFromProduct;

            StockMovementHistoryControl historyControl;
            if (selected != null)
            {
                historyControl = new StockMovementHistoryControl(_warehouseId, selected.Id, selected.Name);
            }
            else
            {
                historyControl = new StockMovementHistoryControl(warehouseId: _warehouseId, productName: "كافة حركات هذا المستودع");
            }

            var window = Window.GetWindow(this) as AdminDashboardWindow;
            window?.ShowOverlay(historyControl);
        }

        private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (_warehouseFromProducts == null || sender is not CheckBox cb)
                return;
            var select = cb.IsChecked == true;
            foreach (var p in _warehouseFromProducts)
                p.IsSelected = select;
        }

        private async void DeleteProductButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = _warehouseFromProducts?
                .Where(p => p.IsSelected)
                .ToList() ?? new List<WarehouseFromProduct>();

            if (selected.Count == 0 && ProductsDataGrid.SelectedItems.Count > 0)
            {
                selected = ProductsDataGrid.SelectedItems
                    .OfType<WarehouseFromProduct>()
                    .Distinct()
                    .ToList();
            }

            if (selected.Count == 0)
            {
                MessageBox.Show("الرجاء تحديد منتج واحد أو أكثر (مربع الاختيار أو تحديد الصفوف).");
                return;
            }

            var productsWithStock = selected.Where(p => p.StockQuantity > 0).ToList();
            if (productsWithStock.Any())
            {
                MessageBox.Show(
                    "لا يمكن حذف المنتجات المحددة لوجود كمية في المخزون لبعضها.\nيجب عمل مرتجع مشتريات أولاً لتصفير الكمية قبل التمكن من الحذف.", 
                    "تنبيه - منع الحذف", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
                return;
            }

            var namesPreview = string.Join("، ", selected.Take(5).Select(p => p.Name ?? $"#{p.Id}"));
            if (selected.Count > 5)
                namesPreview += " …";

            var result = MessageBox.Show(
                $"هل تريد حذف {selected.Count} منتج(ات)؟\n{namesPreview}",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                foreach (var product in selected)
                    await _warehouseService.DeleteProductFromWarehouse(product);
                
                ServiceLocator.NotifyInventoryChanged();
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"فشل الحذف: {ex.Message}");
            }
        }
    }
}
