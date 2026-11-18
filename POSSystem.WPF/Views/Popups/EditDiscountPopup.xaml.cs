using POSSystem.WPF.ViewModels.Discounts;
using POSSystem.WPF.ViewModels;

namespace POSSystem.WPF.Views.Popups
{
    public partial class EditDiscountPopup : Window
    {
        private DiscountViewModel _viewModel = null!;
        private DiscountsViewModel _parentViewModel = null!;
        private Discount _originalDiscount = null!;

        public EditDiscountPopup(Discount discount, DiscountsViewModel parentViewModel)
        {
            try
            {
                InitializeComponent();
                _parentViewModel = parentViewModel;
                _originalDiscount = discount;
                _viewModel = new DiscountViewModel
                {
                    Name = discount.Name,
                    Code = discount.Code,
                    Description = discount.Description,
                    Percentage = discount.Percentage,
                    Amount = discount.Amount,
                    StartDate = discount.StartDate,
                    EndDate = discount.EndDate,
                    IsActive = discount.IsActive
                };
                DataContext = _viewModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تهيئة نافذة تعديل الخصم: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
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
                // Update the original discount
                _originalDiscount.Name = _viewModel.Name;
                _originalDiscount.Code = _viewModel.Code;
                _originalDiscount.Description = _viewModel.Description;
                _originalDiscount.Percentage = _viewModel.Percentage;
                _originalDiscount.Amount = _viewModel.Amount;
                _originalDiscount.StartDate = _viewModel.StartDate;
                _originalDiscount.EndDate = _viewModel.EndDate;
                _originalDiscount.IsActive = _viewModel.IsActive;
                _originalDiscount.UpdatedAt = DateTime.Now;

                // Update in parent view model
                _parentViewModel.UpdateDiscount(_originalDiscount);

                MessageBox.Show("تم تحديث الخصم بنجاح!", "تم التحديث", 
                    MessageBoxButton.OK, MessageBoxImage.Information);

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحديث الخصم: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("هل أنت متأكد من إلغاء تعديل الخصم؟", 
                "تأكيد الإلغاء", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }
    }

    public class DiscountViewModel : BaseViewModel
    {
        private string _name = "";
        private string _code = "";
        private string _description = "";
        private decimal _percentage = 0;
        private decimal _amount = 0;
        private DateTime _startDate = DateTime.Now;
        private DateTime _endDate = DateTime.Now.AddDays(30);
        private bool _isActive = false;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Code
        {
            get => _code;
            set
            {
                _code = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        public decimal Percentage
        {
            get => _percentage;
            set
            {
                _percentage = value;
                OnPropertyChanged();
            }
        }

        public decimal Amount
        {
            get => _amount;
            set
            {
                _amount = value;
                OnPropertyChanged();
            }
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged();
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged();
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged();
            }
        }
    }
}
