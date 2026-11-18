using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Controls;
using POSSystem.WPF.Models;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Business;

namespace POSSystem.WPF.Views.Popups
{
    public partial class RepayCustomerDebtPopup : Window
    {
        private readonly CustomerDebt _debt;
        private readonly ICustomerDebtService _customerDebtService;
        private readonly IShiftService _shiftService;

        public RepayCustomerDebtPopup(CustomerDebt debt)
        {
            InitializeComponent();
            _debt = debt;
            _customerDebtService = POSSystem.WPF.App.ServiceProvider.GetRequiredService<ICustomerDebtService>();
            _shiftService = new ShiftService(POSSystem.WPF.App.ServiceProvider.GetRequiredService<IUnitOfWork>());

            LoadDebtData();
        }

        private void LoadDebtData()
        {
            CustomerNameTextBlock.Text = _debt.CustomerName;
            TotalAmountTextBlock.Text = $"{_debt.Amount:N0} ريال";
            RemainingAmountTextBlock.Text = $"{_debt.RemainingAmount:N0} ريال";
            AmountTextBox.Text = _debt.RemainingAmount.ToString("N0");
            AmountTextBox.Focus();
            AmountTextBox.SelectAll();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void PaymentMethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TransactionRefBorder == null) return;
            var selectedItem = PaymentMethodComboBox?.SelectedItem as ComboBoxItem;
            var tag = selectedItem?.Tag?.ToString() ?? "Cash";

            if (tag != "Cash")
            {
                TransactionRefBorder.Visibility = Visibility.Visible;
                if (tag.StartsWith("Wallet_") || tag == "MobilePayment")
                {
                    TransactionRefLabel.Text = "رقم العملية / الحوالة في المحفظة (اختياري)";
                }
                else if (tag == "BankTransfer")
                {
                    TransactionRefLabel.Text = "رقم الحوالة البنكية / الإشعار (اختياري)";
                }
                else
                {
                    TransactionRefLabel.Text = "رقم الإيصال / العملية (اختياري)";
                }
            }
            else
            {
                TransactionRefBorder.Visibility = Visibility.Collapsed;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(AmountTextBox.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("يرجى إدخال مبلغ صحيح", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (amount > _debt.RemainingAmount)
            {
                var result = MessageBox.Show($"المبلغ المدخل ({amount:N0}) أكبر من المتبقي ({_debt.RemainingAmount:N0}). هل تريد تسديد المتبقي بالكامل فقط؟", "تنبيه", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    amount = _debt.RemainingAmount;
                }
                else
                {
                    return;
                }
            }

            try
            {
                // Get current user
                var currentUser = AdminDashboardWindow._currentUser;
                if (currentUser == null)
                {
                    MessageBox.Show("لم يتم العثور على مستخدم مسجل الدخول", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Map payment method
                var selectedMethodItem = PaymentMethodComboBox?.SelectedItem as ComboBoxItem;
                var methodTag = selectedMethodItem?.Tag?.ToString() ?? "Cash";
                var paymentMethod = (methodTag.StartsWith("Wallet_") || methodTag == "MobilePayment")
                    ? PaymentMethod.MobilePayment
                    : methodTag switch
                    {
                        "Cash" => PaymentMethod.Cash,
                        "BankTransfer" => PaymentMethod.BankTransfer,
                        "DebitCard" => PaymentMethod.DebitCard,
                        _ => PaymentMethod.Cash
                    };

                // Prepare wallet / reference notes
                string finalNotes = NotesTextBox.Text?.Trim() ?? string.Empty;
                string walletName = methodTag switch
                {
                    "Wallet_Kuraimi" => "حاسب كريمي",
                    "Wallet_Jeeb" => "محفظة جيب",
                    "Wallet_Floosak" => "محفظة فلوسك",
                    "Wallet_OneCash" => "محفظة ون كاش",
                    "Wallet_Jawali" => "محفظة جوالي",
                    "Wallet_MobileMoney" => "محفظة موبايل موني",
                    "Wallet_YemenWallet" => "محفظة يمن والت",
                    _ => methodTag.StartsWith("Wallet_") ? methodTag.Substring("Wallet_".Length) : string.Empty
                };

                var refNo = TransactionRefTextBox?.Text?.Trim();
                var noteTags = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrEmpty(walletName) && !finalNotes.Contains(walletName))
                {
                    noteTags.Add($"[محفظة: {walletName}]");
                }
                if (!string.IsNullOrEmpty(refNo) && !finalNotes.Contains(refNo))
                {
                    noteTags.Add($"[رقم العملية: {refNo}]");
                }
                if (noteTags.Count > 0)
                {
                    finalNotes = string.IsNullOrWhiteSpace(finalNotes)
                        ? string.Join(" ", noteTags)
                        : $"{finalNotes} {string.Join(" ", noteTags)}";
                }

                // Get active shift
                var activeShift = await _shiftService.GetActiveShiftByUserIdAsync(currentUser.Id);

                await _customerDebtService.RepayDebtAsync(_debt.Id, amount, currentUser.Id, activeShift?.Id, finalNotes, paymentMethod);
                
                // Try to print receipt
                try
                {
                    var dummyOrderForDebt = new Order 
                    { 
                        Total = amount,
                        PaymentMethod = paymentMethod,
                        OrderItems = new System.Collections.Generic.List<OrderItem>(),
                        OrderNumber = "DEBT-P-" + _debt.Id,
                        CreatedAt = DateTime.Now
                    };
                    _ = Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            POSSystem.WPF.StaticFunction.GloublStatic.PrintReceipt(dummyOrderForDebt, _debt);
                        }
                        catch { }
                    }));
                }
                catch { }

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
