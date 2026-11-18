using Microsoft.Extensions.DependencyInjection;
using POSSystem.WPF.Models;
using POSSystem.WPF.ViewModels.Categories;
using POSSystem.WPF.ViewModels.Products;
using POSSystem.WPF.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace POSSystem.WPF.Views.Popups
{
    public partial class ManageCategoriesControl : UserControl
    {
        private ManageCategoriesViewModel _viewModel;

        public ManageCategoriesControl()
        {
            InitializeComponent();
            _viewModel = new ManageCategoriesViewModel();
            DataContext = _viewModel;
        }

        public void Initialize(ProductsViewModel productsVM)
        {
            _viewModel.productsViewModel = productsVM;
        }

        private void AddKitchenButton_Click(object sender, RoutedEventArgs e)
        {
            QuickAddKitchenBorder.Visibility = Visibility.Visible;
            QuickKitchenNameTextBox.Focus();
        }

        private void CancelQuickAddKitchen_Click(object sender, RoutedEventArgs e)
        {
            QuickAddKitchenBorder.Visibility = Visibility.Collapsed;
            QuickKitchenNameTextBox.Text = string.Empty;
            QuickPrinterNameTextBox.Text = string.Empty;
        }

        private async void SaveQuickAddKitchen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = QuickKitchenNameTextBox.Text.Trim();
                string printerName = QuickPrinterNameTextBox.Text.Trim();

                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("يرجى إدخال اسم المطبخ", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    QuickKitchenNameTextBox.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(printerName))
                {
                    MessageBox.Show("يرجى إدخال اسم الطابعة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    QuickPrinterNameTextBox.Focus();
                    return;
                }

                var kitchen = new Kitchen
                {
                    Name = name,
                    PrinterName = printerName,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                await POSSystem.WPF.App.ServiceProvider.GetRequiredService<IKitchenService>().CreateKitchenAsync(kitchen);
                await _viewModel.LoadKitchensAsync();
                
                // Select the new kitchen
                _viewModel.SelectedKitchen = _viewModel.AvailableKitchens.FirstOrDefault(k => k.Name == name);
                
                QuickAddKitchenBorder.Visibility = Visibility.Collapsed;
                QuickKitchenNameTextBox.Text = string.Empty;
                QuickPrinterNameTextBox.Text = string.Empty;
                
                MessageBox.Show("تم إضافة المطبخ بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء إضافة المطبخ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddUpdateCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _viewModel.SaveCategoryAsync();
                CloseButton_Click(null, null);

                if (_viewModel.IsEditing)
                {
                    AddUpdateButton.Content = "➕ إضافة فئة";
                    CancelButton.Visibility = Visibility.Collapsed;
                    _viewModel.IsEditing = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Category category)
            {
                _viewModel.EditCategory(category);
                AddUpdateButton.Content = "💾 حفظ التعديلات";
                CancelButton.Visibility = Visibility.Visible;
                CategoryNameTextBox.Focus();
            }
        }

        private void CancelEditButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ClearForm();
            AddUpdateButton.Content = "➕ إضافة فئة";
            CancelButton.Visibility = Visibility.Collapsed;
        }

        private async void DeleteCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Category category)
            {
                var result = MessageBox.Show($"هل أنت متأكد من حذف الفئة '{category.Name}'؟",
                    "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    await _viewModel.DeleteCategoryAsync(category);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window is AdminDashboardWindow adminWindow)
            {
                adminWindow.CloseOverlay();
            }
            else
            {
                window?.Close();
            }
        }
    }
}
