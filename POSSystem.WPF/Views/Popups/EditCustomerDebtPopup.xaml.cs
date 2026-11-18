using POSSystem.WPF.Models;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Business.Services.interfase;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.WPF.Data;
using POSSystem.WPF.Data.UnitOfWork;

namespace POSSystem.WPF.Views.Popups
{
    public partial class EditCustomerDebtPopup : Window
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICustomerDebtService _customerDebtService;
        private CustomerDebt _originalDebt;
        private readonly IServiceScope _scope;

        public EditCustomerDebtPopup(CustomerDebt debt)
        {
            InitializeComponent();
            _scope = App.ServiceProvider.CreateScope();
            var context = _scope.ServiceProvider.GetRequiredService<POSDbContext>();
            _unitOfWork = new UnitOfWork(context);
            _customerDebtService = new CustomerDebtService(_unitOfWork);
            Closed += (_, _) => 
            {
                _unitOfWork.Dispose();
                _scope.Dispose();
            };
            _originalDebt = debt;
            DataContext = _originalDebt;

            Loaded += async (_, _) =>
            {
                try
                {
                    if (_originalDebt.CustomerId > 0)
                    {
                        var cust = await _unitOfWork.Customers.GetByIdAsync(_originalDebt.CustomerId);
                        if (cust != null)
                        {
                            CreditLimitTextBox.Text = cust.CreditLimit.ToString("G29");
                        }
                    }
                }
                catch { }
            };
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CustomerNameTextBox.Text))
                {
                    MessageBox.Show("يرجى إدخال اسم العميل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(AmountTextBox.Text, out var amount) || (amount <= 0 && _originalDebt.Id > 0) || amount < 0)
                {
                    MessageBox.Show("يرجى إدخال مبلغ صحيح أكبر من الصفر", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Update debt
                _originalDebt.CustomerName = CustomerNameTextBox.Text.Trim();
                _originalDebt.PhoneNumber = PhoneNumberTextBox.Text.Trim();
                if (decimal.TryParse(CreditLimitTextBox.Text?.Trim(), out var parsedLimit) && parsedLimit >= 0)
                {
                    _originalDebt.CreditLimit = parsedLimit;
                }
                _originalDebt.Amount = amount;
                _originalDebt.DebtDate = DebtDatePicker.SelectedDate ?? DateTime.Now;
                _originalDebt.Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();
                _originalDebt.UpdatedAt = DateTime.Now;

                if (_originalDebt.Id == 0 && _originalDebt.CustomerId > 0)
                {
                    var context = _unitOfWork.GetContext();
                    var cust = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(context.Customers, c => c.Id == _originalDebt.CustomerId);
                    if (cust != null)
                    {
                        cust.Name = _originalDebt.CustomerName;
                        cust.PhoneNumber = _originalDebt.PhoneNumber ?? string.Empty;
                        cust.CreditLimit = _originalDebt.CreditLimit ?? 0m;
                        cust.UpdatedAt = System.DateTime.Now;
                        context.Customers.Update(cust);
                        await _unitOfWork.SaveChangesAsync();
                    }
                    if (_originalDebt.Amount > 0)
                    {
                        await _customerDebtService.CreateDebtAsync(_originalDebt);
                    }
                }
                else
                {
                    await _customerDebtService.UpdateDebtAsync(_originalDebt);
                }
                MessageBox.Show("تم تحديث الدين بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحديث الدين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

