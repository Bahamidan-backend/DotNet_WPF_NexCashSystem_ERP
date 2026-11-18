using Microsoft.Extensions.DependencyInjection;


using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.ViewModels.Addons;
using POSSystem.WPF.ViewModels;

namespace POSSystem.WPF.Views.Popups
{
    public partial class EditAddonPopup : Window
    {
        private AddonManagementViewModel _viewModel = null!;
        private AddonManagementViewModel _parentViewModel = null!;
        private readonly IAddonService _addonService;
        private readonly ICategoryService _categoryService;
        private readonly Addon _originalAddon;

        public EditAddonPopup(AddonManagementViewModel parentViewModel, Addon addon)
        {
            try
            {
                InitializeComponent();
                _parentViewModel = parentViewModel;
                _originalAddon = addon;
                _addonService = POSSystem.WPF.App.ServiceProvider.GetRequiredService<IAddonService>();
                _categoryService = POSSystem.WPF.App.ServiceProvider.GetRequiredService<ICategoryService>();

                _viewModel = _parentViewModel;
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                DataContext = _viewModel;
                
                LoadAddonData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تهيئة نافذة تعديل الإضافة: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

      
        private void LoadAddonData()
        {
            _viewModel.EditAddonVM.Id = _originalAddon.Id;
            _viewModel.EditAddonVM.Name = _originalAddon.Name;
            _viewModel.EditAddonVM.Description = _originalAddon.Description ?? string.Empty;
            _viewModel.EditAddonVM.Price = _originalAddon.Price;
            _viewModel.EditAddonVM.Quantity = _originalAddon.Quantity;
            _viewModel.EditAddonVM.CreatedAt = _originalAddon.CreatedAt;
            _viewModel.EditAddonVM.UpdatedAt = _originalAddon.UpdatedAt;
            
            // Find and set the selected category
            foreach (var category in _viewModel.addonManagement.Categories)
            {
                if (category.Id == _originalAddon.CategoryId)
                {
                    _viewModel.addonManagement.SelectedCategory = category;
                    break;
                }
            }
            
            UpdateLowStockWarning();
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EditAddonProberty.Quantity))
            {
                UpdateLowStockWarning();
            }
        }

        private void UpdateLowStockWarning()
        {
            if (_viewModel.EditAddonVM.Quantity <= 5)
            {
                LowStockWarning.Visibility = Visibility.Visible;
            }
            else
            {
                LowStockWarning.Visibility = Visibility.Collapsed;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(_viewModel.EditAddonVM.Name))
            {
                MessageBox.Show("يرجى إدخال اسم الإضافة", "خطأ في التحقق", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            if (_viewModel.addonManagement.SelectedCategory == null)
            {
                MessageBox.Show("يرجى اختيار فئة للإضافة", "خطأ في التحقق", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CategoryComboBox.Focus();
                return;
            }

            if (_viewModel.EditAddonVM.Price < 0)
            {
                MessageBox.Show("السعر لا يمكن أن يكون سالباً", "خطأ في التحقق", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PriceTextBox.Focus();
                return;
            }

            if (_viewModel.EditAddonVM.Quantity < 0)
            {
                MessageBox.Show("الكمية لا يمكن أن تكون سالبة", "خطأ في التحقق", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                QuantityTextBox.Focus();
                return;
            }

            try
            {
                // Update addon
                var updatedAddon = new Addon
                {
                    Id = _originalAddon.Id,
                    Name = _viewModel.EditAddonVM.Name.Trim(),
                    Description = _viewModel.EditAddonVM.Description?.Trim(),
                    Price = _viewModel.EditAddonVM.Price,
                    Quantity = _viewModel.EditAddonVM.Quantity,
                    CategoryId = _viewModel.addonManagement.SelectedCategory.Id,
                    IsActive = _originalAddon.IsActive,
                    IsInHouse = _originalAddon.IsInHouse,
                    BaseAddonId = _originalAddon.BaseAddonId,
                    BaseAddonQuantity = _originalAddon.BaseAddonQuantity,
                    DeductOnSale = _originalAddon.DeductOnSale,
                    CreatedAt = _originalAddon.CreatedAt,
                    UpdatedAt = DateTime.Now
                };

                // Update in database
                await _addonService.UpdateAddonAsync(_viewModel.EditAddonVM.Id,updatedAddon);

                MessageBox.Show("تم تحديث الإضافة بنجاح!", "تم الحفظ", 
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Refresh parent view
                _parentViewModel.RefreshCommand.Execute(null);

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحديث الإضافة: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("هل أنت متأكد من إلغاء التعديلات؟", 
                "تأكيد الإلغاء", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }
    }

}
