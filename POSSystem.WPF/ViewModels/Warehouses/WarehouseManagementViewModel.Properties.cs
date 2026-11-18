using POSSystem.WPF.Models;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.ViewModels.Products;

namespace POSSystem.WPF.ViewModels.Warehouses
{
    public partial class WarehouseManagementViewModel
    {
        private readonly IWarehouseService _warehouseService;
        private readonly IProductService _productService;
        private Warehouse? _selectedWarehouse;
        private IUnitOfWork? _unitOfWork;
        private POSSystem.WPF.Business.Services.interfase.ICategoryService? _categoryService;
  
        public WarehouseCommands Commands { get; }
        public ProductsViewModel productsViewModel { get; set; }
        public WarehouseManagementProberty warehouseManagementProberty { get; }

        public Warehouse? SelectedWarehouse
        {
            get => _selectedWarehouse;
            set
            {
                if (SetProperty(ref _selectedWarehouse, value))
                {
                    ((RelayCommand)Commands.EditWarehouseCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)Commands.DeleteWarehouseCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)Commands.ViewProductsCommand).RaiseCanExecuteChanged();
                }
            }
        }
    }
}
