using Microsoft.Extensions.DependencyInjection;


using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.ViewModels.Addons;
using POSSystem.WPF.ViewModels;

namespace POSSystem.WPF.Views.Popups
{
    public partial class AddAddonPopup : Window
    {
        private AddonManagementViewModel _viewModel = null!;
        private AddonManagementViewModel _parentViewModel = null!;
        private readonly IAddonService _addonService;

        public AddAddonPopup(AddonManagementViewModel parentViewModel)
        {
            try
            {
                InitializeComponent();
                _parentViewModel = parentViewModel;
                _addonService = POSSystem.WPF.App.ServiceProvider.GetRequiredService<IAddonService>();

                _viewModel = new AddonManagementViewModel(parentViewModel._orderItem);
                DataContext = _viewModel;

            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تهيئة نافذة إضافة الإضافة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }


        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AddonProberty.Quantity))
            {
                UpdateLowStockWarning();
            }
        }

        private void UpdateLowStockWarning()
        {
            if (_viewModel.AddonVM.Quantity <= 5)
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
            if (string.IsNullOrWhiteSpace(_viewModel.AddonVM.Name))
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
                CategoryComboBox1.Focus();
                return;
            }

            if (_viewModel.AddonVM.Price < 0)
            {
                MessageBox.Show("السعر لا يمكن أن يكون سالباً", "خطأ في التحقق",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PriceTextBox.Focus();
                return;
            }

            if (_viewModel.AddonVM.Quantity < 0)
            {
                MessageBox.Show("الكمية لا يمكن أن تكون سالبة", "خطأ في التحقق",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                QuantityTextBox.Focus();
                return;
            }

            try
            {
                // Create new addon
                var newAddon = new Addon
                {
                    Name = _viewModel.AddonVM.Name.Trim(),
                    Description = _viewModel.AddonVM.Description?.Trim(),
                    Price = _viewModel.AddonVM.Price,
                    Quantity = _viewModel.AddonVM.Quantity,
                    CategoryId = _viewModel.addonManagement.SelectedCategory.Id,
                    IsActive = false,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                // Add to database
                await _addonService.CreateAddonAsync(newAddon);

                MessageBox.Show("تم إضافة الإضافة بنجاح!", "تم الحفظ",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Refresh parent view
                _parentViewModel.RefreshCommand.Execute(null);

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء حفظ الإضافة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("هل أنت متأكد من إلغاء إضافة الإضافة؟",
                "تأكيد الإلغاء", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }

        private void CategoryComboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }

}