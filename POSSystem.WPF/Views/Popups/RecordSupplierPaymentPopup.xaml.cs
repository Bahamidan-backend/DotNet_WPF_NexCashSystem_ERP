using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using POSSystem.WPF.Models;
using POSSystem.WPF.Business;
using POSSystem.WPF.Helpers;
using System;
using System.Threading.Tasks;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;

namespace POSSystem.WPF.Views.Popups
{
    public partial class RecordSupplierPaymentPopup : Window
    {
        private readonly Supplier _supplier;
        private decimal _currentAvailableBalance = 0m;

        public RecordSupplierPaymentPopup(Supplier supplier)
        {
            InitializeComponent();
            _supplier = supplier;
            SupplierNameTextBlock.Text = supplier.Name;
            CurrentBalanceTextBlock.Text = supplier.TotalBalance.ToString("N0");
            RemainingBalanceTextBlock.Text = supplier.TotalBalance.ToString("N0");

            _ = UpdateAvailableBalanceAsync();
        }

        private void AmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (RemainingBalanceTextBlock == null) return;

            if (decimal.TryParse(AmountTextBox.Text, out decimal paidAmount))
            {
                decimal remaining = _supplier.TotalBalance - paidAmount;
                RemainingBalanceTextBlock.Text = remaining.ToString("N0");

                if (remaining < 0)
                    RemainingBalanceTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                else if (remaining == 0)
                    RemainingBalanceTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                else
                    RemainingBalanceTextBlock.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#2980B9")!;
            }
            else
            {
                RemainingBalanceTextBlock.Text = _supplier.TotalBalance.ToString("N0");
                RemainingBalanceTextBlock.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#2980B9")!;
            }

            ValidateAmountAgainstBalance();
        }

        private void PaymentMethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TransactionRefPanel == null) return;
            var selectedItem = PaymentMethodComboBox?.SelectedItem as ComboBoxItem;
            var tag = selectedItem?.Tag?.ToString() ?? "Cash";

            if (tag != "Cash" && tag != "Other")
            {
                TransactionRefPanel.Visibility = Visibility.Visible;
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
                TransactionRefPanel.Visibility = Visibility.Collapsed;
            }

            _ = UpdateAvailableBalanceAsync();
        }

        private async Task UpdateAvailableBalanceAsync()
        {
            try
            {
                if (AvailableBalanceBorder == null || AvailableBalanceTextBlock == null) return;

                var selectedItem = PaymentMethodComboBox?.SelectedItem as ComboBoxItem;
                var tag = selectedItem?.Tag?.ToString() ?? "Cash";

                if (tag == "Other")
                {
                    AvailableBalanceBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f8fafc"));
                    AvailableBalanceBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#cbd5e1"));
                    AvailableBalanceTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
                    AvailableBalanceLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
                    AvailableBalanceLabel.Text = "جهة الصرف: خزينة الإدارة";
                    AvailableBalanceTextBlock.Text = "خارج الصندوق (غير مقيد)";
                    if (AvailableBalanceIcon != null) AvailableBalanceIcon.Text = "🏢";
                    _currentAvailableBalance = decimal.MaxValue;
                    return;
                }

                var channelName = FinancialChannelHelper.GetChannelDisplayName(tag);
                AvailableBalanceTextBlock.Text = "جاري التحميل...";

                int? shiftId = null;
                if (tag == "Cash")
                {
                    var user = AdminDashboardWindow._currentUser;
                    if (user != null)
                    {
                        var shiftService = new Business.Services.ShiftService(POSSystem.WPF.App.ServiceProvider.GetRequiredService<IUnitOfWork>());
                        var activeShift = await shiftService.GetActiveShiftByUserIdAsync(user.Id);
                        shiftId = activeShift?.Id;
                    }
                }

                decimal balance = await ServiceLocator.FinancialChannelService.GetAvailableBalanceAsync(tag, shiftId);
                _currentAvailableBalance = balance;
                AvailableBalanceTextBlock.Text = $"{balance:N0} ريال";

                ValidateAmountAgainstBalance();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating available balance in RecordSupplierPaymentPopup: {ex.Message}");
            }
        }

        private void ValidateAmountAgainstBalance()
        {
            if (AvailableBalanceBorder == null || AvailableBalanceTextBlock == null || AvailableBalanceLabel == null) return;

            var selectedItem = PaymentMethodComboBox?.SelectedItem as ComboBoxItem;
            var tag = selectedItem?.Tag?.ToString() ?? "Cash";

            if (tag == "Other") return;

            var channelName = FinancialChannelHelper.GetChannelDisplayName(tag);

            decimal enteredAmount = 0m;
            if (decimal.TryParse(AmountTextBox?.Text?.Trim(), out var a))
            {
                enteredAmount = a;
            }

            if (enteredAmount > _currentAvailableBalance)
            {
                AvailableBalanceBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fef2f2"));
                AvailableBalanceBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fca5a5"));
                AvailableBalanceTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dc2626"));
                AvailableBalanceLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#991b1b"));
                AvailableBalanceLabel.Text = $"⚠️ رصيد {channelName} غير كافٍ! المتاح:";
                if (AvailableBalanceIcon != null) AvailableBalanceIcon.Text = "⚠️";
            }
            else
            {
                AvailableBalanceBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f0fdf4"));
                AvailableBalanceBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86efac"));
                AvailableBalanceTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803d"));
                AvailableBalanceLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534"));
                AvailableBalanceLabel.Text = $"الرصيد المتاح في {channelName}:";
                if (AvailableBalanceIcon != null) AvailableBalanceIcon.Text = "💳";
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(AmountTextBox.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("يرجى إدخال مبلغ صحيح", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (amount > _supplier.TotalBalance)
            {
                var result = MessageBox.Show($"المبلغ المدفوع ({amount:N0}) أكبر من المديونية الحالية ({_supplier.TotalBalance:N0}). هل تريد المتابعة؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No) return;
            }

            var selectedMethodItemPre = PaymentMethodComboBox?.SelectedItem as ComboBoxItem;
            var methodTagPre = selectedMethodItemPre?.Tag?.ToString() ?? "Cash";

            if (methodTagPre != "Other" && amount > _currentAvailableBalance)
            {
                var channelName = FinancialChannelHelper.GetChannelDisplayName(methodTagPre);
                var shortage = amount - _currentAvailableBalance;
                MessageBox.Show(
                    $"عذراً، رصيد {channelName} غير كافٍ لصرف هذه الدفعة للمورد!\n\n" +
                    $"• المبلغ المطلوب صرفه: {amount:N0} ريال\n" +
                    $"• الرصيد المتاح حالياً: {_currentAvailableBalance:N0} ريال\n" +
                    $"• العجز المالي: {shortage:N0} ريال\n\n" +
                    $"يرجى تعديل المبلغ أو تغذية الرصيد أو اختيار جهة صرف أخرى.",
                    "تنبيه: عدم كفاية الرصيد",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                AmountTextBox.Focus();
                AmountTextBox.SelectAll();
                return;
            }

            try
            {
                var supplierService = POSSystem.WPF.App.ServiceProvider.GetRequiredService<ISupplierService>();

                // 1. Map Payment Method
                var selectedMethodItem = PaymentMethodComboBox?.SelectedItem as ComboBoxItem;
                var methodTag = selectedMethodItem?.Tag?.ToString() ?? "Cash";
                PaymentMethod method = (methodTag.StartsWith("Wallet_") || methodTag == "MobilePayment")
                    ? PaymentMethod.MobilePayment
                    : methodTag switch
                    {
                        "Cash" => PaymentMethod.Cash,
                        "BankTransfer" => PaymentMethod.BankTransfer,
                        "DebitCard" => PaymentMethod.DebitCard,
                        _ => PaymentMethod.Cash
                    };

                // 2. Prepare wallet / reference notes
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
                    "Other" => "خزينة الإدارة (خارج الصندوق)",
                    _ => methodTag.StartsWith("Wallet_") ? methodTag.Substring("Wallet_".Length) : string.Empty
                };

                var refNo = TransactionRefTextBox?.Text?.Trim();
                var noteTags = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrEmpty(walletName) && !finalNotes.Contains(walletName))
                {
                    noteTags.Add($"[طريقة الصرف: {walletName}]");
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

                // 3. Get User
                var user = AdminDashboardWindow._currentUser;
                if (user == null)
                {
                    MessageBox.Show("لم يتم العثور على مستخدم مسجل الدخول.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 4. Resolve active shift only if paying cash from shift drawer
                int? shiftId = null;
                if (method == PaymentMethod.Cash && methodTag != "Other")
                {
                    var shiftService = new Business.Services.ShiftService(POSSystem.WPF.App.ServiceProvider.GetRequiredService<IUnitOfWork>());
                    var activeShift = await shiftService.GetActiveShiftByUserIdAsync(user.Id);
                    shiftId = activeShift?.Id;
                }

                // 5. Record Supplier Payment
                var payment = new SupplierPayment
                {
                    SupplierId = _supplier.Id,
                    Amount = amount,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = method,
                    Notes = finalNotes,
                    UserId = user.Id
                };

                await supplierService.RecordPaymentAsync(payment, shiftId);

                MessageBox.Show("تم تسجيل الدفعة بنجاح وتحديث الرصيد وإصدار سند الصرف", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تسجيل الدفعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
