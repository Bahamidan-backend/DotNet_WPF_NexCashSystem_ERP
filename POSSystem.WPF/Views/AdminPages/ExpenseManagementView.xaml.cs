using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using POSSystem.WPF.Models;
using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.ViewModels.Expenses;
using POSSystem.WPF.ViewModels;
using POSSystem.WPF.Views;
using POSSystem.WPF.Views.Popups;

namespace POSSystem.WPF.Views.AdminPages
{
    public partial class ExpenseManagementView : UserControl
    {
        private ExpenseManagementViewModel _viewModel;

        public ExpenseManagementView() : this(false) { }

        public ExpenseManagementView(bool showVouchersByDefault)
        {
            InitializeComponent();
            _viewModel = new ExpenseManagementViewModel();
            DataContext = _viewModel;
            if (showVouchersByDefault)
            {
                _viewModel.expenseManagementProberty.IsExpensesVisible = false;
                _viewModel.expenseManagementProberty.IsAccountsVisible = false;
                _viewModel.expenseManagementProberty.IsVouchersVisible = true;
            }
        }

        private void AddExpenseToSelectedAccount_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string? targetCategory = _viewModel?.expenseManagementProberty?.SelectedExpenseAccount?.AccountName;
                var expenseControl = new AddEditExpenseControl(_viewModel!, null, targetCategory);
                if (Window.GetWindow(this) is AdminDashboardWindow adminWindow)
                {
                    adminWindow.ShowOverlay(expenseControl);
                }
                else
                {
                    _viewModel?.AddExpenseForSelectedAccount();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح نافذة إضافة المصروف للحساب: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintAccountsSummary_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel?.expenseManagementProberty?.ExpenseAccountsSummary == null ||
                    !_viewModel.expenseManagementProberty.ExpenseAccountsSummary.Any())
                {
                    MessageBox.Show("لا توجد حسابات مصروفات لتصديرها حالياً.", "معلومة",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var accounts = _viewModel.expenseManagementProberty.ExpenseAccountsSummary.ToList();
                var columnHeaders = new[]
                {
                    "اسم الحساب / جهة الصرف",
                    "عدد العمليات",
                    "المنصرف نقداً (من الصندوق)",
                    "المنصرف عبر المحافظ والبنوك",
                    "إجمالي المنصرف للحساب",
                    "النسبة %",
                    "آخر حركة"
                };

                decimal totalSpent = accounts.Sum(a => a.TotalAmount);
                decimal totalCash = accounts.Sum(a => a.CashAmount);
                decimal totalWallet = accounts.Sum(a => a.WalletAmount);
                int totalTx = accounts.Sum(a => a.TransactionsCount);

                string summaryText =
                    $"الفترة: من {_viewModel.expenseManagementProberty.StartDate:yyyy/MM/dd} إلى {_viewModel.expenseManagementProberty.EndDate:yyyy/MM/dd}\n" +
                    $"إجمالي عدد الحسابات: {accounts.Count} حساب (إجمالي العمليات: {totalTx} عملية)\n" +
                    $"إجمالي المنصرف نقداً: {totalCash:#,##0} ريال | عبر المحافظ والبنوك: {totalWallet:#,##0} ريال\n" +
                    $"الإجمالي العام لجميع حسابات المصروفات: {totalSpent:#,##0} ريال";

                Services.PrintService.PrintReport(
                    "كشف وجرد حسابات المصروفات",
                    "Expense Accounts Summary Report",
                    accounts,
                    columnHeaders,
                    (object item) =>
                    {
                        if (item is not ExpenseAccountSummaryItem acc)
                            return new string[columnHeaders.Length];

                        return new[]
                        {
                            acc.AccountName ?? "",
                            acc.TransactionsCount.ToString(),
                            $"{acc.CashAmount:#,##0} ريال",
                            $"{acc.WalletAmount:#,##0} ريال",
                            $"{acc.TotalAmount:#,##0} ريال",
                            acc.PercentageText ?? "0%",
                            acc.LastTransactionDateText ?? ""
                        };
                    },
                    summaryText,
                    preferPdf: true
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting accounts summary: {ex.Message}");
                MessageBox.Show("حدث خطأ أثناء تصدير جرد الحسابات إلى Excel.", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintSelectedAccountDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedAccount = _viewModel?.expenseManagementProberty?.SelectedExpenseAccount;
                if (selectedAccount == null)
                {
                    MessageBox.Show("يرجى اختيار حساب من القائمة اليمنى أولاً لتصدير كشفه التفصيلي.", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var accountExpenses = _viewModel?.expenseManagementProberty?.SelectedAccountExpenses?.ToList() ?? new List<Expense>();
                if (!accountExpenses.Any())
                {
                    MessageBox.Show($"لا توجد عمليات صرف مسجلة في '{selectedAccount.AccountName}' خلال الفترة المحددة لتصديرها.", "معلومة",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var columnHeaders = new[]
                {
                    "التاريخ",
                    "اسم المصروف / البيان",
                    "الحساب",
                    "النوع",
                    "طريقة الدفع / المحفظة",
                    "المبلغ",
                    "المستفيد / المورد",
                    "رقم الفاتورة",
                    "الملاحظات"
                };

                decimal totalAmount = accountExpenses.Sum(x => x.Amount);
                decimal cashAmount = accountExpenses.Where(x => !x.IsWalletExpense).Sum(x => x.Amount);
                decimal walletAmount = accountExpenses.Where(x => x.IsWalletExpense).Sum(x => x.Amount);

                string summaryText =
                    $"اسم الحساب: {selectedAccount.AccountName}\n" +
                    $"عدد العمليات: {accountExpenses.Count} عملية\n" +
                    $"المنصرف نقداً: {cashAmount:#,##0} ريال | عبر المحافظ والبنوك: {walletAmount:#,##0} ريال\n" +
                    $"إجمالي المنصرف في هذا الحساب: {totalAmount:#,##0} ريال";

                Services.PrintService.PrintReport(
                    $"كشف حساب تفصيلي - {selectedAccount.AccountName}",
                    $"Account Statement - {selectedAccount.AccountName}",
                    accountExpenses,
                    columnHeaders,
                    (object item) =>
                    {
                        if (item is not Expense exp)
                            return new string[columnHeaders.Length];

                        return new[]
                        {
                            exp.ExpenseDate != default ? exp.ExpenseDate.ToString("yyyy/MM/dd") : "",
                            exp.Name ?? "",
                            exp.Category ?? selectedAccount.AccountName,
                            exp.TypeText ?? "",
                            exp.PaymentMethodText ?? "نقدي (من الصندوق)",
                            $"{exp.Amount:#,##0} ريال",
                            exp.SupplierName ?? "",
                            exp.InvoiceNumber ?? "",
                            exp.Notes ?? ""
                        };
                    },
                    summaryText,
                    preferPdf: true
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting selected account statement: {ex.Message}");
                MessageBox.Show("حدث خطأ أثناء تصدير كشف الحساب المحدد إلى Excel.", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintReport_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (_viewModel == null) 
                {
                    System.Windows.MessageBox.Show("لا يمكن الوصول إلى البيانات", "خطأ", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                
                List<Expense> expenses = new List<Expense>();
                try
                {
                    if (_viewModel.expenseManagementProberty.Expenses != null)
                    {
                        expenses = _viewModel.expenseManagementProberty.Expenses
                            .Where(e => e != null)
                            .ToList();
                    }
                }
                catch (Exception loadEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading expenses: {loadEx.Message}");
                    expenses = new List<Expense>();
                }
                
                if (expenses.Count == 0)
                {
                    System.Windows.MessageBox.Show("لا توجد بيانات للطباعة", "معلومة", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    return;
                }
                
                var columnHeaders = new[] { "التاريخ", "اسم المصروف", "الحساب / الفئة", "النوع", "طريقة الدفع / المحفظة", "المبلغ", "الملاحظات" };
                
                string summaryText = "";
                try
                {
                    var validExpenses = expenses.Where(e => e != null && e.Amount >= 0).ToList();
                    var totalAmount = validExpenses.Sum(e => e.Amount);
                    summaryText = $"إجمالي المصروفات: {validExpenses.Count}\nإجمالي المبلغ: {totalAmount:#,##0} ريال";
                }
                catch
                {
                    summaryText = $"إجمالي المصروفات: {expenses.Count}";
                }
                
                Services.PrintService.PrintReport(
                    "تقرير المصروفات الشهرية",
                    "Expenses Management Report",
                    expenses,
                    columnHeaders,
                    (object item) => 
                    {
                        if (item is not Expense expense) return new string[columnHeaders.Length];
                        
                        try
                        {
                            string dateStr = expense.ExpenseDate != default ? expense.ExpenseDate.ToString("yyyy/MM/dd") : "";
                            string nameStr = expense.Name ?? "";
                            string categoryStr = expense.Category ?? "";
                            string typeStr = expense.TypeText ?? expense.Type.ToString();
                            string amountStr = expense.Amount >= 0 ? $"{expense.Amount:#,##0} ريال" : "0 ريال";
                            string notesStr = expense.Notes ?? "";
                            
                            return new[]
                            {
                                dateStr,
                                nameStr,
                                categoryStr,
                                typeStr,
                                expense.PaymentMethodText ?? "نقدي (من الصندوق)",
                                amountStr,
                                notesStr
                            };
                        }
                        catch
                        {
                            return new string[columnHeaders.Length];
                        }
                    },
                    summaryText,
                    preferPdf: true
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Print error: {ex.Message}\n{ex.StackTrace}");
                System.Windows.MessageBox.Show("حدث خطأ أثناء محاولة حفظ التقرير. يرجى المحاولة مرة أخرى.", "خطأ", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void AddExpenseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var expenseControl = new AddEditExpenseControl(_viewModel);
                if (Window.GetWindow(this) is AdminDashboardWindow adminWindow)
                {
                    adminWindow.ShowOverlay(expenseControl);
                }
                else
                {
                    MessageBox.Show("خطأ في فتح نافذة إضافة المصروف", "خطأ", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح نافذة إضافة المصروف: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddVoucherButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var voucherControl = new AddEditVoucherControl(_viewModel);
                if (Window.GetWindow(this) is AdminDashboardWindow adminWindow)
                {
                    adminWindow.ShowOverlay(voucherControl);
                }
                else
                {
                    MessageBox.Show("خطأ في فتح نافذة إضافة السند", "خطأ", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح نافذة إضافة السند: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintVoucherReport_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (_viewModel == null) 
                {
                    System.Windows.MessageBox.Show("لا يمكن الوصول إلى البيانات", "خطأ", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                
                List<ReceiptVoucher> vouchers = new List<ReceiptVoucher>();
                try
                {
                    if (_viewModel.expenseManagementProberty.Vouchers != null)
                    {
                        vouchers = _viewModel.expenseManagementProberty.Vouchers
                            .Where(v => v != null)
                            .ToList();
                    }
                }
                catch (Exception loadEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading vouchers: {loadEx.Message}");
                    vouchers = new List<ReceiptVoucher>();
                }
                
                if (vouchers.Count == 0)
                {
                    System.Windows.MessageBox.Show("لا توجد بيانات للطباعة", "معلومة", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    return;
                }
                
                var columnHeaders = new[] { "رقم السند", "النوع", "اسم العميل", "المبلغ", "التاريخ", "الملاحظات" };
                
                string summaryText = "";
                try
                {
                    var validVouchers = vouchers.Where(v => v != null && v.Amount >= 0).ToList();
                    var receipts = validVouchers.Where(v => v.Type == Models.VoucherType.Receipt).Sum(v => v.Amount);
                    var payments = validVouchers.Where(v => v.Type == Models.VoucherType.Payment).Sum(v => v.Amount);
                    summaryText = $"إجمالي السندات: {validVouchers.Count}\nإجمالي القبض: {receipts:#,##0} ريال\nإجمالي الصرف: {payments:#,##0} ريال\nصافي المبلغ: {(receipts - payments):#,##0} ريال";
                }
                catch
                {
                    summaryText = $"إجمالي السندات: {vouchers.Count}";
                }
                
                Services.PrintService.PrintReport(
                    "تقرير السندات",
                    "Vouchers Management Report",
                    vouchers,
                    columnHeaders,
                    (object item) => 
                    {
                        if (item is not ReceiptVoucher voucher) return new string[columnHeaders.Length];
                        
                        try
                        {
                            string voucherNumberStr = voucher.VoucherNumber ?? "";
                            string typeStr = voucher.TypeText ?? voucher.Type.ToString();
                            string customerStr = voucher.CustomerName ?? "";
                            string amountStr = voucher.Amount >= 0 ? $"{voucher.Amount:#,##0} ريال" : "0.00 ريال";
                            string dateStr = voucher.VoucherDate != default ? voucher.VoucherDate.ToString("yyyy/MM/dd") : "";
                            string notesStr = voucher.Notes ?? "";
                            
                            return new[]
                            {
                                voucherNumberStr,
                                typeStr,
                                customerStr,
                                amountStr,
                                dateStr,
                                notesStr
                            };
                        }
                        catch
                        {
                            return new string[columnHeaders.Length];
                        }
                    },
                    summaryText,
                    preferPdf: true
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Print voucher error: {ex.Message}\n{ex.StackTrace}");
                System.Windows.MessageBox.Show("حدث خطأ أثناء محاولة حفظ التقرير. يرجى المحاولة مرة أخرى.", "خطأ", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
