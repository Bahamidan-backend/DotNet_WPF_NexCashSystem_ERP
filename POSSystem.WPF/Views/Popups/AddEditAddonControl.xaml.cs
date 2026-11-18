using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Controls;
using POSSystem.WPF.Models;
using POSSystem.WPF.ViewModels;
using POSSystem.WPF.ViewModels.Addons;
using POSSystem.WPF.Views.AdminPages;
using POSSystem.WPF.Business.Services.interfase;

namespace POSSystem.WPF.Views.Popups
{
    public partial class AddEditAddonControl : UserControl
    {
        private AddonManagementViewModel _parentViewModel;
        private Addon? _addon;
        private bool _isEditMode;
        private readonly IAddonService _addonService;

        public AddEditAddonControl(AddonManagementViewModel parentViewModel, Addon? addon = null)
        {
            InitializeComponent();
            _parentViewModel = parentViewModel;
            _addon = addon;
            _isEditMode = addon != null;
            _addonService = POSSystem.WPF.App.ServiceProvider.GetRequiredService<IAddonService>();


            LoadBaseAddonsAsync();
            LoadCategoriesAsync();

            if (_isEditMode)
            {
                SetupEditMode();
            }
            else
            {
                SetupAddMode();
            }
        }

        private async void LoadBaseAddonsAsync()
        {
            try
            {
                var addons = await _addonService.GetAllAddonsAsync();
                if (addons == null || !addons.Any())
                {
                    BaseAddonComboBox.ItemsSource = new List<Addon> { new Addon { Name = "(لا توجد إضافات متاحة)", Id = -1 } };
                    BaseAddonComboBox.IsEnabled = false;
                    return;
                }

                // Don't include the current addon in the list of potential base addons to avoid circular references
                var availableAddons = addons.Where(a => _addon == null || a.Id != _addon.Id).ToList();
                
                Dispatcher.Invoke(() => {
                    if (!availableAddons.Any())
                    {
                        BaseAddonComboBox.ItemsSource = new List<Addon> { new Addon { Name = "(لا توجد إضافات أخرى متاحة)", Id = -1 } };
                    }
                    else
                    {
                        BaseAddonComboBox.ItemsSource = availableAddons;
                        BaseAddonComboBox.IsEnabled = true;
                    }

                    // If in edit mode, select the correct base addon
                    if (_isEditMode && _addon != null && _addon.BaseAddonId.HasValue)
                    {
                        BaseAddonComboBox.SelectedValue = _addon.BaseAddonId.Value;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading base addons: {ex.Message}");
            }
        }


        private void SetupAddMode()
        {
            HeaderText.Text = "إضافة إضافة جديدة";
            HeaderSubText.Text = "Add New Addon";
            SaveButton.Content = "حفظ البيانات";
            StatusInfoPanel.Visibility = Visibility.Collapsed;
        }

        private void SetupEditMode()
        {
            HeaderText.Text = "تعديل الإضافة";
            HeaderSubText.Text = "Edit Addon";
            SaveButton.Content = "حفظ التعديلات";
            StatusInfoPanel.Visibility = Visibility.Visible;

            if (_addon != null)
            {
                NameTextBox.Text = _addon.Name;
                DescriptionTextBox.Text = _addon.Description;
                PriceTextBox.Text = _addon.Price.ToString();
                BuyingPriceTextBox.Text = _addon.BuyingPrice.ToString("0.##");
                IsInHouseCheckBox.IsChecked = _addon.IsInHouse;
                QuantityTextBox.Text = _addon.Quantity.ToString("0.##");
                
                BaseAddonQuantityTextBox.Text = _addon.BaseAddonQuantity.ToString();
                DeductOnSaleCheckBox.IsChecked = _addon.DeductOnSale;


                if (_addon.CategoryId.HasValue)
                {
                    // Select category in UI (this requires categories to be loaded)
                    SelectCategory(_addon.CategoryId.Value);
                }

                if (_addon.CreatedAt != DateTime.MinValue)
                    CreatedAtText.Text = _addon.CreatedAt.ToString("yyyy/MM/dd");
                
                if (_addon.UpdatedAt != DateTime.MinValue)
                    UpdatedAtText.Text = _addon.UpdatedAt.ToString("yyyy/MM/dd");
            }
        }

        private async void LoadCategoriesAsync()
        {
            try
            {
                var categories = await POSSystem.WPF.App.ServiceProvider.GetRequiredService<ICategoryService>().GetAllCategoriesAsync();
                Dispatcher.Invoke(() => {
                    CategoryComboBox.ItemsSource = categories;
                    if (_isEditMode && _addon != null && _addon.CategoryId.HasValue)
                    {
                        SelectCategory(_addon.CategoryId.Value);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
            }
        }

        private void SelectCategory(int categoryId)
        {
            foreach (var item in CategoryComboBox.Items)
            {
                if (item is Category cat && cat.Id == categoryId)
                {
                    CategoryComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                if (_isEditMode && _addon != null)
                {
                    // Update
                    _addon.Name = NameTextBox.Text.Trim();
                    _addon.Description = DescriptionTextBox.Text.Trim();
                    _addon.Price = decimal.TryParse(PriceTextBox.Text, out decimal price) ? price : 0;
                    _addon.IsInHouse = IsInHouseCheckBox.IsChecked ?? false;
                    
                    // FIX: Parse quantity if possible, only default to 0 if parsing fails AND it's a new addon.
                    // If parsing fails during edit, keep original quantity.
                    if (decimal.TryParse(QuantityTextBox.Text, out decimal updatedQty))
                    {
                        _addon.Quantity = updatedQty;
                    }
                    
                    _addon.CategoryId = (CategoryComboBox.SelectedItem as Category)?.Id ?? _addon.CategoryId;
                    _addon.BaseAddonId = (BaseAddonComboBox.SelectedItem as Addon)?.Id;
                    _addon.BaseAddonQuantity = decimal.TryParse(BaseAddonQuantityTextBox.Text, out decimal bQty) ? bQty : 0;
                    _addon.DeductOnSale = DeductOnSaleCheckBox.IsChecked ?? true;
                    // BuyingPrice is updated by PurchaseService, but persisted here if loaded from UI
                    _addon.BuyingPrice = decimal.TryParse(BuyingPriceTextBox.Text, out decimal buyPrice) ? buyPrice : _addon.BuyingPrice;
                    _addon.UpdatedAt = DateTime.Now;

                    await _addonService.UpdateAddonAsync(_addon.Id, _addon);
                    
                    MessageBox.Show("تم تعديل الإضافة بنجاح!", "تم التعديل", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Create
                    var newAddon = new Addon
                    {
                        Name = NameTextBox.Text.Trim(),
                        Description = DescriptionTextBox.Text.Trim(),
                        Price = decimal.TryParse(PriceTextBox.Text, out decimal nPrice) ? nPrice : 0,
                        IsInHouse = IsInHouseCheckBox.IsChecked ?? false,
                        Quantity = decimal.TryParse(QuantityTextBox.Text, out decimal nQty) ? nQty : 0,
                        CategoryId = (CategoryComboBox.SelectedItem as Category)?.Id,
                        BaseAddonId = (BaseAddonComboBox.SelectedItem as Addon)?.Id,
                        BaseAddonQuantity = decimal.TryParse(BaseAddonQuantityTextBox.Text, out decimal nbQty) ? nbQty : 0,
                        IsActive = true, // Default to true
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        DeductOnSale = DeductOnSaleCheckBox.IsChecked ?? true
                    };

                    await _addonService.CreateAddonAsync(newAddon);
                    
                    MessageBox.Show("تم إضافة الإضافة بنجاح!", "تم الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Refresh Parent
                _parentViewModel.RefreshCommand.Execute(null);

                CloseOverlay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("يرجى إدخال اسم الإضافة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return false;
            }


            if (!decimal.TryParse(PriceTextBox.Text, out decimal price) || price < 0)
            {
                MessageBox.Show("يرجى إدخال سعر صحيح", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                PriceTextBox.Focus();
                return false;
            }

            if (IsInHouseCheckBox.IsChecked == true)
            {
                if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity < 0)
                {
                    MessageBox.Show("يرجى إدخال كمية صحيحة كأرقام صحيحة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    QuantityTextBox.Focus();
                    return false;
                }
            }

            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CloseOverlay();
        }

        private void CloseOverlay()
        {
            var window = Window.GetWindow(this) as AdminDashboardWindow;
             window?.CloseOverlay();
        }
        private void BaseAddonComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isEditMode && BaseAddonComboBox.SelectedItem != null)
            {
                // Default to false (Batch Production mode) to match user's workflow
                DeductOnSaleCheckBox.IsChecked = false;
            }
            UpdateDeductionEstimation();
        }

        private void DeductionInputs_Changed(object sender, RoutedEventArgs e)
        {
            UpdateDeductionEstimation();
        }

        private void UpdateDeductionEstimation()
        {
            if (DeductionEstimationText == null || BaseAddonComboBox == null || QuantityTextBox == null || BaseAddonQuantityTextBox == null) return;

            var selectedBase = BaseAddonComboBox.SelectedItem as Addon;
            if (selectedBase == null)
            {
                DeductionEstimationText.Visibility = Visibility.Collapsed;
                return;
            }

            if (decimal.TryParse(QuantityTextBox.Text, out decimal newQty) && 
                decimal.TryParse(BaseAddonQuantityTextBox.Text, out decimal perUnitOrTotalDeduction))
            {
                decimal totalToDeductNow = 0;
                bool deductOnSale = DeductOnSaleCheckBox.IsChecked ?? true;

                if (!deductOnSale)
                {
                    totalToDeductNow = perUnitOrTotalDeduction;
                    if (totalToDeductNow > 0)
                    {
                        DeductionEstimationText.Text = $"⚠️ سيتم خصم إجمالي {totalToDeductNow:0.##} من {selectedBase.Name} الآن.";
                        DeductionEstimationText.Foreground = System.Windows.Media.Brushes.OrangeRed;
                        DeductionEstimationText.Visibility = Visibility.Visible;
                    }
                    else DeductionEstimationText.Visibility = Visibility.Collapsed;
                }
                else
                {
                    if (perUnitOrTotalDeduction > 0)
                    {
                        DeductionEstimationText.Text = $"ℹ️ سيتم خصم {perUnitOrTotalDeduction:0.##} من {selectedBase.Name} عند بيع كل حبة.";
                        DeductionEstimationText.Foreground = System.Windows.Media.Brushes.DeepSkyBlue;
                        DeductionEstimationText.Visibility = Visibility.Visible;
                    }
                    else DeductionEstimationText.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                DeductionEstimationText.Visibility = Visibility.Collapsed;
            }
        }
    }
}
