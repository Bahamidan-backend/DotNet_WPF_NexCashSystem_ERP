using System;
using System.Windows;
using System.Windows.Controls;
using POSSystem.WPF.Models;
using POSSystem.WPF.ViewModels;
using POSSystem.WPF.ViewModels.Discounts;

namespace POSSystem.WPF.Views.Popups
{
    public partial class AddEditDiscountControl : UserControl
    {
        private DiscountsViewModel _parentViewModel;
        private Discount _discount;
        private bool _isEditMode;

        public AddEditDiscountControl(DiscountsViewModel parentViewModel, Discount? discount = null)
        {
            InitializeComponent();
            _parentViewModel = parentViewModel;

            if (discount != null)
            {
                _isEditMode = true;
                _discount = discount;
                LoadDiscountData();
                HeaderText.Text = "تعديل الخصم";
                HeaderSubText.Text = "Edit Discount";
                SaveButton.Content = "حفظ التعديلات";
            }
            else
            {
                _isEditMode = false;
                _discount = new Discount
                {
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(30),
                    IsActive = true
                };
                StartDatePicker.SelectedDate = _discount.StartDate;
                EndDatePicker.SelectedDate = _discount.EndDate;
            }
        }

        private void LoadDiscountData()
        {
            NameTextBox.Text = _discount.Name;
            CodeTextBox.Text = _discount.Code;
            DescriptionTextBox.Text = _discount.Description;
            PercentageTextBox.Text = _discount.Percentage > 0 ? _discount.Percentage.ToString("0.##") : "";
            AmountTextBox.Text = _discount.Amount > 0 ? _discount.Amount.ToString("0.##") : "";
            StartDatePicker.SelectedDate = _discount.StartDate;
            EndDatePicker.SelectedDate = _discount.EndDate;
            IsActiveCheckBox.IsChecked = _discount.IsActive;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                try
                {
                    _discount.Name = NameTextBox.Text.Trim();
                    _discount.Code = CodeTextBox.Text.Trim();
                    _discount.Description = DescriptionTextBox.Text.Trim();
                    
                    decimal percentage = 0;
                    decimal amount = 0;
                    decimal.TryParse(PercentageTextBox.Text, out percentage);
                    decimal.TryParse(AmountTextBox.Text, out amount);
                    
                    _discount.Percentage = percentage;
                    _discount.Amount = amount;
                    
                    _discount.StartDate = StartDatePicker.SelectedDate ?? DateTime.Now;
                    _discount.EndDate = EndDatePicker.SelectedDate ?? DateTime.Now;
                    _discount.IsActive = IsActiveCheckBox.IsChecked ?? false;

                    if (!_isEditMode)
                    {
                        _discount.CreatedAt = DateTime.Now;
                        _ = _parentViewModel.AddDiscount(_discount);
                        MessageBox.Show("تم إضافة الخصم بنجاح!", "تم الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        _discount.UpdatedAt = DateTime.Now;
                        _parentViewModel.UpdateDiscount(_discount);
                        MessageBox.Show("تم تحديث الخصم بنجاح!", "تم التحديث", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    CloseOverlay();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"حدث خطأ أثناء الحفظ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("يرجى إدخال اسم المناسبة", "خطأ في التحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(CodeTextBox.Text))
            {
                MessageBox.Show("يرجى إدخال كود الخصم", "خطأ في التحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
                CodeTextBox.Focus();
                return false;
            }

            if (!StartDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("يرجى اختيار تاريخ البداية", "خطأ في التحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
                StartDatePicker.Focus();
                return false;
            }

            if (!EndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("يرجى اختيار تاريخ النهاية", "خطأ في التحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
                EndDatePicker.Focus();
                return false;
            }

            if (StartDatePicker.SelectedDate > EndDatePicker.SelectedDate)
            {
                MessageBox.Show("تاريخ البداية يجب أن يكون قبل تاريخ النهاية", "خطأ في التحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
                StartDatePicker.Focus();
                return false;
            }
            
            decimal percentage = 0;
            decimal amount = 0;
            bool hasPercentage = decimal.TryParse(PercentageTextBox.Text, out percentage) && percentage > 0;
            bool hasAmount = decimal.TryParse(AmountTextBox.Text, out amount) && amount > 0;

            if (!hasPercentage && !hasAmount)
            {
                MessageBox.Show("يرجى إدخال نسبة أو قيمة الخصم", "خطأ في التحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
                PercentageTextBox.Focus();
                return false;
            }

            if (hasPercentage && hasAmount)
            {
                MessageBox.Show("يرجى إدخال إما نسبة أو قيمة، وليس كلاهما", "خطأ في التحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }
    }
}
