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
    public partial class EmployeeStatementPopup : Window
    {
        private readonly IEmployeeService _employeeService;
        private readonly int _employeeId;
        private readonly string _employeeName;
        private readonly User? _employee;
        private List<EmployeeLedgerEntry> _ledgerEntries = new();

        public EmployeeStatementPopup(User employee)
            : this(employee.Id, employee.Name, employee)
        {
        }

        public EmployeeStatementPopup(int employeeId, string employeeName, User? employee = null)
        {
            InitializeComponent();
            _employeeId = employeeId;
            _employeeName = employeeName ?? "موظف غير محدد";
            _employee = employee;

            var sp = POSSystem.WPF.App.ServiceProvider;
            _employeeService = sp.GetRequiredService<IEmployeeService>();

            UpdateEmployeeHeaderInfo();

            Loaded += async (s, e) => await LoadStatementAsync();
        }

        private void UpdateEmployeeHeaderInfo()
        {
            string job = !string.IsNullOrWhiteSpace(_employee?.JobTitle) ? $" | المسمى: {_employee.JobTitle}" : "";
            string salary = _employee?.BaseSalary > 0 ? $" | الراتب: {_employee.BaseSalary:N0} ريال" : "";
            string hire = _employee?.HireDate.HasValue == true ? $" | تاريخ التعيين: {_employee.HireDate.Value:yyyy/MM/dd}" : "";
            string phone = !string.IsNullOrWhiteSpace(_employee?.Phone) ? $" | الهاتف: {_employee.Phone}" : "";

            EmployeeInfoTextBlock.Text = $"الموظف: {_employeeName}{job}{salary}{hire}{phone}";
        }

        private async Task LoadStatementAsync()
        {
            try
            {
                DateTime? startDate = StartDatePicker.SelectedDate;
                DateTime? endDate = EndDatePicker.SelectedDate;

                var entries = await _employeeService.GetEmployeeStatementAsync(_employeeId, startDate, endDate);
                _ledgerEntries = entries ?? new List<EmployeeLedgerEntry>();
                LedgerDataGrid.ItemsSource = _ledgerEntries;

                if (_ledgerEntries.Any())
                {
                    EmptyStateTextBlock.Visibility = Visibility.Collapsed;

                    // Non-opening balance entries for period totals
                    var nonOpeningEntries = _ledgerEntries.Where(e => e.TransactionType != "رصيد سابق (افتتاحي)").ToList();
                    decimal totalDebit = nonOpeningEntries.Sum(e => e.Debit);
                    decimal totalCredit = nonOpeningEntries.Sum(e => e.Credit);
                    decimal currentBalance = _ledgerEntries.First().Balance; // First entry is the latest after reverse

                    TotalDebitTextBlock.Text = $"{totalDebit:N0} ريال";
                    TotalCreditTextBlock.Text = $"{totalCredit:N0} ريال";

                    if (currentBalance > 0)
                    {
                        NetBalanceTitleTextBlock.Text = "صافي الرصيد الحالي (له مستحق)";
                        NetBalanceTitleTextBlock.Foreground = (Brush)new BrushConverter().ConvertFrom("#16A34A")!;
                        NetBalanceTextBlock.Text = $"{currentBalance:N0} ريال (له)";
                        NetBalanceTextBlock.Foreground = (Brush)new BrushConverter().ConvertFrom("#16A34A")!;
                        BalanceCardBorder.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#86EFAC")!;
                    }
                    else if (currentBalance < 0)
                    {
                        NetBalanceTitleTextBlock.Text = "صافي الرصيد الحالي (عليه سلف)";
                        NetBalanceTitleTextBlock.Foreground = (Brush)new BrushConverter().ConvertFrom("#DC2626")!;
                        NetBalanceTextBlock.Text = $"{Math.Abs(currentBalance):N0} ريال (عليه)";
                        NetBalanceTextBlock.Foreground = (Brush)new BrushConverter().ConvertFrom("#DC2626")!;
                        BalanceCardBorder.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#FECACA")!;
                    }
                    else
                    {
                        NetBalanceTitleTextBlock.Text = "صافي الرصيد الحالي (خالص)";
                        NetBalanceTitleTextBlock.Foreground = (Brush)new BrushConverter().ConvertFrom("#0369A1")!;
                        NetBalanceTextBlock.Text = "0.00 ريال (خالص)";
                        NetBalanceTextBlock.Foreground = (Brush)new BrushConverter().ConvertFrom("#0369A1")!;
                        BalanceCardBorder.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#BAE6FD")!;
                    }
                }
                else
                {
                    EmptyStateTextBlock.Visibility = Visibility.Visible;
                    TotalDebitTextBlock.Text = "0.00 ريال";
                    TotalCreditTextBlock.Text = "0.00 ريال";
                    NetBalanceTextBlock.Text = "0.00 ريال";
                    NetBalanceTitleTextBlock.Text = "صافي الرصيد المستحق الحالي";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحميل كشف الحساب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadStatementAsync();
        }

        private async void ResetFilterButton_Click(object sender, RoutedEventArgs e)
        {
            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;
            await LoadStatementAsync();
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_ledgerEntries == null || _ledgerEntries.Count == 0)
                {
                    MessageBox.Show("لا توجد حركات في كشف حساب الموظف لتصديرها.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var columnHeaders = new[] { "التاريخ والوقت", "نوع الحركة", "رقم المرجع / المستند", "مدين (- على الموظف)", "دائن (+ للموظف)", "طريقة الصرف", "الرصيد التراكمي", "ملاحظات" };
                var nonOpeningEntries = _ledgerEntries.Where(x => x.TransactionType != "رصيد سابق (افتتاحي)").ToList();
                decimal totalDebit = nonOpeningEntries.Sum(x => x.Debit);
                decimal totalCredit = nonOpeningEntries.Sum(x => x.Credit);
                decimal currentBalance = _ledgerEntries.First().Balance;
                string balanceStatus = currentBalance > 0 ? $"{currentBalance:N0} ريال (له مستحق)" : (currentBalance < 0 ? $"{Math.Abs(currentBalance):N0} ريال (عليه سلف)" : "0 ريال (خالص)");

                string summary = $"إجمالي الرواتب والاستحقاقات (دائن + للموظف): {totalCredit:N0} ريال" + Environment.NewLine +
                                 $"إجمالي المنصرف والمسحوبات (مدين - على الموظف): {totalDebit:N0} ريال" + Environment.NewLine +
                                 $"صافي الرصيد الحالي: {balanceStatus}";

                POSSystem.WPF.Services.PrintService.PrintReport(
                    $"كشف حساب موظف - {_employeeName}",
                    EmployeeInfoTextBlock.Text,
                    _ledgerEntries,
                    columnHeaders,
                    (object item) =>
                    {
                        if (item is not EmployeeLedgerEntry entry) return new string[columnHeaders.Length];
                        return new[]
                        {
                            entry.Date.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture),
                            entry.TransactionType ?? "",
                            entry.ReferenceNumber ?? "",
                            entry.Debit.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                            entry.Credit.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                            entry.PayoutMethod ?? "-",
                            entry.Balance.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                            entry.Notes ?? ""
                        };
                    },
                    summary,
                    preferPdf: true
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"تعذر تصدير كشف الحساب إلى إكسل: {ex.Message}", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LedgerDataGrid_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Shift)
            {
                var scrollViewer = FindVisualChild<System.Windows.Controls.ScrollViewer>(LedgerDataGrid);
                if (scrollViewer != null)
                {
                    if (e.Delta < 0)
                        scrollViewer.LineRight();
                    else
                        scrollViewer.LineLeft();
                    e.Handled = true;
                }
            }
        }

        private static T? FindVisualChild<T>(System.Windows.DependencyObject? parent) where T : System.Windows.DependencyObject
        {
            if (parent == null) return null;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild) return typedChild;
                var descendant = FindVisualChild<T>(child);
                if (descendant != null) return descendant;
            }
            return null;
        }
    }
}

