using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.WPF.Business;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Views.Popups
{
    public partial class PaySalaryPopup : Window
    {
        private readonly IServiceScope _scope;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmployeeService _employeeService;
        private readonly List<EmployeeSalary> _salariesToPay;
        private decimal _totalNetRequired;
        private decimal _currentAvailableBalance;

        public class PayoutChannelOption
        {
            public string ChannelTag { get; set; } = "Cash";
            public string DisplayText { get; set; } = string.Empty;
            public string? WalletName { get; set; }
            public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        }

        public class ShiftDisplayItem
        {
            public int ShiftId { get; set; }
            public string DisplayText { get; set; } = string.Empty;
            public decimal Balance { get; set; }
        }

        public PaySalaryPopup(EmployeeSalary salary) : this(new List<EmployeeSalary> { salary })
        {
        }

        public PaySalaryPopup(List<EmployeeSalary> salaries)
        {
            InitializeComponent();

            _salariesToPay = salaries ?? new List<EmployeeSalary>();
            _scope = App.ServiceProvider.CreateScope();
            var context = _scope.ServiceProvider.GetRequiredService<POSDbContext>();
            _unitOfWork = new UnitOfWork(context);
            _employeeService = new EmployeeService(_unitOfWork);

            Closed += (_, _) =>
            {
                _unitOfWork.Dispose();
                _scope.Dispose();
            };

            InitializeSummary();
            LoadPayoutChannels();
            LoadActiveShifts();
        }

        private void InitializeSummary()
        {
            decimal totalBase = _salariesToPay.Sum(s => s.BaseSalary);
            decimal totalDed = _salariesToPay.Sum(s => s.AdvanceDeduction);
            _totalNetRequired = _salariesToPay.Sum(s => s.NetSalary);

            if (_salariesToPay.Count == 1)
            {
                var sal = _salariesToPay[0];
                string empName = sal.Employee?.Name ?? "موظف";
                TitleTextBlock.Text = $"💵 صرف راتب: {empName}";
                EmployeeNameTextBlock.Text = $"الموظف: {empName} (شهر {sal.SalaryDate:yyyy/MM})";
            }
            else
            {
                TitleTextBlock.Text = $"💵 صرف مرتبات ({_salariesToPay.Count} موظفين)";
                EmployeeNameTextBlock.Text = $"إجمالي الموظفين المحددين للصرف: {_salariesToPay.Count} موظف";
            }

            BaseSalaryTextBlock.Text = $"{totalBase:N0} ريال";
            AdvanceDeductionTextBlock.Text = $"{totalDed:N0} ريال";
            NetSalaryTextBlock.Text = $"{_totalNetRequired:N0} ريال";
        }

        private void LoadPayoutChannels()
        {
            var options = new List<PayoutChannelOption>
            {
                new PayoutChannelOption
                {
                    ChannelTag = "Cash",
                    DisplayText = "💵 نقداً من صندوق الكاشير",
                    WalletName = null,
                    PaymentMethod = PaymentMethod.Cash
                }
            };

            var wallets = FinancialChannelHelper.GetActiveWalletsList();
            foreach (var w in wallets)
            {
                if (string.IsNullOrWhiteSpace(w)) continue;
                var clean = w.Trim();
                options.Add(new PayoutChannelOption
                {
                    ChannelTag = FinancialChannelHelper.NormalizeChannelKey(clean),
                    DisplayText = $"🏦 محفظة / حساب: {clean}",
                    WalletName = clean,
                    PaymentMethod = PaymentMethod.MobilePayment
                });
            }

            PayoutChannelComboBox.ItemsSource = options;
            PayoutChannelComboBox.SelectedIndex = 0;
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
                    shiftItems.Add(new ShiftDisplayItem
                    {
                        ShiftId = shift.Id,
                        DisplayText = $"مناوبة #{shift.ShiftNumber ?? shift.Id.ToString()} - {cashierName} (الرصيد: {balance:N0} ريال)",
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
                    DrawerBalanceHintTextBlock.Text = "تنبيه: لا توجد مناوبة كاشير نشطة حالياً. يمكنك اختيار محفظة إلكترونية للصرف مباشرة.";
                    DrawerBalanceHintTextBlock.Foreground = Brushes.OrangeRed;
                }

                await UpdateAvailableBalanceAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading active shifts in PaySalaryPopup: {ex.Message}");
            }
        }

        private void PayoutChannelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TargetShiftBorder == null) return;

            if (PayoutChannelComboBox.SelectedItem is PayoutChannelOption opt && opt.PaymentMethod == PaymentMethod.Cash)
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
            _ = UpdateAvailableBalanceAsync();
        }

        private async Task UpdateAvailableBalanceAsync()
        {
            try
            {
                if (AvailableBalanceBorder == null || AvailableBalanceTextBlock == null) return;
                if (PayoutChannelComboBox.SelectedItem is not PayoutChannelOption selectedOpt) return;

                AvailableBalanceTextBlock.Text = "جاري التحميل...";
                int? shiftId = null;
                if (selectedOpt.PaymentMethod == PaymentMethod.Cash && TargetShiftComboBox.SelectedItem is ShiftDisplayItem shiftItem)
                {
                    shiftId = shiftItem.ShiftId;
                }

                string lookupKey = !string.IsNullOrWhiteSpace(selectedOpt.WalletName) ? selectedOpt.WalletName : selectedOpt.ChannelTag;
                decimal balance = await ServiceLocator.FinancialChannelService.GetAvailableBalanceAsync(lookupKey, shiftId);
                _currentAvailableBalance = balance;
                AvailableBalanceTextBlock.Text = $"{balance:N0} ريال";

                string channelDisplay = !string.IsNullOrWhiteSpace(selectedOpt.WalletName)
                    ? selectedOpt.WalletName
                    : "صندوق الكاشير";

                if (_totalNetRequired > _currentAvailableBalance)
                {
                    AvailableBalanceBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fef2f2"));
                    AvailableBalanceBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fca5a5"));
                    AvailableBalanceTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dc2626"));
                    AvailableBalanceLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#991b1b"));
                    AvailableBalanceLabel.Text = $"⚠️ رصيد {channelDisplay} غير كافٍ! المتاح:";
                    AvailableBalanceIcon.Text = "⚠️";
                }
                else
                {
                    AvailableBalanceBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f0fdf4"));
                    AvailableBalanceBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86efac"));
                    AvailableBalanceTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803d"));
                    AvailableBalanceLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534"));
                    AvailableBalanceLabel.Text = $"الرصيد المتاح في {channelDisplay}:";
                    AvailableBalanceIcon.Text = "💳";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating balance in PaySalaryPopup: {ex.Message}");
            }
        }

        private async void ConfirmPayButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PayoutChannelComboBox.SelectedItem is not PayoutChannelOption selectedOpt)
                {
                    MessageBox.Show("يرجى اختيار جهة الصرف أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int? targetShiftId = null;
                if (selectedOpt.PaymentMethod == PaymentMethod.Cash)
                {
                    if (TargetShiftComboBox.SelectedItem is ShiftDisplayItem shiftItem)
                    {
                        targetShiftId = shiftItem.ShiftId;
                    }
                    else if (_totalNetRequired > 0)
                    {
                        MessageBox.Show("لا توجد مناوبة كاشير نشطة للصرف نقداً. يرجى اختيار محفظة إلكترونية أو فتح مناوبة كاشير.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                await UpdateAvailableBalanceAsync();
                if (_totalNetRequired > _currentAvailableBalance)
                {
                    string chName = !string.IsNullOrWhiteSpace(selectedOpt.WalletName) ? selectedOpt.WalletName : "صندوق الكاشير";
                    decimal shortage = _totalNetRequired - _currentAvailableBalance;
                    MessageBox.Show(
                        $"عذراً، الرصيد المتاح في ({chName}) غير كافٍ لصرف الراتب!\n\n" +
                        $"• صافي الراتب المطلوب صرفه: {_totalNetRequired:N0} ريال\n" +
                        $"• الرصيد المتاح حالياً: {_currentAvailableBalance:N0} ريال\n" +
                        $"• مقدار العجز: {shortage:N0} ريال\n\n" +
                        "يرجى اختيار محفظة أخرى بها رصيد كافٍ أو تغذية الرصيد أولاً.",
                        "عدم كفاية الرصيد",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                string? extraNotes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();

                foreach (var sal in _salariesToPay)
                {
                    await _employeeService.MarkSalaryAsPaidAsync(
                        sal.Id,
                        selectedOpt.PaymentMethod,
                        targetShiftId,
                        selectedOpt.WalletName,
                        extraNotes);
                    sal.IsPaid = true;
                }

                string sourceDisplay = !string.IsNullOrWhiteSpace(selectedOpt.WalletName)
                    ? $"محفظة ({selectedOpt.WalletName})"
                    : "صندوق الكاشير (نقداً)";

                MessageBox.Show(
                    $"تم صرف الراتب بنجاح من {sourceDisplay}، وتم قيده تلقائياً في (حساب رواتب وموظفين) ضمن كشف وجرد حسابات المصروفات.",
                    "تم صرف الراتب",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء صرف الراتب:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
