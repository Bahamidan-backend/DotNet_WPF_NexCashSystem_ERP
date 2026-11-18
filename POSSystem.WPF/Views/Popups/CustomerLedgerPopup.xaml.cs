using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Views.Popups
{
    public partial class CustomerLedgerPopup : Window
    {
        private readonly ICustomerDebtService _customerDebtService;
        private readonly int? _customerId;
        private readonly string _customerName;
        private readonly string? _phoneNumber;
        private List<CustomerLedgerEntry> _ledgerEntries = new();

        public CustomerLedgerPopup(CustomerDebt debt)
            : this(debt.CustomerId > 0 ? debt.CustomerId : (int?)null, debt.CustomerName, debt.PhoneNumber)
        {
        }

        public CustomerLedgerPopup(int? customerId, string customerName, string? phoneNumber = null)
        {
            InitializeComponent();
            _customerId = customerId;
            _customerName = customerName ?? "عميل غير محدد";
            _phoneNumber = phoneNumber;

            var sp = POSSystem.WPF.App.ServiceProvider;
            _customerDebtService = sp.GetRequiredService<ICustomerDebtService>();

            CustomerInfoTextBlock.Text = $"العميل: {_customerName}" + 
                (!string.IsNullOrWhiteSpace(_phoneNumber) ? $" | رقم الهاتف: {_phoneNumber}" : "");

            Loaded += async (s, e) => await LoadLedgerAsync();
        }

        private async Task LoadLedgerAsync()
        {
            try
            {
                IEnumerable<CustomerLedgerEntry> entries;
                if (_customerId.HasValue && _customerId.Value > 0)
                {
                    entries = await _customerDebtService.GetCustomerLedgerAsync(_customerId.Value);
                }
                else
                {
                    entries = await _customerDebtService.GetCustomerLedgerByNameOrPhoneAsync(_customerName);
                }

                try
                {
                    var sp = POSSystem.WPF.App.ServiceProvider;
                    var uow = sp.GetRequiredService<POSSystem.WPF.Data.UnitOfWork.IUnitOfWork>();
                    Customer? customer = null;
                    if (_customerId.HasValue && _customerId.Value > 0) customer = await uow.Customers.GetByIdAsync(_customerId.Value);
                    else customer = await uow.Customers.FirstOrDefaultAsync(c => c.Name == _customerName);
                    string limitStr = customer?.CreditLimit > 0 ? $"{customer.CreditLimit:N0} ريال" : "مفتوح";
                    CustomerInfoTextBlock.Text = $"العميل: {_customerName}" +
                        (!string.IsNullOrWhiteSpace(_phoneNumber) ? $" | رقم الهاتف: {_phoneNumber}" : "") +
                        $" | سقف المديونية: {limitStr}";
                }
                catch { }

                _ledgerEntries = entries.ToList();
                LedgerDataGrid.ItemsSource = _ledgerEntries;

                if (_ledgerEntries.Any())
                {
                    EmptyStateTextBlock.Visibility = Visibility.Collapsed;

                    decimal totalDebit = _ledgerEntries.Sum(e => e.Debit);
                    decimal totalCredit = _ledgerEntries.Sum(e => e.Credit);
                    decimal currentBalance = _ledgerEntries.First().Balance; // First entry is the latest after reverse

                    TotalDebitTextBlock.Text = $"{totalDebit:N0} ريال";
                    TotalCreditTextBlock.Text = $"{totalCredit:N0} ريال";
                    NetBalanceTextBlock.Text = $"{currentBalance:N0} ريال";

                    if (currentBalance > 0)
                    {
                        NetBalanceTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                        BalanceCardBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FECACA"));
                    }
                    else if (currentBalance == 0)
                    {
                        NetBalanceTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"));
                        BalanceCardBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BBF7D0"));
                    }
                    else
                    {
                        NetBalanceTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
                        BalanceCardBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BAE6FD"));
                    }
                }
                else
                {
                    EmptyStateTextBlock.Visibility = Visibility.Visible;
                    TotalDebitTextBlock.Text = "0.00 ريال";
                    TotalCreditTextBlock.Text = "0.00 ريال";
                    NetBalanceTextBlock.Text = "0.00 ريال";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحميل كشف الحساب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new System.Windows.Controls.PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(this, $"كشف حساب - {_customerName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"تعذر إرسال أمر الطباعة: {ex.Message}", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

