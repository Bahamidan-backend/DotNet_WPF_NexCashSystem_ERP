using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Views.Popups
{
    public partial class AddKitchenPopup : UserControl
    {
        private readonly Action _onSaved;
        private readonly Action _onCancel;

        public AddKitchenPopup(Action onSaved, Action onCancel)
        {
            InitializeComponent();
            _onSaved = onSaved;
            _onCancel = onCancel;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = NameTextBox.Text.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("يرجى إدخال اسم المطبخ", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SaveButton.IsEnabled = false;

                var kitchen = new Kitchen
                {
                    Name = name,
                    PrinterName = PrinterNameTextBox.Text.Trim(),
                    IsPrinterEnabled = IsPrinterEnabledCheckBox.IsChecked ?? false,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                await POSSystem.WPF.App.ServiceProvider.GetRequiredService<IKitchenService>().CreateKitchenAsync(kitchen);
                
                MessageBox.Show("تم إضافة المطبخ بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                _onSaved?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء الحفظ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                SaveButton.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _onCancel?.Invoke();
        }
    }
}
