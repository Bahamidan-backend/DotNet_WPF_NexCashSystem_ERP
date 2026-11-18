using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.ViewModels.Discounts;
using POSSystem.WPF.ViewModels;
namespace POSSystem.WPF.Views.AdminPages
{
    public partial class DiscountsView : UserControl
    {
        private DiscountsViewModel _viewModel;

        public DiscountsView()
        {
            InitializeComponent();
            _viewModel = new DiscountsViewModel();
            DataContext = _viewModel;
        }

        private void AddDiscountButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addEditDiscountControl = new POSSystem.WPF.Views.Popups.AddEditDiscountControl(_viewModel);
                var adminWindow = Window.GetWindow(this) as AdminDashboardWindow;
                adminWindow?.ShowOverlay(addEditDiscountControl);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح نافذة إضافة الخصم: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditDiscountButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Discount discount)
            {
                try
                {
                    var addEditDiscountControl = new POSSystem.WPF.Views.Popups.AddEditDiscountControl(_viewModel, discount);
                    var adminWindow = Window.GetWindow(this) as AdminDashboardWindow;
                    adminWindow?.ShowOverlay(addEditDiscountControl);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في فتح نافذة تعديل الخصم: {ex.Message}", "خطأ", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteDiscountButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Discount discount)
            {
                var result = MessageBox.Show($"هل أنت متأكد من حذف الخصم '{discount.Name}'؟", 
                    "تأكيد الحذف", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _viewModel.DeleteDiscount(discount);
                    MessageBox.Show("تم حذف الخصم بنجاح", "تم الحذف", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                _viewModel.discountsProberty.SearchText = textBox.Text;
                _viewModel.discountsProberty.FilterDiscounts();
            }
        }

        private void AllStatus_Checked(object sender, RoutedEventArgs e)
        {
            _viewModel.SelectAllStatus();
        }

        private void StatusFilter_Checked(object sender, RoutedEventArgs e)
        {
            _viewModel.discountsProberty.FilterDiscounts();
        }

        private void StatusFilter_Unchecked(object sender, RoutedEventArgs e)
        {
            _viewModel.discountsProberty.FilterDiscounts();
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ClearAllFilters();
        }

        private async void ApplyBulkDiscountButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.discountsProberty.BulkDiscountPercentage <= 0)
            {
                MessageBox.Show("يرجى إدخال نسبة خصم صحيحة أكبر من صفر.", "تنبيه", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"هل أنت متأكد من تطبيق خصم بنسبة {_viewModel.discountsProberty.BulkDiscountPercentage}% على جميع المنتجات؟\n\n⚠️ سيتم تحديث أسعار جميع المنتجات بشكل دائم.", 
                "تأكيد الخصم الجماعي", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _viewModel.ApplyBulkDiscount();
                    MessageBox.Show("تم تطبيق الخصم على جميع المنتجات بنجاح.", "تم التحديث", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ أثناء تطبيق الخصم: {ex.Message}", "خطأ", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void RevertBulkDiscountButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("هل أنت متأكد من إلغاء الخصم واستعادة الأسعار الأصلية لجميع المنتجات؟", 
                "تأكيد استعادة الأسعار", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _viewModel.RevertBulkDiscount();
                    MessageBox.Show("تم استعادة الأسعار الأصلية بنجاح.", "تم التحديث", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ أثناء استعادة الأسعار: {ex.Message}", "خطأ", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

    }
}
