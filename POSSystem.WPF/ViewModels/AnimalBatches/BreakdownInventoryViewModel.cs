using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.ViewModels.AnimalBatches
{
    public partial class BreakdownInventoryViewModel : BaseViewModel
    {
        public BreakdownInventoryViewModel(IAnimalBatchService batchService, IUnitOfWork unitOfWork)
        {
            _batchService = batchService;
            _unitOfWork = unitOfWork;
            
            Warehouses = new ObservableCollection<Warehouse>();
            AvailableProducts = new ObservableCollection<WarehouseFromProduct>();
            Suppliers = new ObservableCollection<Supplier>();
            
            CancelCommand = new RelayCommand(CloseWindow);
            ProcessCommand = new RelayCommand(async () => await ProcessBreakdownAsync());
            
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                var warehouses = await _unitOfWork.Warehouses.GetAllAsync();
                foreach (var w in warehouses) Warehouses.Add(w);

                var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
                foreach (var s in suppliers) Suppliers.Add(s);

                if (Warehouses.Any()) 
                {
                    SelectedWarehouseId = Warehouses.First().Id;
                    SelectedTargetWarehouseId = Warehouses.Last().Id;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
            }
        }

        private async void LoadProducts()
        {
            AvailableProducts.Clear();
            if (SelectedWarehouseId <= 0) return;

            try
            {
                var products = await _unitOfWork.warehouseFromProduct.FindAsync(p => p.WarehouseId == SelectedWarehouseId && p.IsActive);
                foreach (var p in products) 
                {
                    AvailableProducts.Add(p);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading products: {ex.Message}");
            }
        }

        private void UpdateStockText()
        {
            var product = AvailableProducts.FirstOrDefault(p => p.Id == SelectedProductId);
            if (product != null)
            {
                SelectedProductStockText = $"الرصيد المتوفر في المخزن: {product.StockQuantity}";
            }
            else
            {
                SelectedProductStockText = "الرصيد المتوفر: 0";
            }
        }

        private async Task ProcessBreakdownAsync()
        {
            if (SelectedProductId <= 0 || SelectedWarehouseId <= 0 || SelectedTargetWarehouseId <= 0)
            {
                MessageBox.Show("يرجى اختيار المستودع الأساسي والهدف والمنتج", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Count <= 0)
            {
                MessageBox.Show("يرجى إدخال عدد صحيح", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int defaultCategory = 1;
                var success = await _batchService.BreakdownCarcassesFromInventoryAsync(
                    SelectedWarehouseId, 
                    SelectedProductId, 
                    SelectedTargetWarehouseId, 
                    Count, 
                    IsWeightMode, 
                    PortionsCount, 
                    AverageWeight,
                    AverageLiverWeight,
                    AverageBackWeight,
                    defaultCategory,
                    SelectedSupplierId);
                
                if (success)
                {
                    MessageBox.Show("تم تحويل وفك الذبائح بنجاح وتوجيهها للمخزن الهدف!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    CloseWindow();
                }
                else
                {
                    MessageBox.Show("فشل التحويل. قد يكون الرصيد غير كافٍ.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء المعالجة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseWindow()
        {
            var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
            window?.Close();
        }
    }
}
