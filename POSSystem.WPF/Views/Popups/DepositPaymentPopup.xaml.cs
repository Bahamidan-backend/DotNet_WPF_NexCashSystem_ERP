using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Services;

namespace POSSystem.WPF.Views.Popups
{
    public partial class DepositPaymentPopup : Window
    {
        public string SelectedCurrency { get; private set; } = "ريال يمني";
        public decimal DepositAmount { get; private set; }
        public string BankOrWallet { get; private set; } = string.Empty;
        public string ReferenceNumber { get; private set; } = string.Empty;

        public DepositPaymentPopup(decimal orderTotal)
        {
            InitializeComponent();
            OrderTotalTextBlock.Text = orderTotal.ToString("N2", CultureInfo.InvariantCulture);
            AmountTextBox.Text = orderTotal.ToString("F2", CultureInfo.InvariantCulture);

            PopulateWalletsList();

            var settings = SettingsService.LoadSettings();
            string defaultBank = !string.IsNullOrWhiteSpace(settings.DefaultBankOrWallet) && settings.DefaultBankOrWallet != "بنك الراجحي" 
                ? settings.DefaultBankOrWallet 
                : "حاسب كريمي";
            BankComboBox.Text = defaultBank;
            BankOrWallet = defaultBank;

            SelectedCurrency = "ريال يمني";
            CurrencyBadge.Text = SelectedCurrency;

            UpdateCurrencyButtonsState();
            HighlightSelectedWallet(defaultBank);

            AmountTextBox.Focus();
            AmountTextBox.SelectAll();
        }

        private void PopulateWalletsList()
        {
            BankComboBox.Items.Clear();
            foreach (var w in FinancialChannelHelper.GetActiveWalletsList())
            {
                BankComboBox.Items.Add(new ComboBoxItem { Content = w });
            }
        }

        private void CurrencyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string currency)
            {
                SelectedCurrency = currency;
                CurrencyBadge.Text = currency;
                UpdateCurrencyButtonsState();
            }
        }

        private void UpdateCurrencyButtonsState()
        {
            SetCurrencyButtonStyle(CurrencyYerButton, SelectedCurrency == "ريال يمني");
            SetCurrencyButtonStyle(CurrencySarButton, SelectedCurrency == "ريال سعودي");
            SetCurrencyButtonStyle(CurrencyUsdButton, SelectedCurrency == "دولار أمريكي");
        }

        private void SetCurrencyButtonStyle(Button button, bool isSelected)
        {
            if (button == null) return;

            if (isSelected)
            {
                button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28a745"));
                button.Foreground = Brushes.White;
                button.BorderThickness = new Thickness(0);
                button.FontWeight = FontWeights.Bold;
            }
            else
            {
                button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f8f9fa"));
                button.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#495057"));
                button.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ced4da"));
                button.BorderThickness = new Thickness(1);
                button.FontWeight = FontWeights.SemiBold;
            }
        }

        private void SaveDefaultBank(string bank)
        {
            if (string.IsNullOrWhiteSpace(bank)) return;
            FinancialChannelHelper.AddCustomWallet(bank);
        }

        private void WalletQuickButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string wallet)
            {
                if (wallet == "ADD_NEW_WALLET")
                {
                    PromptAddCustomWallet();
                    return;
                }
                BankComboBox.Text = wallet;
                BankOrWallet = wallet;
                HighlightSelectedWallet(wallet);
                SaveDefaultBank(wallet);
            }
        }

        private void PromptAddCustomWallet()
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
                Background = Brushes.White
            };

            var sp = new StackPanel { Margin = new Thickness(18) };
            sp.Children.Add(new TextBlock { Text = "اسم المحفظة الجديدة *", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 8) });
            var tb = new TextBox { Height = 36, FontSize = 14, Padding = new Thickness(8, 5, 8, 5), Margin = new Thickness(0, 0, 0, 14) };
            sp.Children.Add(tb);

            var btn = new Button
            {
                Content = "حفظ المحفظة وإضافتها للنظام",
                Height = 38,
                Background = new SolidColorBrush(Color.FromRgb(0, 53, 140)),
                Foreground = Brushes.White,
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
                PopulateWalletsList();
                BankComboBox.Text = added;
                BankOrWallet = added;
                HighlightSelectedWallet(added);
            }
        }

        private void BankComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BankComboBox.SelectedItem is ComboBoxItem item && item.Content is string bankName)
            {
                BankOrWallet = bankName;
                HighlightSelectedWallet(bankName);
                SaveDefaultBank(bankName);
            }
        }

        private void BankComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(BankComboBox.Text))
            {
                BankOrWallet = BankComboBox.Text.Trim();
                HighlightSelectedWallet(BankOrWallet);
            }
        }

        private void HighlightSelectedWallet(string? selectedWallet)
        {
            var buttons = new[]
            {
                BtnWalletKuraimi,
                BtnWalletJeeb,
                BtnWalletFloosak,
                BtnWalletOneCash,
                BtnWalletJawali,
                BtnWalletMobileMoney,
                BtnWalletYemenWallet,
                BtnBankTransfer
            };

            foreach (var btn in buttons)
            {
                if (btn == null) continue;
                bool isMatch = !string.IsNullOrWhiteSpace(selectedWallet) &&
                               (btn.Tag?.ToString()?.Equals(selectedWallet.Trim(), StringComparison.OrdinalIgnoreCase) == true);

                if (isMatch)
                {
                    btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00358c"));
                    btn.Foreground = Brushes.White;
                    btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00286b"));
                    btn.BorderThickness = new Thickness(2);
                }
                else
                {
                    btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f0f4ff"));
                    btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00358c"));
                    btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c2daf8"));
                    btn.BorderThickness = new Thickness(1);
                }
            }
        }

        private void AmountTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            AmountTextBox.SelectAll();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            var text = AmountTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text) || (!decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedAmount) && !decimal.TryParse(text, out parsedAmount)))
            {
                MessageBox.Show("يرجى إدخال مبلغ إيداع صحيح", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                AmountTextBox.Focus();
                AmountTextBox.SelectAll();
                return;
            }

            if (parsedAmount <= 0)
            {
                MessageBox.Show("يجب أن يكون مبلغ الإيداع أكبر من الصفر", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                AmountTextBox.Focus();
                return;
            }

            DepositAmount = parsedAmount;
            BankOrWallet = BankComboBox.Text?.Trim() ?? string.Empty;
            ReferenceNumber = ReferenceNumberTextBox.Text?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(BankOrWallet))
            {
                FinancialChannelHelper.AddCustomWallet(BankOrWallet);
            }

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
