using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using POSSystem.WPF.Business;
using POSSystem.WPF.Models;
using POSSystem.WPF.Services;

namespace POSSystem.WPF.Views.AdminPages
{
    public partial class FinancialAccountsView : UserControl, ISearchable
    {
        private List<FinancialAccount> _accounts = new();
        private FinancialAccount? _selectedStatementAccount;
        private FinancialAccount? _editingAccount;
        private string _currentModalMode = ""; // CreateAccount, EditAccount, Deposit, Withdraw, Transfer, OwnerDeposit
        private List<FinancialAccountTransaction> _currentStatementRows = new();

        public FinancialAccountsView()
        {
            InitializeComponent();
            StatementFromDate.SelectedDate = DateTime.Today.AddMonths(-1);
            StatementToDate.SelectedDate = DateTime.Today;
            Loaded += async (s, e) => await LoadAccountsAsync();
        }

        public async Task LoadAccountsAsync()
        {
            try
            {
                _accounts = await ServiceLocator.FinancialAccountService.GetAllAccountsAsync(includeInactive: true);
                AccountsItemsControl.ItemsSource = _accounts;

                decimal walletsTotal = _accounts
                    .Where(a => a.IsActive && a.AccountType != FinancialAccountType.MainCashBox)
                    .Sum(a => a.CurrentBalance);
                int activeWalletsCount = _accounts.Count(a => a.IsActive && a.AccountType != FinancialAccountType.MainCashBox);

                decimal cashBoxTotal = _accounts
                    .Where(a => a.IsActive && a.AccountType == FinancialAccountType.MainCashBox)
                    .Sum(a => a.CurrentBalance);

                decimal grandTotal = walletsTotal + cashBoxTotal;

                if (TotalWalletsBalanceText != null)
                    TotalWalletsBalanceText.Text = $"{walletsTotal:N0} ريال";
                if (TotalWalletsCountText != null)
                    TotalWalletsCountText.Text = $"مجموع الأرصدة في ({activeWalletsCount}) محفظة وحساب نشط";
                if (MainCashBoxBalanceText != null)
                    MainCashBoxBalanceText.Text = $"{cashBoxTotal:N0} ريال";
                if (GrandTotalLiquidityText != null)
                    GrandTotalLiquidityText.Text = $"{grandTotal:N0} ريال";

                var activeList = _accounts.Where(a => a.IsActive).ToList();
                FromAccountCombo.ItemsSource = activeList;
                ToAccountCombo.ItemsSource = activeList;

                if (_selectedStatementAccount != null && StatementModalOverlay.Visibility == Visibility.Visible)
                {
                    await LoadStatementAsync(_selectedStatementAccount);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الحسابات المالية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        
        public void ApplySearch(string query)
        {
            if (AccountsItemsControl == null) return;
            var q = query?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(q))
            {
                AccountsItemsControl.ItemsSource = _accounts;
            }
            else
            {
                AccountsItemsControl.ItemsSource = _accounts
                    .Where(a =>
                        (a.AccountName != null && a.AccountName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                        (a.BankOrWalletName != null && a.BankOrWalletName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                        (a.AccountNumber != null && a.AccountNumber.Contains(q, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadAccountsAsync();
        }

        private void AddAccount_Click(object sender, RoutedEventArgs e)
        {
            if (!PermissionGuard.EnsurePermission(AdminDashboardWindow._currentUser, SystemPermissions.FinancialAccounts_Manage, "إنشاء حساب مالي جديد"))
                return;

            _currentModalMode = "CreateAccount";
            _editingAccount = null;
            ModalTitleText.Text = "➕ إنشاء حساب مالي / بنك / محفظة جديدة";
            AccountEditFieldsPanel.Visibility = Visibility.Visible;
            TransactionFieldsPanel.Visibility = Visibility.Collapsed;

            AccNameBox.Text = "";
            AccTypeCombo.SelectedIndex = 1;
            AccBankNameBox.Text = "";
            AccNumberBox.Text = "";
            AccOpeningBalanceLabel.Visibility = Visibility.Visible;
            AccOpeningBalanceBox.Visibility = Visibility.Visible;
            AccOpeningBalanceBox.Text = "0";

            ModalOverlay.Visibility = Visibility.Visible;
        }

        private void EditCard_Click(object sender, RoutedEventArgs e)
        {
            if (!PermissionGuard.EnsurePermission(AdminDashboardWindow._currentUser, SystemPermissions.FinancialAccounts_Manage, "تعديل بيانات الحساب المالي"))
                return;

            if (sender is Button btn && btn.Tag is FinancialAccount acc)
            {
                _currentModalMode = "EditAccount";
                _editingAccount = acc;
                ModalTitleText.Text = $"✏️ تعديل بيانات الحساب: {acc.AccountName}";
                AccountEditFieldsPanel.Visibility = Visibility.Visible;
                TransactionFieldsPanel.Visibility = Visibility.Collapsed;

                AccNameBox.Text = acc.AccountName;
                AccTypeCombo.SelectedIndex = (int)acc.AccountType;
                AccBankNameBox.Text = acc.BankOrWalletName;
                AccNumberBox.Text = acc.AccountNumber ?? "";
                AccOpeningBalanceLabel.Visibility = Visibility.Collapsed;
                AccOpeningBalanceBox.Visibility = Visibility.Collapsed;

                ModalOverlay.Visibility = Visibility.Visible;
            }
        }

        private async void ToggleCard_Click(object sender, RoutedEventArgs e)
        {
            if (!PermissionGuard.EnsurePermission(AdminDashboardWindow._currentUser, SystemPermissions.FinancialAccounts_Manage, "تفعيل أو تعطيل الحساب المالي"))
                return;

            if (sender is Button btn && btn.Tag is FinancialAccount acc)
            {
                string actionWord = acc.IsActive ? "تعطيل" : "تفعيل";
                if (MessageBox.Show($"هل أنت متأكد من {actionWord} الحساب '{acc.AccountName}'؟\n(يتم الاحتفاظ بكامل السجل التاريخي للحركات ولا يُحذف الحساب)",
                    "تأكيد حالة الحساب", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        var user = AdminDashboardWindow._currentUser;
                        await ServiceLocator.FinancialAccountService.ToggleAccountStatusAsync(acc.Id, !acc.IsActive, user?.Id, user?.Name);
                        await LoadAccountsAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"تعذر تغيير حالة الحساب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void DepositCard_Click(object sender, RoutedEventArgs e)
        {
            if (!PermissionGuard.EnsurePermission(AdminDashboardWindow._currentUser, SystemPermissions.FinancialAccounts_Deposit, "تسجيل إيداع مالي"))
                return;

            if (sender is Button btn && btn.Tag is FinancialAccount acc)
            {
                _currentModalMode = "Deposit";
                ModalTitleText.Text = $"➕ تسجيل إيداع في: {acc.AccountName}";
                AccountEditFieldsPanel.Visibility = Visibility.Collapsed;
                TransactionFieldsPanel.Visibility = Visibility.Visible;
                FromAccountLabel.Text = "الحساب المستلم للإيداع *";
                FromAccountCombo.SelectedValue = acc.Id;
                ToAccountPanel.Visibility = Visibility.Collapsed;
                TxAmountBox.Text = "";
                TxRefBox.Text = "";
                TxDescBox.Text = $"إيداع في {acc.AccountName}";
                TxNotesBox.Text = "";
                ModalOverlay.Visibility = Visibility.Visible;
            }
        }

        private void WithdrawCard_Click(object sender, RoutedEventArgs e)
        {
            if (!PermissionGuard.EnsurePermission(AdminDashboardWindow._currentUser, SystemPermissions.FinancialAccounts_Withdraw, "تسجيل سحب مالي"))
                return;

            if (sender is Button btn && btn.Tag is FinancialAccount acc)
            {
                _currentModalMode = "Withdraw";
                ModalTitleText.Text = $"➖ تسجيل سحب من: {acc.AccountName}";
                AccountEditFieldsPanel.Visibility = Visibility.Collapsed;
                TransactionFieldsPanel.Visibility = Visibility.Visible;
                FromAccountLabel.Text = "الحساب المسحوب منه *";
                FromAccountCombo.SelectedValue = acc.Id;
                ToAccountPanel.Visibility = Visibility.Collapsed;
                TxAmountBox.Text = "";
                TxRefBox.Text = "";
                TxDescBox.Text = $"سحب من {acc.AccountName}";
                TxNotesBox.Text = "";
                ModalOverlay.Visibility = Visibility.Visible;
            }
        }

        private void Transfer_Click(object sender, RoutedEventArgs e)
        {
            if (!PermissionGuard.EnsurePermission(AdminDashboardWindow._currentUser, SystemPermissions.FinancialAccounts_Transfer, "تحويل الأموال بين الحسابات"))
                return;

            _currentModalMode = "Transfer";
            ModalTitleText.Text = "🔄 تحويل أموال بين الحسابات";
            AccountEditFieldsPanel.Visibility = Visibility.Collapsed;
            TransactionFieldsPanel.Visibility = Visibility.Visible;
            FromAccountLabel.Text = "من الحساب (المصدر) *";
            ToAccountPanel.Visibility = Visibility.Visible;
            if (FromAccountCombo.Items.Count > 0) FromAccountCombo.SelectedIndex = 0;
            if (ToAccountCombo.Items.Count > 1) ToAccountCombo.SelectedIndex = 1;
            TxAmountBox.Text = "";
            TxRefBox.Text = "";
            TxDescBox.Text = "تحويل بين الحسابات";
            TxNotesBox.Text = "";
            ModalOverlay.Visibility = Visibility.Visible;
        }

        private void OwnerDeposit_Click(object sender, RoutedEventArgs e)
        {
            if (!PermissionGuard.EnsurePermission(AdminDashboardWindow._currentUser, SystemPermissions.FinancialAccounts_Transfer, "إيداع من الصندوق إلى البنك"))
                return;

            _currentModalMode = "OwnerDeposit";
            ModalTitleText.Text = "🏛️ إيداع صاحب المطعم من الصندوق إلى البنك (تحويل داخلي)";
            AccountEditFieldsPanel.Visibility = Visibility.Collapsed;
            TransactionFieldsPanel.Visibility = Visibility.Visible;
            FromAccountLabel.Text = "من الصندوق النقدي *";
            ToAccountPanel.Visibility = Visibility.Visible;

            var mainCash = _accounts.FirstOrDefault(a => a.AccountType == FinancialAccountType.MainCashBox && a.IsActive);
            var firstBank = _accounts.FirstOrDefault(a => a.AccountType == FinancialAccountType.BankAccount && a.IsActive);
            if (mainCash != null) FromAccountCombo.SelectedValue = mainCash.Id;
            if (firstBank != null) ToAccountCombo.SelectedValue = firstBank.Id;

            TxAmountBox.Text = "";
            TxRefBox.Text = "";
            TxDescBox.Text = "توريد نقدي من الصندوق إلى الحساب البنكي";
            TxNotesBox.Text = "";
            ModalOverlay.Visibility = Visibility.Visible;
        }

        private async void StatementCard_Click(object sender, RoutedEventArgs e)
        {
            if (!PermissionGuard.EnsurePermission(AdminDashboardWindow._currentUser, SystemPermissions.FinancialAccounts_Statement, "عرض كشف الحساب"))
                return;

            if (sender is Button btn && btn.Tag is FinancialAccount acc)
            {
                _selectedStatementAccount = acc;
                await LoadStatementAsync(acc);
            }
        }

        private async Task LoadStatementAsync(FinancialAccount acc)
        {
            try
            {
                StatementModalOverlay.Visibility = Visibility.Visible;
                StatementTitleText.Text = $"📑 كشف حساب: {acc.AccountName} ({acc.BankOrWalletName})";
                StatementSubTitleText.Text = $"الرصيد الحالي: {acc.CurrentBalance:N0} {acc.Currency} | إجمالي الداخل: {acc.TotalInflow:N0} | إجمالي الخارج: {acc.TotalOutflow:N0}";

                _currentStatementRows = await ServiceLocator.FinancialAccountService.GetAccountStatementAsync(
                    acc.Id, StatementFromDate.SelectedDate, StatementToDate.SelectedDate);
                StatementDataGrid.ItemsSource = _currentStatementRows;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل كشف الحساب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void StatementDate_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_selectedStatementAccount != null && IsLoaded)
            {
                await LoadStatementAsync(_selectedStatementAccount);
            }
        }

        private void CloseStatementPopup_Click(object sender, RoutedEventArgs e)
        {
            StatementModalOverlay.Visibility = Visibility.Collapsed;
        }

        private void ModalCancel_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
        }

        private async void ModalConfirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var user = AdminDashboardWindow._currentUser;
                string userName = user?.Name ?? "مدير النظام";

                if (_currentModalMode == "CreateAccount")
                {
                    decimal.TryParse(AccOpeningBalanceBox.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal openBal);
                    var typeTag = (AccTypeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "1";
                    var newAcc = new FinancialAccount
                    {
                        AccountName = AccNameBox.Text.Trim(),
                        AccountType = (FinancialAccountType)int.Parse(typeTag),
                        BankOrWalletName = string.IsNullOrWhiteSpace(AccBankNameBox.Text) ? AccNameBox.Text.Trim() : AccBankNameBox.Text.Trim(),
                        AccountNumber = AccNumberBox.Text.Trim(),
                        CurrentBalance = Math.Max(0m, openBal)
                    };
                    await ServiceLocator.FinancialAccountService.CreateAccountAsync(newAcc, user?.Id, userName);
                    MessageBox.Show("تم إنشاء الحساب المالي بنجاح.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (_currentModalMode == "EditAccount" && _editingAccount != null)
                {
                    var typeTag = (AccTypeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "1";
                    _editingAccount.AccountName = AccNameBox.Text.Trim();
                    _editingAccount.AccountType = (FinancialAccountType)int.Parse(typeTag);
                    _editingAccount.BankOrWalletName = AccBankNameBox.Text.Trim();
                    _editingAccount.AccountNumber = AccNumberBox.Text.Trim();
                    await ServiceLocator.FinancialAccountService.UpdateAccountAsync(_editingAccount, user?.Id, userName);
                    MessageBox.Show("تم تحديث بيانات الحساب بنجاح.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    if (!decimal.TryParse(TxAmountBox.Text.Trim().Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
                    {
                        MessageBox.Show("يرجى إدخال مبلغ صحيح أكبر من الصفر.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    int fromId = (FromAccountCombo.SelectedValue is int fid) ? fid : 0;
                    if (fromId <= 0)
                    {
                        MessageBox.Show("يرجى اختيار الحساب.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string refNo = TxRefBox.Text.Trim();
                    string desc = string.IsNullOrWhiteSpace(TxDescBox.Text) ? "حركة مالية" : TxDescBox.Text.Trim();
                    string notes = TxNotesBox.Text.Trim();

                    if (_currentModalMode == "Deposit")
                    {
                        await ServiceLocator.FinancialAccountService.RecordDepositAsync(fromId, amount, refNo, desc, notes, user?.Id, userName);
                        MessageBox.Show("تم تسجيل الإيداع وتحديث رصيد الحساب بنجاح.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else if (_currentModalMode == "Withdraw")
                    {
                        await ServiceLocator.FinancialAccountService.RecordWithdrawalAsync(fromId, amount, refNo, desc, notes, user?.Id, userName);
                        MessageBox.Show("تم تسجيل السحب وتحديث رصيد الحساب بنجاح.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else if (_currentModalMode == "Transfer" || _currentModalMode == "OwnerDeposit")
                    {
                        int toId = (ToAccountCombo.SelectedValue is int tid) ? tid : 0;
                        if (toId <= 0)
                        {
                            MessageBox.Show("يرجى اختيار الحساب المستلم.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        bool isOwnerDep = _currentModalMode == "OwnerDeposit";
                        await ServiceLocator.FinancialAccountService.TransferBetweenAccountsAsync(fromId, toId, amount, refNo, notes, user?.Id, userName, isOwnerDep);
                        MessageBox.Show("تم تنفيذ التحويل المالي بين الحسابين داخل معاملة ذرية واحدة بنجاح (دون احتسابه إيراداً أو مصروفاً).", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                ModalOverlay.Visibility = Visibility.Collapsed;
                await LoadAccountsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"تعذر إتمام العملية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportStatementCsv_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStatementAccount == null || _currentStatementRows.Count == 0)
            {
                MessageBox.Show("لا توجد حركات لتصديرها في كشف الحساب الحالي.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "ملف إكسل منسق (*.xlsx)|*.xlsx|ملف CSV (*.csv)|*.csv",
                DefaultExt = ".xlsx",
                FileName = $"كشف_حساب_{_selectedStatementAccount.AccountName}_{DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}.xlsx"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    if (dlg.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        using var wb = new XLWorkbook();
                        var ws = wb.Worksheets.Add("كشف الحساب");
                        ws.RightToLeft = true;

                        // Title Banner
                        ws.Cell(1, 1).Value = $"كشف حساب: {_selectedStatementAccount.AccountName} ({_selectedStatementAccount.BankOrWalletName})";
                        ws.Range(1, 1, 1, 8).Merge();
                        ws.Range(1, 1, 1, 8).Style.Font.Bold = true;
                        ws.Range(1, 1, 1, 8).Style.Font.FontSize = 15;
                        ws.Range(1, 1, 1, 8).Style.Font.FontColor = XLColor.White;
                        ws.Range(1, 1, 1, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#00358C");
                        ws.Range(1, 1, 1, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Range(1, 1, 1, 8).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        ws.Row(1).Height = 32;

                        // Summary Row
                        ws.Cell(2, 1).Value = $"الرصيد الحالي: {_selectedStatementAccount.CurrentBalance:N0} {_selectedStatementAccount.Currency}   |   إجمالي الداخل: {_selectedStatementAccount.TotalInflow:N0}   |   إجمالي الخارج: {_selectedStatementAccount.TotalOutflow:N0}   |   تاريخ التصدير: {DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}";
                        ws.Range(2, 1, 2, 8).Merge();
                        ws.Range(2, 1, 2, 8).Style.Font.Bold = true;
                        ws.Range(2, 1, 2, 8).Style.Font.FontSize = 11;
                        ws.Range(2, 1, 2, 8).Style.Font.FontColor = XLColor.FromHtml("#1E293B");
                        ws.Range(2, 1, 2, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#E0F2FE");
                        ws.Range(2, 1, 2, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Range(2, 1, 2, 8).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        ws.Row(2).Height = 24;

                        // Table Headers
                        string[] headers = { "التاريخ والوقت", "نوع الحركة", "رقم المرجع", "البيان", "المبلغ الداخل (+)", "المبلغ الخارج (-)", "الرصيد بعد الحركة", "المنفذ" };
                        for (int c = 0; c < headers.Length; c++)
                        {
                            var cell = ws.Cell(4, c + 1);
                            cell.Value = headers[c];
                            cell.Style.Font.Bold = true;
                            cell.Style.Font.FontSize = 11;
                            cell.Style.Font.FontColor = XLColor.White;
                            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
                        }
                        ws.Row(4).Height = 26;

                        int rowIdx = 5;
                        foreach (var r in _currentStatementRows)
                        {
                            ws.Cell(rowIdx, 1).Value = r.TransactionDate.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                            ws.Cell(rowIdx, 2).Value = r.TransactionTypeText ?? "";
                            ws.Cell(rowIdx, 3).Value = r.ReferenceNumber ?? "";
                            ws.Cell(rowIdx, 4).Value = r.Description ?? "";
                            ws.Cell(rowIdx, 5).Value = r.AmountIn;
                            ws.Cell(rowIdx, 6).Value = r.AmountOut;
                            ws.Cell(rowIdx, 7).Value = r.BalanceAfter;
                            ws.Cell(rowIdx, 8).Value = r.PerformedByUserName ?? "";

                            ws.Cell(rowIdx, 5).Style.NumberFormat.Format = "#,##0";
                            ws.Cell(rowIdx, 6).Style.NumberFormat.Format = "#,##0";
                            ws.Cell(rowIdx, 7).Style.NumberFormat.Format = "#,##0";

                            if (r.AmountIn > 0)
                                ws.Cell(rowIdx, 5).Style.Font.FontColor = XLColor.FromHtml("#15803D");
                            if (r.AmountOut > 0)
                                ws.Cell(rowIdx, 6).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                            ws.Cell(rowIdx, 7).Style.Font.Bold = true;

                            var rowRange = ws.Range(rowIdx, 1, rowIdx, 8);
                            rowRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            ws.Cell(rowIdx, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Cell(rowIdx, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Cell(rowIdx, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Cell(rowIdx, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                            ws.Cell(rowIdx, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Cell(rowIdx, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Cell(rowIdx, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Cell(rowIdx, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            if (rowIdx % 2 == 0)
                                rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");

                            for (int c = 1; c <= 8; c++)
                            {
                                ws.Cell(rowIdx, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                ws.Cell(rowIdx, c).Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");
                            }
                            ws.Row(rowIdx).Height = 22;
                            rowIdx++;
                        }

                        // Totals Row
                        ws.Cell(rowIdx, 1).Value = "الإجمالي";
                        ws.Range(rowIdx, 1, rowIdx, 4).Merge();
                        ws.Cell(rowIdx, 5).Value = _currentStatementRows.Sum(x => x.AmountIn);
                        ws.Cell(rowIdx, 6).Value = _currentStatementRows.Sum(x => x.AmountOut);
                        ws.Cell(rowIdx, 7).Value = _selectedStatementAccount.CurrentBalance;
                        ws.Cell(rowIdx, 8).Value = "";

                        ws.Cell(rowIdx, 5).Style.NumberFormat.Format = "#,##0";
                        ws.Cell(rowIdx, 6).Style.NumberFormat.Format = "#,##0";
                        ws.Cell(rowIdx, 7).Style.NumberFormat.Format = "#,##0";

                        var totalRange = ws.Range(rowIdx, 1, rowIdx, 8);
                        totalRange.Style.Font.Bold = true;
                        totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                        totalRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        totalRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        for (int c = 1; c <= 8; c++)
                        {
                            ws.Cell(rowIdx, c).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                            ws.Cell(rowIdx, c).Style.Border.OutsideBorderColor = XLColor.FromHtml("#94A3B8");
                        }
                        ws.Row(rowIdx).Height = 25;

                        ws.Columns().AdjustToContents();
                        for (int c = 1; c <= 8; c++)
                        {
                            if (ws.Column(c).Width < 16) ws.Column(c).Width = 16;
                        }
                        if (ws.Column(4).Width < 36) ws.Column(4).Width = 36;

                        wb.SaveAs(dlg.FileName);
                    }
                    else
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("sep=,");
                        sb.AppendLine("التاريخ والوقت,نوع الحركة,رقم المرجع,البيان,المبلغ الداخل,المبلغ الخارج,الرصيد بعد الحركة,المستخدم المنفذ");
                        foreach (var r in _currentStatementRows)
                        {
                            string dateStr = r.TransactionDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                            string amtIn = r.AmountIn.ToString("0", CultureInfo.InvariantCulture);
                            string amtOut = r.AmountOut.ToString("0", CultureInfo.InvariantCulture);
                            string balAfter = r.BalanceAfter.ToString("0", CultureInfo.InvariantCulture);
                            string desc = (r.Description ?? "").Replace(",", " - ");
                            sb.AppendLine($"{dateStr},{r.TransactionTypeText},{r.ReferenceNumber},{desc},{amtIn},{amtOut},{balAfter},{r.PerformedByUserName}");
                        }
                        File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
                    }

                    MessageBox.Show("تم تصدير كشف الحساب بنجاح في جدول إكسل منسق ومرتب.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"حدث خطأ أثناء تصدير الملف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PrintStatement_Click(object sender, RoutedEventArgs e)
        {
            ExportStatementCsv_Click(sender, e);
        }
    }
}
