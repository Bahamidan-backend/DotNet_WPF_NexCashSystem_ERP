using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using POSSystem.WPF.Business;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;
using POSSystem.WPF.ViewModels.Warehouses;
using static POSSystem.WPF.Models.Recipe;

namespace POSSystem.WPF.Views.Popups
{
    public partial class ProductFormInventoryControl : UserControl
    {
        private readonly WarehouseManagementViewModel _viewModel;
        private readonly WarehouseService _warehouseService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly int _warehouseId;
        private readonly WarehouseFromProduct? _editingProduct;
        public ObservableCollection<WarehouseFromProduct> _warehouseFromProducts;

        public ProductFormInventoryControl(int warehouseId, WarehouseFromProduct? editingProduct = null)
        {
            InitializeComponent();
            _warehouseId = warehouseId;
            _editingProduct = editingProduct;

            _unitOfWork = POSSystem.WPF.App.ServiceProvider.GetRequiredService<IUnitOfWork>();
            _warehouseService = new WarehouseService(_unitOfWork);
            _viewModel = new WarehouseManagementViewModel();
            _warehouseFromProducts = new ObservableCollection<WarehouseFromProduct>();

            DataContext = _viewModel;

            _ = LoadAvailableUnit();

            if (_editingProduct != null)
            {
                if (FormHeaderTitleText != null) FormHeaderTitleText.Text = "✏️ تعديل بيانات منتج المخزن";
                if (FormHeaderSubTitleText != null) FormHeaderSubTitleText.Text = "تعديل الاسم، وحدة القياس، والحد الأدنى للتنبيه";
                if (SaveButton != null) SaveButton.Content = "حفظ التعديلات";

                _viewModel.warehouseManagementProberty.Name = _editingProduct.Name ?? "";
                _viewModel.warehouseManagementProberty.Description = _editingProduct.Description ?? "";
                _viewModel.warehouseManagementProberty.MinimumStockLevel = _editingProduct.MinimumStockLevel;

                NameTextBox.Text = _editingProduct.Name ?? "";
                DescriptionTextBox.Text = _editingProduct.Description ?? "";
                MinimumStockTextBox.Text = _editingProduct.MinimumStockLevel.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

                if (_editingProduct.PortionUnit.HasValue)
                {
                    InvontryTypeComb2.SelectedValue = _editingProduct.PortionUnit.Value;
                }
            }
        }

        public async Task LoadAvailableUnit()
        {
            try
            {
                InvontryTypeComb2.ItemsSource = PortionUnitHelper.DisplayNames;
                InvontryTypeComb2.DisplayMemberPath = "Value";
                InvontryTypeComb2.SelectedValuePath = "Key";
                if (_editingProduct?.PortionUnit != null)
                {
                    InvontryTypeComb2.SelectedValue = _editingProduct.PortionUnit.Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل وحدات القياس: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReturnToWarehouseProducts()
        {
            var window = Window.GetWindow(this) as AdminDashboardWindow;
            if (window == null) return;

            window.CloseOverlay();
            window.Dispatcher.BeginInvoke(() =>
            {
                window.ShowOverlay(new WarehouseProductsControl(_warehouseId));
            }, DispatcherPriority.Background);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ReturnToWarehouseProducts();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string enteredName = NameTextBox.Text?.Trim() ?? _viewModel.warehouseManagementProberty.Name?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(enteredName))
            {
                MessageBox.Show("يرجى إدخال اسم المنتج", "خطأ في التحقق",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            if (InvontryTypeComb2.SelectedValue == null)
            {
                MessageBox.Show("يرجى اختيار وحدة قياس للمنتج", "خطأ في التحقق",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                InvontryTypeComb2.Focus();
                return;
            }

            try
            {
                var selectedUnit = (PortionUnit)InvontryTypeComb2.SelectedValue;

                decimal minLevel = 5m;
                if (decimal.TryParse(MinimumStockTextBox.Text?.Trim().Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedMin) && parsedMin >= 0)
                {
                    minLevel = parsedMin;
                }
                else if (_viewModel.warehouseManagementProberty.MinimumStockLevel > 0)
                {
                    minLevel = _viewModel.warehouseManagementProberty.MinimumStockLevel;
                }

                if (_editingProduct != null)
                {
                    var context = _unitOfWork.GetContext();
                    string oldName = _editingProduct.Name ?? "";

                    var dbItem = await context.warehouseFromProducts.FirstOrDefaultAsync(x => x.Id == _editingProduct.Id);
                    if (dbItem != null)
                    {
                        dbItem.Name = enteredName;
                        dbItem.Description = DescriptionTextBox.Text?.Trim() ?? _viewModel.warehouseManagementProberty.Description;
                        dbItem.PortionUnit = selectedUnit;
                        dbItem.MinimumStockLevel = minLevel;
                        dbItem.UpdatedAt = DateTime.Now;
                    }

                    // Keep linked Recipes in sync with the updated name and unit
                    var linkedRecipes = await context.Recipes
                        .Where(r => r.WarehouseFromProductId == _editingProduct.Id || (!string.IsNullOrEmpty(oldName) && r.Name == oldName))
                        .ToListAsync();
                    foreach (var r in linkedRecipes)
                    {
                        r.Name = enteredName;
                        r.WarehouseFromProductId = _editingProduct.Id;
                        r.portionUnit = selectedUnit;
                    }

                    await _unitOfWork.SaveChangesAsync();
                    _unitOfWork.ClearEntityTracking();
                    ServiceLocator.NotifyInventoryChanged();
                    MessageBox.Show("تم تحديث بيانات منتج المخزن بنجاح.", "تم التحديث", MessageBoxButton.OK, MessageBoxImage.Information);
                    ReturnToWarehouseProducts();
                    return;
                }

                var newProduct = new WarehouseFromProduct
                {
                    Name = enteredName,
                    Description = DescriptionTextBox.Text?.Trim() ?? _viewModel.warehouseManagementProberty.Description,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    WarehouseId = _warehouseId,
                    PortionUnit = selectedUnit,
                    MinimumStockLevel = minLevel
                };

                var createdProduct = await _warehouseService.CreateProductFromWarehouse(newProduct);
                _warehouseFromProducts.Add(createdProduct);

                ServiceLocator.NotifyInventoryChanged();
                ReturnToWarehouseProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء حفظ المنتج: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
