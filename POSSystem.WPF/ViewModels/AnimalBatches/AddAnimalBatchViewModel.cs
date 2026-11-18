using POSSystem.WPF.Helpers;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.ViewModels.AnimalBatches
{
    public partial class AddAnimalBatchViewModel : BaseViewModel
    {
        public AddAnimalBatchViewModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            Warehouses = new ObservableCollection<Warehouse>();
            Suppliers = new ObservableCollection<Supplier>();
            
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);

            LoadInitialDataAsync().SafeFireAndForget();
        }

        private async Task LoadInitialDataAsync()
        {
            try
            {
                var warehouses = await _unitOfWork.Warehouses.GetAllAsync();
                var suppliers = await _unitOfWork.Suppliers.GetAllAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Warehouses.Clear();
                    foreach (var warehouse in warehouses)
                    {
                        Warehouses.Add(warehouse);
                    }

                    Suppliers.Clear();
                    foreach (var supplier in suppliers)
                    {
                        Suppliers.Add(supplier);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading animal batch data: {ex.Message}");
            }
        }

        private async Task LoadProductsForWarehouseAsync(int warehouseId)
        {
            var allProducts = await _unitOfWork.warehouseFromProduct.GetAllAsync();
            var warehouseProducts = allProducts.Where(p => p.WarehouseId == warehouseId).ToList();
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                Products.Clear();
                foreach (var p in warehouseProducts)
                {
                    Products.Add(p);
                }
                SelectedProduct = Products.FirstOrDefault();
            });
        }
    }
}
