using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Views.Popups
{
    public class CustomerLookupItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public decimal PreviousDebt { get; set; }

        public string DisplayPhone => !string.IsNullOrWhiteSpace(PhoneNumber) ? $"📱 {PhoneNumber}" : "بدون هاتف";
        public bool HasPhoneNumber => !string.IsNullOrWhiteSpace(PhoneNumber);
        public bool HasDebt => PreviousDebt > 0;
        public string DebtSummaryText => PreviousDebt > 0 
            ? $"⚠️ دين: {PreviousDebt:N0} ريال" 
            : "✓ سليم (لا ديون)";

        public Brush DebtBadgeBackground => PreviousDebt > 0 
            ? new SolidColorBrush(Color.FromRgb(254, 226, 226)) 
            : new SolidColorBrush(Color.FromRgb(240, 253, 244));

        public Brush DebtBadgeForeground => PreviousDebt > 0 
            ? new SolidColorBrush(Color.FromRgb(185, 28, 28)) 
            : new SolidColorBrush(Color.FromRgb(21, 128, 61));
    }

    public partial class CustomerInfoPopup : Window
    {
        private List<CustomerLookupItem> _allCustomers = new();
        private bool _isUpdatingFromCode;

        public int SelectedCustomerId { get; private set; }
        public string CustomerName { get; private set; } = string.Empty;
        public string PhoneNumber { get; private set; } = string.Empty;
        public decimal? CreditLimit { get; private set; }
        public string Notes { get; private set; } = string.Empty;
        public decimal OrderTotal { get; private set; }

        public CustomerInfoPopup() : this(0)
        {
        }

        public CustomerInfoPopup(decimal orderTotal)
        {
            InitializeComponent();
            OrderTotal = orderTotal;

            if (OrderTotal > 0)
            {
                OrderTotalText.Text = $"{OrderTotal:N0} ريال";
                OrderTotalBanner.Visibility = Visibility.Visible;
            }
            else
            {
                OrderTotalBanner.Visibility = Visibility.Collapsed;
            }

            Loaded += CustomerInfoPopup_Loaded;
        }

        private async void CustomerInfoPopup_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var sp = App.ServiceProvider;
                var unitOfWork = sp.GetRequiredService<IUnitOfWork>();

                var customers = (await unitOfWork.Customers.FindAsync(c => c.IsActive)).ToList();
                var unpaidDebts = (await unitOfWork.CustomerDebts.FindAsync(d => !d.IsPaid)).ToList();

                var debtMap = unpaidDebts
                    .GroupBy(d => d.CustomerId)
                    .ToDictionary(g => g.Key, g => g.Sum(d => d.RemainingAmount));

                _allCustomers = customers
                    .OrderBy(c => c.Name)
                    .Select(c => new CustomerLookupItem
                    {
                        Id = c.Id,
                        Name = c.Name,
                        PhoneNumber = c.PhoneNumber ?? string.Empty,
                        CreditLimit = c.CreditLimit,
                        PreviousDebt = debtMap.TryGetValue(c.Id, out var debt) ? debt : 0
                    })
                    .ToList();

                CustomerComboBox.ItemsSource = _allCustomers;

                // Listen to typing in editable ComboBox text
                CustomerComboBox.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(OnCustomerComboBoxTextChanged));

                CustomerComboBox.Focus();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading customers in CustomerInfoPopup: {ex.Message}");
            }
        }

        private void CustomerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CustomerComboBox.SelectedItem is CustomerLookupItem selected)
            {
                _isUpdatingFromCode = true;
                SelectedCustomerId = selected.Id;
                PhoneNumberTextBox.Text = selected.PhoneNumber;
                CreditLimitTextBox.Text = selected.CreditLimit.ToString("G29");
                _isUpdatingFromCode = false;

                ShowCustomerStatus(selected);
            }
        }

        private void OnCustomerComboBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingFromCode) return;

            string text = CustomerComboBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                SelectedCustomerId = 0;
                CustomerStatusBorder.Visibility = Visibility.Collapsed;
                return;
            }

            var match = _allCustomers.FirstOrDefault(c => c.Name.Equals(text, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                SelectedCustomerId = match.Id;
                if (string.IsNullOrWhiteSpace(PhoneNumberTextBox.Text) || _allCustomers.Any(c => c.PhoneNumber == PhoneNumberTextBox.Text))
                {
                    _isUpdatingFromCode = true;
                    PhoneNumberTextBox.Text = match.PhoneNumber;
                    CreditLimitTextBox.Text = match.CreditLimit.ToString("G29");
                    _isUpdatingFromCode = false;
                }
                ShowCustomerStatus(match);
            }
            else
            {
                SelectedCustomerId = 0;
                CustomerStatusBorder.Visibility = Visibility.Visible;
                CustomerStatusBorder.Background = new SolidColorBrush(Color.FromRgb(238, 242, 255));
                CustomerStatusText.Foreground = new SolidColorBrush(Color.FromRgb(67, 56, 202));
                CustomerStatusText.Text = $"➕ عميل جديد: \"{text}\" (سيتم حفظه تلقائياً في سجل العملاء)";
            }
        }

        private void PhoneNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingFromCode) return;

            string phone = PhoneNumberTextBox.Text?.Trim() ?? string.Empty;
            if (phone.Length >= 6 && SelectedCustomerId == 0)
            {
                var match = _allCustomers.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.PhoneNumber) && c.PhoneNumber == phone);
                if (match != null)
                {
                    _isUpdatingFromCode = true;
                    SelectedCustomerId = match.Id;
                    CustomerComboBox.Text = match.Name;
                    _isUpdatingFromCode = false;
                    ShowCustomerStatus(match);
                }
            }
        }

        private void ShowCustomerStatus(CustomerLookupItem customer)
        {
            CustomerStatusBorder.Visibility = Visibility.Visible;
            string limitInfo = customer.CreditLimit > 0
                ? $" | سقف المديونية: {customer.CreditLimit:N0} ريال (المتاح: {Math.Max(0, customer.CreditLimit - customer.PreviousDebt):N0} ريال)"
                : " | سقف المديونية: مفتوح";

            if (customer.PreviousDebt > 0)
            {
                CustomerStatusBorder.Background = new SolidColorBrush(Color.FromRgb(254, 242, 242));
                CustomerStatusText.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
                CustomerStatusText.Text = $"⚠️ عميل مسجل: {customer.Name} | الديون السابقة: {customer.PreviousDebt:N0} ريال{limitInfo}";
            }
            else
            {
                CustomerStatusBorder.Background = new SolidColorBrush(Color.FromRgb(240, 253, 244));
                CustomerStatusText.Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61));
                CustomerStatusText.Text = $"✓ عميل مسجل: {customer.Name} | لا توجد عليه ديون سابقة{limitInfo}";
            }
        }

        private void NewCustomerButton_Click(object sender, RoutedEventArgs e)
        {
            _isUpdatingFromCode = true;
            SelectedCustomerId = 0;
            CustomerComboBox.SelectedItem = null;
            CustomerComboBox.Text = string.Empty;
            PhoneNumberTextBox.Text = string.Empty;
            CreditLimitTextBox.Text = "0";
            NotesTextBox.Text = string.Empty;
            _isUpdatingFromCode = false;

            CustomerStatusBorder.Visibility = Visibility.Visible;
            CustomerStatusBorder.Background = new SolidColorBrush(Color.FromRgb(238, 242, 255));
            CustomerStatusText.Foreground = new SolidColorBrush(Color.FromRgb(67, 56, 202));
            CustomerStatusText.Text = "➕ أدخل اسم العميل الجديد ورقم هاتفه";
            CustomerComboBox.Focus();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            string name = CustomerComboBox.Text?.Trim() ?? string.Empty;
            string phone = PhoneNumberTextBox.Text?.Trim() ?? string.Empty;
            string notes = NotesTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("يرجى إدخال اسم العميل أو اختياره من القائمة المنسدلة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                CustomerComboBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("يرجى إدخال رقم هاتف العميل للتواصل وقيد الدين.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                PhoneNumberTextBox.Focus();
                return;
            }

            // If an existing customer matches by name or phone and wasn't selected, link it
            if (SelectedCustomerId == 0)
            {
                var match = _allCustomers.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                                                             (!string.IsNullOrWhiteSpace(c.PhoneNumber) && c.PhoneNumber == phone));
                if (match != null)
                {
                    SelectedCustomerId = match.Id;
                }
            }

            if (decimal.TryParse(CreditLimitTextBox.Text?.Trim(), out var parsedLimit) && parsedLimit >= 0)
            {
                CreditLimit = parsedLimit;
            }
            else
            {
                CreditLimit = 0m;
            }

            CustomerName = name;
            PhoneNumber = phone;
            Notes = notes;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
