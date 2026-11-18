using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using POSSystem.WPF.Models;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Business;

namespace POSSystem.WPF.Views.Popups
{
    public partial class RepayAdvancePopup : Window
    {
        private readonly EmployeeAdvance _advance;
        private readonly IEmployeeService _employeeService;

        public RepayAdvancePopup(EmployeeAdvance advance)
        {
            InitializeComponent();
            _advance = advance;
            _employeeService = POSSystem.WPF.App.ServiceProvider.GetRequiredService<IEmployeeService>();

            LoadAdvanceData();
        }

        private void LoadAdvanceData()
        {
            EmployeeNameTextBlock.Text = _advance.Employee?.Name ?? "موظف";
            TotalAmountTextBlock.Text = $"{_advance.Amount:N0} ريال";
            RemainingAmountTextBlock.Text = $"{_advance.RemainingAmount:N0} ريال";
            AmountTextBox.Text = _advance.RemainingAmount.ToString("N0");
            AmountTextBox.Focus();
            AmountTextBox.SelectAll();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(AmountTextBox.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("يرجى إدخال مبلغ صحيح", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (amount > _advance.RemainingAmount)
            {
                var result = MessageBox.Show($"المبلغ المدخل ({amount:N0}) أكبر من المتبقي ({_advance.RemainingAmount:N0}). هل تريد تسديد المتبقي بالكامل فقط؟", "تنبيه", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    amount = _advance.RemainingAmount;
                }
                else
                {
                    return;
                }
            }

            try
            {
                await _employeeService.RepayAdvanceAsync(_advance.Id, amount, NotesTextBox.Text);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التسديد: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
