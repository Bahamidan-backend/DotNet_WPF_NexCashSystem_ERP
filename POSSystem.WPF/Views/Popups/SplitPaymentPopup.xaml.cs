using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using POSSystem.WPF.Data;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;
using POSSystem.WPF.Services;

namespace POSSystem.WPF.Views.Popups
{
    public partial class SplitPaymentPopup : Window
    {
        public decimal TotalAmount { get; }
        public decimal CashAmount { get; private set; }
        public decimal CardAmount { get; private set; }
        public decimal DebtAmount { get; private set; }
        public string BankOrWallet { get; private set; } = "حاسب كريمي";
        public int? SelectedCustomerId { get; private set; }
        public string CustomerName { get; private set; } = string.Empty;
        public string CustomerPhone { get; private set; } = string.Empty;

        private bool _isUpdatingText = false;
        private List<Customer> _customers = new();

        public SplitPaymentPopup(decimal totalAmount)
        {
            InitializeComponent();
            TotalAmount = totalAmount;
            TotalAmountText.Text = $"{TotalAmount:N0} ريال";

            LoadUnifiedWallets();
            LoadExistingCustomers();

            decimal half = Math.Round(TotalAmount / 2m, 2);
            _isUpdatingText = true;
            FirstAmountBox.Text = half.ToString("F2", CultureInfo.InvariantCulture);
            SecondAmountBox.Text = (TotalAmount - half).ToString("F2", CultureInfo.InvariantCulture);
            _isUpdatingText = false;

            UpdatePanelsVisibility();
            FirstAmountBox.Focus();
            FirstAmountBox.SelectAll();
        }

        private void LoadUnifiedWallets()
        {
            var wallets = FinancialChannelHelper.GetActiveWalletsList();
            var settings = SettingsService.LoadSettings();
            string defaultWallet = !string.IsNullOrWhiteSpace(settings.DefaultBankOrWallet) && settings.DefaultBankOrWallet != "بنك الراجحي"
                ? settings.DefaultBankOrWallet
                : "حاسب كريمي";

            BankOrWallet = defaultWallet;

            FirstWalletComboBox.ItemsSource = wallets;
            SecondWalletComboBox.ItemsSource = wallets;

            FirstWalletComboBox.SelectedItem = wallets.FirstOrDefault(w => w.Equals(defaultWallet, StringComparison.OrdinalIgnoreCase)) ?? wallets.FirstOrDefault();
            SecondWalletComboBox.SelectedItem = FirstWalletComboBox.SelectedItem;
        }

        private void LoadExistingCustomers()
        {
            try
            {
                using var context = new POSDbContext();
                _customers = context.Customers.Where(c => c.IsActive).OrderBy(c => c.Name).ToList();
                ExistingCustomersComboBox.ItemsSource = _customers;
            }
            catch { }
        }

        private string GetMethodTag(ComboBox combo)
        {
            if (combo?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
                return tag;
            return "Cash";
        }

        private void FirstMethodCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePanelsVisibility();
        }

        private void SecondMethodCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePanelsVisibility();
        }

        private void UpdatePanelsVisibility()
        {
            if (FirstWalletBorder == null || SecondWalletBorder == null || DebtCustomerBorder == null)
                return;

            string m1 = GetMethodTag(FirstMethodCombo);
            string m2 = GetMethodTag(SecondMethodCombo);

            FirstWalletBorder.Visibility = m1 == "Deposit" ? Visibility.Visible : Visibility.Collapsed;
            SecondWalletBorder.Visibility = m2 == "Deposit" ? Visibility.Visible : Visibility.Collapsed;
            DebtCustomerBorder.Visibility = m2 == "OnCredit" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ExistingCustomersComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ExistingCustomersComboBox.SelectedItem is Customer cust)
            {
                SelectedCustomerId = cust.Id;
                DebtCustomerNameBox.Text = cust.Name;
                DebtCustomerPhoneBox.Text = cust.PhoneNumber ?? string.Empty;
            }
        }

        private void WalletComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && !string.IsNullOrWhiteSpace(cb.SelectedItem?.ToString()))
            {
                BankOrWallet = cb.SelectedItem.ToString()!.Trim();
            }
        }

        private void AddCustomWallet_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "إضافة محفظة جديدة",
                Width = 380,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                FlowDirection = FlowDirection.RightToLeft,
                ResizeMode = ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.White
            };

            var sp = new StackPanel { Margin = new Thickness(18) };
            sp.Children.Add(new TextBlock { Text = "اسم المحفظة الجديدة *", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 8) });
            var tb = new TextBox { Height = 36, FontSize = 14, Padding = new Thickness(8, 5, 8, 5), Margin = new Thickness(0, 0, 0, 14) };
            sp.Children.Add(tb);

            var btn = new Button
            {
                Content = "حفظ المحفظة وإضافتها للنظام",
                Height = 38,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 53, 140)),
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0)
            };
            btn.Click += (s, args) =>
            {
                if (!string.IsNullOrWhiteSpace(tb.Text))
                {
                    FinancialChannelHelper.AddCustomWallet(tb.Text.Trim());
                    dialog.DialogResult = true;
                    dialog.Close();
                }
            };
            sp.Children.Add(btn);
            dialog.Content = sp;
            tb.Focus();

            if (dialog.ShowDialog() == true)
            {
                string added = tb.Text.Trim();
                LoadUnifiedWallets();
                FirstWalletComboBox.SelectedItem = added;
                SecondWalletComboBox.SelectedItem = added;
                BankOrWallet = added;
            }
        }

        private void FirstAmountBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingText || SecondAmountBox == null) return;

            string txt = FirstAmountBox.Text.Trim().Replace(',', '.');
            if (decimal.TryParse(txt, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal firstAmt))
            {
                if (firstAmt >= 0 && firstAmt <= TotalAmount)
                {
                    _isUpdatingText = true;
                    SecondAmountBox.Text = (TotalAmount - firstAmt).ToString("F2", CultureInfo.InvariantCulture);
                    _isUpdatingText = false;
                    ClearError();
                }
                else if (firstAmt > TotalAmount)
                {
                    ShowError($"مبلغ الدفعة الأولى أكبر من إجمالي الفاتورة ({TotalAmount:N0} ريال)");
                }
            }
        }

        private void SecondAmountBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingText || FirstAmountBox == null) return;

            string txt = SecondAmountBox.Text.Trim().Replace(',', '.');
            if (decimal.TryParse(txt, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal secondAmt))
            {
                if (secondAmt >= 0 && secondAmt <= TotalAmount)
                {
                    _isUpdatingText = true;
                    FirstAmountBox.Text = (TotalAmount - secondAmt).ToString("F2", CultureInfo.InvariantCulture);
                    _isUpdatingText = false;
                    ClearError();
                }
                else if (secondAmt > TotalAmount)
                {
                    ShowError($"مبلغ الدفعة الثانية أكبر من إجمالي الفاتورة ({TotalAmount:N0} ريال)");
                }
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            string t1 = FirstAmountBox.Text.Trim().Replace(',', '.');
            string t2 = SecondAmountBox.Text.Trim().Replace(',', '.');

            if (!decimal.TryParse(t1, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amt1) || amt1 < 0)
            {
                ShowError("يرجى إدخال مبلغ صحيح للدفعة الأولى.");
                FirstAmountBox.Focus();
                return;
            }

            if (!decimal.TryParse(t2, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amt2) || amt2 < 0)
            {
                ShowError("يرجى إدخال مبلغ صحيح للدفعة الثانية.");
                SecondAmountBox.Focus();
                return;
            }

            if (Math.Abs((amt1 + amt2) - TotalAmount) > 0.05m)
            {
                ShowError($"مجموع الدفعتين ({amt1 + amt2:N0} ريال) يجب أن يساوي إجمالي الفاتورة ({TotalAmount:N0} ريال).");
                return;
            }

            string m1 = GetMethodTag(FirstMethodCombo);
            string m2 = GetMethodTag(SecondMethodCombo);

            decimal cashTotal = 0m;
            decimal depositTotal = 0m;
            decimal debtTotal = 0m;
            string chosenWallet = string.Empty;

            if (m1 == "Cash") cashTotal += amt1;
            else if (m1 == "Deposit")
            {
                depositTotal += amt1;
                chosenWallet = FirstWalletComboBox.Text?.Trim() ?? "حاسب كريمي";
            }

            if (m2 == "Cash") cashTotal += amt2;
            else if (m2 == "Deposit")
            {
                depositTotal += amt2;
                string w2 = SecondWalletComboBox.Text?.Trim() ?? "حاسب كريمي";
                chosenWallet = string.IsNullOrWhiteSpace(chosenWallet) ? w2 : chosenWallet;
            }
            else if (m2 == "OnCredit")
            {
                debtTotal += amt2;
            }

            if (debtTotal > 0)
            {
                string custName = DebtCustomerNameBox.Text?.Trim() ?? string.Empty;
                string custPhone = DebtCustomerPhoneBox.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(custName))
                {
                    ShowError("يرجى اختيار العميل أو كتابة اسم العميل لتسجيل المبلغ المتبقي كدين آجل.");
                    DebtCustomerNameBox.Focus();
                    return;
                }
                if (string.IsNullOrWhiteSpace(custPhone))
                {
                    ShowError("يرجى إدخال رقم هاتف العميل لتسجيل الدين الآجل.");
                    DebtCustomerPhoneBox.Focus();
                    return;
                }

                CustomerName = custName;
                CustomerPhone = custPhone;
                var matchedCust = _customers.FirstOrDefault(c => c.Name.Equals(custName, StringComparison.OrdinalIgnoreCase));
                SelectedCustomerId = matchedCust?.Id;
            }

            if (!string.IsNullOrWhiteSpace(chosenWallet))
            {
                FinancialChannelHelper.AddCustomWallet(chosenWallet);
                BankOrWallet = chosenWallet;
            }

            CashAmount = cashTotal;
            CardAmount = depositTotal;
            DebtAmount = debtTotal;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowError(string message)
        {
            StatusMessage.Text = message;
            StatusMessage.Visibility = Visibility.Visible;
        }

        private void ClearError()
        {
            StatusMessage.Text = string.Empty;
            StatusMessage.Visibility = Visibility.Collapsed;
        }
    }
}
