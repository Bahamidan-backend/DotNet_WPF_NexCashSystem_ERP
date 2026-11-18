using POSSystem.WPF.ViewModels.Discounts;

namespace POSSystem.WPF.Views.Popups
{
    public partial class AddDiscountPopup : Window
    {
        private DiscountViewModel _viewModel = null!;
        private DiscountsViewModel _parentViewModel = null!;

        public AddDiscountPopup(DiscountsViewModel parentViewModel)
        {
            try
            {
                InitializeComponent();
                _parentViewModel = parentViewModel;
                _viewModel = new DiscountViewModel();
                DataContext = _viewModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تهيئة نافذة إضافة الخصم: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }


        public async void AddNewDiscount(Discount newDiscount)
        {
          await  _parentViewModel.AddDiscount(newDiscount);

        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(_viewModel.Name))
            {
                MessageBox.Show("يرجى إدخال اسم المناسبة", "خطأ في التحقق", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(_viewModel.Code))
            {
                MessageBox.Show("يرجى إدخال كود الخصم", "خطأ في التحقق", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CodeTextBox.Focus();
                return;
            }

            if (_viewModel.StartDate == default(DateTime))
            {
                MessageBox.Show("يرجى اختيار تاريخ البداية", "خطأ في التحقق", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                StartDatePicker.Focus();
                return;
            }

            if (_viewModel.EndDate == default(DateTime))
            {
                MessageBox.Show("يرجى اختيار تاريخ النهاية", "خطأ في التحقق", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EndDatePicker.Focus();
                return;
            }

            if (_viewModel.StartDate >= _viewModel.EndDate)
            {
                MessageBox.Show("تاريخ البداية يجب أن يكون قبل تاريخ النهاية", "خطأ في التحقق", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                StartDatePicker.Focus();
                return;
            }

            if (_viewModel.Percentage <= 0 && _viewModel.Amount <= 0)
            {
                MessageBox.Show("يرجى إدخال نسبة أو قيمة الخصم", "خطأ في التحقق", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PercentageTextBox.Focus();
                return;
            }

            if (_viewModel.Percentage > 0 && _viewModel.Amount > 0)
            {
                MessageBox.Show("يرجى إدخال إما نسبة أو قيمة، وليس كلاهما", "خطأ في التحقق", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Create new discount
                var newDiscount = new Discount
                {
                    Name = _viewModel.Name,
                    Code = _viewModel.Code,
                    Description = _viewModel.Description,
                    Percentage = _viewModel.Percentage,
                    Amount = _viewModel.Amount,
                    StartDate = _viewModel.StartDate,
                    EndDate = _viewModel.EndDate,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                // Add to parent view model
                AddNewDiscount(newDiscount);
                MessageBox.Show("تم إضافة الخصم بنجاح!", "تم الحفظ", 
                    MessageBoxButton.OK, MessageBoxImage.Information);

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء حفظ الخصم: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("هل أنت متأكد من إلغاء إضافة الخصم؟", 
                "تأكيد الإلغاء", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }
    }

}
