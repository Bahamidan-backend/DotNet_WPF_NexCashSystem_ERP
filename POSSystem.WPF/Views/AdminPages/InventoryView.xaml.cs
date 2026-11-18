using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using POSSystem.WPF.Data;
using POSSystem.WPF.Models;
using POSSystem.WPF.ViewModels.Warehouses;

namespace POSSystem.WPF.Views.AdminPages
{
    public partial class InventoryView : UserControl
    {
        private WarehouseManagementViewModel _warehouseViewModel;
        private POSDbContext _db;
        private readonly User _currentUser;

        public ObservableCollection<Warehouse> Warehouses { get; set; }

        public InventoryView()
        {
            InitializeComponent();
            _warehouseViewModel = new WarehouseManagementViewModel();
            DataContext = _warehouseViewModel;

        
            
            
            
            // Load initial data after UI is loaded
            Loaded += InventoryView_Loaded;

        }
        
        private async void InventoryView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Load data after a short delay to ensure database is ready
            await System.Threading.Tasks.Task.Delay(500);
            if (_warehouseViewModel != null)
            {
                _warehouseViewModel.Commands.LoadLowStockProductsCommand.Execute(null);
            }
        }
        
        private void TabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.Source is System.Windows.Controls.TabControl tabControl && _warehouseViewModel != null)
            {
                if (tabControl.SelectedItem is System.Windows.Controls.TabItem selectedTab)
                {
                    if (selectedTab.Name == "LowStockTab")
                    {
                        // Load low stock products when tab is selected
                        _warehouseViewModel.Commands.LoadLowStockProductsCommand.Execute(null);
                    }
                }
            }
        }






        private void OnAddWarehouseRequested()
        {
            var addWarehouseControl = new POSSystem.WPF.Views.Popups.AddEditWarehouseControl(_warehouseViewModel);
            var adminWindow = Window.GetWindow(this) as AdminDashboardWindow;
            adminWindow?.ShowOverlay(addWarehouseControl);
        }

        





        private void OnEditWarehouseRequested(Warehouse warehouse)
        {
            var editWarehouseControl = new POSSystem.WPF.Views.Popups.AddEditWarehouseControl(_warehouseViewModel, warehouse);
            var adminWindow = Window.GetWindow(this) as AdminDashboardWindow;
             adminWindow?.ShowOverlay(editWarehouseControl);
        }

        private void OnViewProductsRequested(int ID)
        {
             var warehouseProductsControl = new POSSystem.WPF.Views.Popups.WarehouseProductsControl(ID);
             var adminWindow = Window.GetWindow(this) as AdminDashboardWindow;
             adminWindow?.ShowOverlay(warehouseProductsControl);
        }

        // Button Click Handlers
        private void AddWarehouse_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OnAddWarehouseRequested();
        }

        private void TransferStock_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var transferControl = new POSSystem.WPF.Views.Popups.TransferStockControl();
            var adminWindow = Window.GetWindow(this) as AdminDashboardWindow;
            adminWindow?.ShowOverlay(transferControl);
        }


        private void ViewProducts_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // For demo purposes, we'll use the first warehouse
           
        }

        private void Refresh_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("تم تحديث البيانات!", "تحديث", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        // Warehouse Card Button Handlers
        private async void ViewWarehouse_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag != null)
            {
                if (int.TryParse(button.Tag.ToString(), out int warehouseType))
                {
                    OnViewProductsRequested(warehouseType);
                }
                else if (button.Tag is int tagInt)
                {
                    OnViewProductsRequested(tagInt);
                }
            }
        }

        private async void EditWarehouse_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag != null)
            {
                int warehouseType = 0;
                if (button.Tag is int tagInt)
                    warehouseType = tagInt;
                else
                    int.TryParse(button.Tag.ToString(), out warehouseType);

                var warehouse = await GetWarehouseByType(warehouseType);
                if (warehouse != null)
                {
                    OnEditWarehouseRequested(warehouse);
                }
            }
        }

        private async void DeleteWarehouse_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //var button = sender as System.Windows.Controls.Button;
            if (sender is Button button && button.Tag is Warehouse warehouse)
            {
                var warehouseType = warehouse.Name?.ToString();

                var result = System.Windows.MessageBox.Show(
             $"هل أنت متأكد من حذف مخزن {warehouseType}؟",
             "تأكيد الحذف",
             System.Windows.MessageBoxButton.YesNo,
             System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    try
                    {
                        bool success = await _warehouseViewModel.Deletewarehouse(warehouse);
                        if (success)
                        {
                            System.Windows.MessageBox.Show($"تم حذف المستودع {warehouseType} بنجاح!", "نجاح",
                                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = ex.InnerException?.Message ?? ex.Message;
                        System.Windows.MessageBox.Show($"خطأ في الحذف: {errorMessage}", "خطأ",
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }

            }

         
        }

        private async Task< Warehouse> GetWarehouseByType(int warehouseType)
        {
            return await  _warehouseViewModel.GetWarehouseByType(warehouseType);

        }

        private void PrintWarehousesReport_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var warehouses = _warehouseViewModel.warehouseManagementProberty.Warehouses.ToList();
                var columnHeaders = new[] { "اسم المنتج", "الكمية", "الوحدة", "سعر الشراء", "الإجمالي", "الحالة" };
                
                Services.PrintService.PrintGroupedReport(
                    "تقرير جرد المخازن التفصيلي والقيمة",
                    "Detailed Inventory & Stock Value Report",
                    warehouses,
                    (object g) => {
                        if (g == null) return "";
                        var w = g as Warehouse;
                        if (w == null) return "";
                        decimal totalVal = w.warehouseFromProducts?.Sum(p => p.Price * p.StockQuantity) ?? 0;
                        return $"🏬 مخزن: {w.Name} - {w.Location} (إجمالي القيمة: {totalVal:#,##0} ريال)";
                    },
                    (object g) => {
                        var w = g as Warehouse;
                        return w?.warehouseFromProducts ?? Enumerable.Empty<WarehouseFromProduct>();
                    },
                    columnHeaders,
                    (object item) => 
                    {
                        if (item == null) return new string[columnHeaders.Length];
                        var product = item as WarehouseFromProduct;
                        if (product == null) return new string[columnHeaders.Length];
                        return new[]
                        {
                            product.Name ?? "",
                            product.StockQuantity.ToString("#,##0"),
                            product.PortionUnit?.ToString() ?? "حبة",
                            product.Price.ToString("#,##0") + " ريال",
                            (product.Price * product.StockQuantity).ToString("#,##0") + " ريال",
                            product.StockQuantity switch {
                                0 => "نفذ المخزون",
                                <= 10 => "مخزون منخفض",
                                _ => "متوفر"
                            }
                        };
                    },
                    $"إجمالي عدد المخازن: {warehouses.Count}\nإجمالي القيمة الكلية للمخزون: {warehouses.Sum(w => w.warehouseFromProducts?.Sum(p => p.Price * p.StockQuantity) ?? 0):#,##0} ريال",
                    preferPdf: true
                );
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ في حفظ التقرير: {ex.Message}", "خطأ", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }


        private void PrintLowStockReport_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (_warehouseViewModel == null) return;
                
                var products = _warehouseViewModel.warehouseManagementProberty.LowStockProducts?.ToList() ?? new List<WarehouseFromProduct>();
                var columnHeaders = new[] { "اسم المنتج", "الكمية المتاحة", "حالة المخزون" };
                
                Services.PrintService.PrintReport(
                    "تقرير المنتجات منخفضة المخزون",
                    "Low Stock Products Report",
                    products,
                    columnHeaders,
                    (object item) => 
                    {
                        if (item == null) return new string[columnHeaders.Length];
                        var product = item as WarehouseFromProduct;
                        if (product == null) return new string[columnHeaders.Length];
                        return new[]
                        {
                            product.Name ?? "",
                            product.StockQuantity.ToString(),
                            product.StockStatus ?? ""
                        };
                    },
                    $"إجمالي المنتجات منخفضة المخزون: {products.Count}",
                    preferPdf: true
                );
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ في حفظ التقرير: {ex.Message}", "خطأ", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
