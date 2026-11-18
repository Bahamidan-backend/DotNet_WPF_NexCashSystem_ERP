using POSSystem.WPF.Models;
using POSSystem.WPF.Data;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Business;
using POSSystem.WPF.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace POSSystem.WPF.Views.Popups
{
    public partial class AddEmployeeAdvancePopup : Window
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmployeeService _employeeService;
        private User? _selectedEmployee;
        private readonly IServiceScope _scope;
        private decimal _currentAvailableBalance = 0m;

        public class ShiftDisplayItem
        {
            public int ShiftId { get; set; }
            public string DisplayText { get; set; } = string.Empty;
            public decimal Balance { get; set; }
        }

        public AddEmployeeAdvancePopup(User? selectedEmployee = null)
        {
            InitializeComponent();
            
            _scope = App.ServiceProvider.CreateScope();
            var context = _scope.ServiceProvider.GetRequiredService<POSDbContext>();
            _unitOfWork = new UnitOfWork(context);
            _employeeService = new EmployeeService(_unitOfWork);
            Closed += (_, _) => 
            {
                _unitOfWork.Dispose();
                _scope.Dispose();
            };
            
            _selectedEmployee = selectedEmployee;
            
            LoadEmployees();
            LoadActiveShifts();
            
            // Set default values
            RepaymentMethodComboBox.SelectedIndex = 0;
            AdvanceDatePicker.SelectedDate = DateTime.Now;

            _ = UpdateAvailableBalanceAsync();
        }

        private async void LoadEmployees()
        {
            try
            {
                var employees = await _employeeService.GetAllEmployeesAsync();
                EmployeeComboBox.ItemsSource = employees.Where(e => e.IsActive).ToList();
                
                if (_selectedEmployee != null)
                {
                    EmployeeComboBox.SelectedValue = _selectedEmployee.Id;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الموظفين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadActiveShifts()
        {
            try
            {
                var context = _unitOfWork.GetContext();
                var activeShifts = await context.Shifts
                    .AsNoTracking()
                    .Where(s => s.IsActive)
                    .Include(s => s.User)
                    .OrderByDescending(s => s.StartTime)
                    .ToListAsync();

                var shiftItems = new List<ShiftDisplayItem>();

                foreach (var shift in activeShifts)
                {
                    var netTransactions = (decimal)(await context.CashBoxTransactions
                        .AsNoTracking()
                        .Where(t => t.ShiftId == shift.Id)
                        .SumAsync(t => t.Type == CashBoxTransactionType.Income ? (double)t.Amount : -(double)t.Amount));

                    decimal balance = shift.StartingCash + netTransactions;
                    string cashierName = shift.User?.Name ?? $"كاشير #{shift.UserId}";
                    string displayText = $"مناوبة #{shift.ShiftNumber ?? shift.Id.ToString()} - {cashierName} (الرصيد: {balance:N0} ريال)";

                    shiftItems.Add(new ShiftDisplayItem
                    {
                        ShiftId = shift.Id,
                        DisplayText = displayText,
                        Balance = balance
                    });
                }

                TargetShiftComboBox.ItemsSource = shiftItems;
                if (shiftItems.Count > 0)
                {
                    TargetShiftComboBox.SelectedIndex = 0;
                }
                else
                {
                    DrawerBalanceHintTextBlock.Text = "تنبيه: لا توجد مناوبات كاشير نشطة حالياً. يرجى اختيار طريقة دفع أخرى (تحويل/محفظة) أو فتح مناوبة.";
                    DrawerBalanceHintTextBlock.Foreground = System.Windows.Media.Brushes.OrangeRed;
                }

                _ = UpdateAvailableBalanceAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading active shifts: {ex.Message}");
            }
        }

        private void PaymentMethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TargetShiftBorder == null) return;

            var selectedItem = PaymentMethodComboBox?.SelectedItem as ComboBoxItem;
            var methodTag = selectedItem?.Tag?.ToString() ?? "Cash";

            if (methodTag == "Cash")
            {
                TargetShiftBorder.Visibility = Visibility.Visible;
            }
            else
            {
                TargetShiftBorder.Visibility = Visibility.Collapsed;
            }

            _ = UpdateAvailableBalanceAsync();
        }

        private void TargetShiftComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TargetShiftComboBox.SelectedItem is ShiftDisplayItem item)
            {
                DrawerBalanceHintTextBlock.Text = $"الرصيد النقدي المتوفر بالدرج: {item.Balance:N0} ريال.";
                DrawerBalanceHintTextBlock.Foreground = item.Balance > 0 ? System.Windows.Media.Brushes.DarkGreen : System.Windows.Media.Brushes.Red;
            }
            _ = UpdateAvailableBalanceAsync();
        }

        private void AmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateAmountAgainstBalance();
        }

        public async Task UpdateAvailableBalanceAsync()
        {
            try
            {
                if (AvailableBalanceBorder == null || AvailableBalanceTextBlock == null) return;

                var selectedItem = PaymentMethodComboBox?.SelectedItem as ComboBoxItem;
                var tag = selectedItem?.Tag?.ToString() ?? "Cash";
                var channelName = FinancialChannelHelper.GetChannelDisplayName(tag);

                AvailableBalanceTextBlock.Text = "جاري التحميل...";

                int? shiftId = null;
                if (tag == "Cash")
                {
                    if (TargetShiftComboBox?.SelectedItem is ShiftDisplayItem shiftItem)
                    {
                        shiftId = shiftItem.ShiftId;
                    }
                }

                decimal balance = await ServiceLocator.FinancialChannelService.GetAvailableBalanceAsync(tag, shiftId);
                _currentAvailableBalance = balance;
                AvailableBalanceTextBlock.Text = $"{balance:N0} ريال";

                ValidateAmountAgainstBalance();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating advance available balance: {ex.Message}");
            }
        }

        private void ValidateAmountAgainstBalance()
        {
            if (AvailableBalanceBorder == null || AvailableBalanceTextBlock == null || AvailableBalanceLabel == null) return;

            var selectedItem = PaymentMethodComboBox?.SelectedItem as ComboBoxItem;
            var tag = selectedItem?.Tag?.ToString() ?? "Cash";
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
            try
            {
                // Validate inputs
                if (EmployeeComboBox.SelectedValue == null)
                {
                    MessageBox.Show("يرجى اختيار موظف", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(AmountTextBox.Text, out var amount) || amount <= 0)
                {
                    MessageBox.Show("يرجى إدخال مبلغ صحيح أكبر من الصفر", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (amount > _currentAvailableBalance)
                {
                    var selectedPayout = (ComboBoxItem)PaymentMethodComboBox.SelectedItem;
                    var pTag = selectedPayout?.Tag?.ToString() ?? "Cash";
                    var chName = FinancialChannelHelper.GetChannelDisplayName(pTag);
                    var shortage = amount - _currentAvailableBalance;
                    MessageBox.Show(
                        $"عذراً، رصيد {chName} غير كافٍ لصرف هذه السلفة!\n\n" +
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

                if (RepaymentMethodComboBox.SelectedItem == null)
                {
                    MessageBox.Show("يرجى اختيار طريقة السداد", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (AdvanceDatePicker.SelectedDate == null)
                {
                    MessageBox.Show("يرجى اختيار تاريخ السلفية", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedRepayItem = (ComboBoxItem)RepaymentMethodComboBox.SelectedItem;
                var repaymentMethod = Enum.Parse<AdvanceRepaymentMethod>(selectedRepayItem.Tag.ToString()!);

                // Payout source
                var selectedPayoutItem = (ComboBoxItem)PaymentMethodComboBox.SelectedItem;
                var payoutTag = selectedPayoutItem?.Tag?.ToString() ?? "Cash";
                var paymentMethod = (payoutTag.StartsWith("Wallet_") || payoutTag == "MobilePayment")
                    ? PaymentMethod.MobilePayment
                    : payoutTag switch
                    {
                        "Cash" => PaymentMethod.Cash,
                        "BankTransfer" => PaymentMethod.BankTransfer,
                        "DebitCard" => PaymentMethod.DebitCard,
                        _ => PaymentMethod.Cash
                    };

                // Prepare wallet note if applicable
                string walletName = payoutTag switch
                {
                    "Wallet_Kuraimi" => "حاسب كريمي",
                    "Wallet_Jeeb" => "محفظة جيب",
                    "Wallet_Floosak" => "محفظة فلوسك",
                    "Wallet_OneCash" => "محفظة ون كاش",
                    "Wallet_Jawali" => "محفظة جوالي",
                    "Wallet_MobileMoney" => "محفظة موبايل موني",
                    "Wallet_YemenWallet" => "محفظة يمن والت",
                    _ => payoutTag.StartsWith("Wallet_") ? payoutTag.Substring("Wallet_".Length) : string.Empty
                };

                string? finalNotes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(walletName))
                {
                    var tag = $"[محفظة: {walletName}]";
                    finalNotes = string.IsNullOrEmpty(finalNotes) ? tag : $"{finalNotes} {tag}";
                }

                int? targetShiftId = null;
                if (paymentMethod == PaymentMethod.Cash)
                {
                    if (TargetShiftComboBox.SelectedValue is int sId && sId > 0)
                    {
                        targetShiftId = sId;
                    }
                    else if (TargetShiftComboBox.Items.Count > 0)
                    {
                        MessageBox.Show("يرجى اختيار صندوق المناوبة لصرف السلفة نقداً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Create advance
                var advance = new EmployeeAdvance
                {
                    EmployeeId = (int)EmployeeComboBox.SelectedValue,
                    Amount = amount,
                    PaymentMethod = paymentMethod,
                    TargetShiftId = targetShiftId,
                    AdvanceDate = AdvanceDatePicker.SelectedDate.Value,
                    RepaymentMethod = repaymentMethod,
                    RepaymentDate = null,
                    IsRepaid = false,
                    Notes = finalNotes,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _employeeService.CreateAdvanceAsync(advance);
                
                MessageBox.Show("تم إضافة وصرف السلفية بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إضافة السلفية:\n{ex.Message}", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
