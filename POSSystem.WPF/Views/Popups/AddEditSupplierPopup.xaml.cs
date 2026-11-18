using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using POSSystem.WPF.Models;
using POSSystem.WPF.Business;

namespace POSSystem.WPF.Views.Popups
{
    public partial class AddEditSupplierPopup : Window
    {
        private readonly Supplier? _supplier;
        private readonly bool _isEdit;

        public AddEditSupplierPopup(Supplier? supplier = null)
        {
            InitializeComponent();
            _supplier = supplier;
            _isEdit = supplier != null;

            if (_isEdit && supplier != null)
            {
                TitleTextBlock.Text = "✏️ تعديل بيانات المورد";
                NameTextBox.Text = supplier.Name;
                VatTextBox.Text = supplier.VatNumber;
                PhoneTextBox.Text = supplier.PhoneNumber;
                AddressTextBox.Text = supplier.Address;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("اسم المورد مطلوب", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var supplierService = POSSystem.WPF.App.ServiceProvider.GetRequiredService<ISupplierService>();
                
                if (_isEdit && _supplier != null)
                {
                    // Store old values to revert if save fails
                    var oldName = _supplier.Name;
                    var oldVat = _supplier.VatNumber;
                    var oldPhone = _supplier.PhoneNumber;
                    var oldAddress = _supplier.Address;
                    var oldUpdatedAt = _supplier.UpdatedAt;

                    try
                    {
                        _supplier.Name = NameTextBox.Text;
                        _supplier.VatNumber = VatTextBox.Text;
                        _supplier.PhoneNumber = PhoneTextBox.Text;
                        _supplier.Address = AddressTextBox.Text;
                        _supplier.UpdatedAt = DateTime.Now;

                        await supplierService.UpdateSupplierAsync(_supplier);
                    }
                    catch
                    {
                        // Revert values on failure
                        _supplier.Name = oldName;
                        _supplier.VatNumber = oldVat;
                        _supplier.PhoneNumber = oldPhone;
                        _supplier.Address = oldAddress;
                        _supplier.UpdatedAt = oldUpdatedAt;
                        throw; // Re-throw to be caught by the outer catch
                    }
                }
                else
                {
                    var newSupplier = new Supplier
                    {
                        Name = NameTextBox.Text,
                        VatNumber = VatTextBox.Text,
                        PhoneNumber = PhoneTextBox.Text,
                        Address = AddressTextBox.Text,
                        TotalBalance = 0,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    await supplierService.CreateSupplierAsync(newSupplier);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                if (message.Contains("UNIQUE") || message.Contains("unique") || message.Contains("constraint"))
                {
                    message = "اسم المورد موجود مسبقاً، يرجى اختيار اسم آخر.";
                }
                MessageBox.Show($"حدث خطأ أثناء الحفظ: {message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
