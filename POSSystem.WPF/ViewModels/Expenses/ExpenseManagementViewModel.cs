using POSSystem.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;
using POSSystem.WPF.Shered.Commands;
using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.Views;

namespace POSSystem.WPF.ViewModels.Expenses
{
    public partial class ExpenseManagementViewModel : BaseViewModel, POSSystem.WPF.Services.ISearchable
    {
        public ExpenseManagementViewModel()
        {
            var sp = POSSystem.WPF.App.ServiceProvider;
            _expenseService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IExpenseService>(sp);
            _receiptVoucherService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IReceiptVoucherService>(sp);

            Commands = new ExpenseManagementCommands(this);
            expenseManagementProberty = new ExpenseManagementProberty(_expenseService, _receiptVoucherService);
            expenseManagementProberty.OnSelectedAccountChanged = (acc) => RefreshSelectedAccountDetails(acc);
            expenseManagementProberty.OnAccountsFilterChanged = () => RebuildExpenseAccountsSummary();
            
            POSSystem.WPF.Helpers.TaskExtensions.RunSafe(async () =>
            {
                await LoadExpensesAsync();
                await LoadVouchersAsync();
            });
        }

        private List<Expense> _allExpensesCache = new();
        private List<Expense> _periodExpensesForAccounts = new();
        private List<ReceiptVoucher> _allVouchersCache = new();
        private string _globalSearchQuery = string.Empty;

        public void ApplySearch(string query)
        {
            _globalSearchQuery = query?.Trim() ?? string.Empty;
            ApplyExpenseAndVoucherSearchFilter();
            RebuildExpenseAccountsSummary();
        }

        private static string NormalizeAccountName(string? category)
        {
            return string.IsNullOrWhiteSpace(category) ? "حساب مصاريف عامة" : category.Trim();
        }

        public void RebuildExpenseAccountsSummary()
        {
            if (expenseManagementProberty == null) return;
            SyncSavedCustomAccounts();
            var source = _periodExpensesForAccounts?.Any() == true ? _periodExpensesForAccounts : (_allExpensesCache ?? new List<Expense>());

            // Ensure any custom account in DB is also in ExpenseCategories filter dropdown
            foreach (var cat in source.Select(e => e.Category?.Trim()).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct())
            {
                if (!expenseManagementProberty.ExpenseCategories.Contains(cat!))
                {
                    expenseManagementProberty.ExpenseCategories.Add(cat!);
                }
            }

            string? prevSelectedAccountName = expenseManagementProberty.SelectedExpenseAccount?.AccountName;
            string searchFilter = !string.IsNullOrWhiteSpace(expenseManagementProberty.AccountSearchText)
                ? expenseManagementProberty.AccountSearchText.Trim()
                : _globalSearchQuery;

            decimal grandTotal = source.Sum(e => e.Amount);

            var activeGroups = source
                .GroupBy(e => NormalizeAccountName(e.Category))
                .Select(g =>
                {
                    decimal cashAmt = g.Where(e => !e.IsWalletExpense).Sum(e => e.Amount);
                    decimal walletAmt = g.Where(e => e.IsWalletExpense).Sum(e => e.Amount);
                    decimal totAmt = g.Sum(e => e.Amount);
                    DateTime? lastDate = g.Max(e => (DateTime?)e.ExpenseDate);
                    return new ExpenseAccountSummaryItem
                    {
                        AccountName = g.Key,
                        TransactionsCount = g.Count(),
                        CashAmount = cashAmt,
                        WalletAmount = walletAmt,
                        TotalAmount = totAmt,
                        Percentage = grandTotal > 0 ? Math.Round((totAmt / grandTotal) * 100m, 1) : 0m,
                        LastTransactionDate = lastDate
                    };
                })
                .OrderByDescending(x => x.TotalAmount)
                .ThenByDescending(x => x.TransactionsCount)
                .ToList();

            var finalItems = new List<ExpenseAccountSummaryItem>(activeGroups);

            // Include registered accounts with 0 transactions when ShowOnlyActiveAccounts is false
            if (!expenseManagementProberty.ShowOnlyActiveAccounts)
            {
                var existingNames = new HashSet<string>(finalItems.Select(x => x.AccountName), StringComparer.OrdinalIgnoreCase);
                foreach (var registeredCat in expenseManagementProberty.ExpenseCategories)
                {
                    if (string.IsNullOrWhiteSpace(registeredCat) || registeredCat == "الكل (جميع الحسابات)")
                        continue;

                    var trimmed = registeredCat.Trim();
                    if (!existingNames.Contains(trimmed))
                    {
                        finalItems.Add(new ExpenseAccountSummaryItem
                        {
                            AccountName = trimmed,
                            TransactionsCount = 0,
                            CashAmount = 0m,
                            WalletAmount = 0m,
                            TotalAmount = 0m,
                            Percentage = 0m,
                            LastTransactionDate = null
                        });
                        existingNames.Add(trimmed);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(searchFilter))
            {
                finalItems = finalItems.Where(acc =>
                    acc.AccountName.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) ||
                    source.Any(e => NormalizeAccountName(e.Category).Equals(acc.AccountName, StringComparison.OrdinalIgnoreCase) &&
                                   ((e.Name != null && e.Name.Contains(searchFilter, StringComparison.OrdinalIgnoreCase)) ||
                                    (e.Notes != null && e.Notes.Contains(searchFilter, StringComparison.OrdinalIgnoreCase)) ||
                                    (e.SupplierName != null && e.SupplierName.Contains(searchFilter, StringComparison.OrdinalIgnoreCase)))))
                    .ToList();
            }

            expenseManagementProberty.ExpenseAccountsSummary.Clear();
            foreach (var item in finalItems)
            {
                expenseManagementProberty.ExpenseAccountsSummary.Add(item);
            }

            ExpenseAccountSummaryItem? toSelect = null;
            if (!string.IsNullOrWhiteSpace(prevSelectedAccountName))
            {
                toSelect = expenseManagementProberty.ExpenseAccountsSummary
                    .FirstOrDefault(x => x.AccountName.Equals(prevSelectedAccountName, StringComparison.OrdinalIgnoreCase));
            }
            toSelect ??= expenseManagementProberty.ExpenseAccountsSummary.FirstOrDefault();

            expenseManagementProberty.SelectedExpenseAccount = toSelect;
            RefreshSelectedAccountDetails(toSelect);
            expenseManagementProberty.NotifyAccountsSummaryChanged();
        }

        public void RefreshSelectedAccountDetails(ExpenseAccountSummaryItem? account)
        {
            if (expenseManagementProberty == null) return;
            expenseManagementProberty.SelectedAccountExpenses.Clear();

            if (account != null)
            {
                var source = _periodExpensesForAccounts?.Any() == true ? _periodExpensesForAccounts : (_allExpensesCache ?? new List<Expense>());
                var matching = source
                    .Where(e => e != null && NormalizeAccountName(e.Category).Equals(account.AccountName, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(e => e.ExpenseDate)
                    .ThenByDescending(e => e.Id)
                    .ToList();

                string searchFilter = !string.IsNullOrWhiteSpace(expenseManagementProberty.AccountSearchText)
                    ? expenseManagementProberty.AccountSearchText.Trim()
                    : _globalSearchQuery;

                if (!string.IsNullOrWhiteSpace(searchFilter) && !account.AccountName.Contains(searchFilter, StringComparison.OrdinalIgnoreCase))
                {
                    matching = matching.Where(e =>
                        (e.Name != null && e.Name.Contains(searchFilter, StringComparison.OrdinalIgnoreCase)) ||
                        (e.Notes != null && e.Notes.Contains(searchFilter, StringComparison.OrdinalIgnoreCase)) ||
                        (e.SupplierName != null && e.SupplierName.Contains(searchFilter, StringComparison.OrdinalIgnoreCase)) ||
                        (e.InvoiceNumber != null && e.InvoiceNumber.Contains(searchFilter, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                }

                foreach (var exp in matching)
                {
                    expenseManagementProberty.SelectedAccountExpenses.Add(exp);
                }
            }

            expenseManagementProberty.NotifySelectedAccountChanged();
        }

        private void SyncSavedCustomAccounts()
        {
            try
            {
                var settings = POSSystem.WPF.Services.SettingsService.LoadSettings();
                if (settings.CustomExpenseAccounts != null)
                {
                    foreach (var acc in settings.CustomExpenseAccounts)
                    {
                        if (!string.IsNullOrWhiteSpace(acc) &&
                            !expenseManagementProberty.ExpenseCategories.Any(c => c.Equals(acc.Trim(), StringComparison.OrdinalIgnoreCase)))
                        {
                            expenseManagementProberty.ExpenseCategories.Add(acc.Trim());
                        }
                    }
                }
            }
            catch { }
        }

        private void PersistCustomAccountsList()
        {
            try
            {
                var settings = POSSystem.WPF.Services.SettingsService.LoadSettings();
                settings.CustomExpenseAccounts = expenseManagementProberty.ExpenseCategories
                    .Where(c => !string.IsNullOrWhiteSpace(c) && c != "الكل (جميع الحسابات)")
                    .Select(c => c.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                POSSystem.WPF.Services.SettingsService.SaveSettings(settings);
            }
            catch { }
        }

        public void SelectExpenseAccount(ExpenseAccountSummaryItem? account)
        {
            if (account == null) return;
            expenseManagementProberty.SelectedExpenseAccount = account;
        }

        public void AddNewExpenseAccount()
        {
            try
            {
                var rawName = expenseManagementProberty.NewAccountName?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(rawName))
                {
                    MessageBox.Show("يرجى كتابة اسم الحساب الجديد في الحقل المخصص أولاً (مثال: حساب محمد، حساب صيانة، حساب دعاية...).", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (!expenseManagementProberty.ExpenseCategories.Any(c => c.Equals(rawName, StringComparison.OrdinalIgnoreCase)))
                {
                    expenseManagementProberty.ExpenseCategories.Add(rawName);
                }

                PersistCustomAccountsList();
                expenseManagementProberty.NewAccountName = string.Empty;
                expenseManagementProberty.ShowOnlyActiveAccounts = false;
                RebuildExpenseAccountsSummary();

                var created = expenseManagementProberty.ExpenseAccountsSummary
                    .FirstOrDefault(x => x.AccountName.Equals(rawName, StringComparison.OrdinalIgnoreCase));
                if (created != null)
                {
                    expenseManagementProberty.SelectedExpenseAccount = created;
                }

                MessageBox.Show($"تم إضافة وتفعيل '{rawName}' بنجاح!\nيمكنك الآن تسجيل المصروفات عليه أو تعديل اسمه في أي وقت.", "تم إضافة الحساب", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء إضافة الحساب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void StartEditExpenseAccount(ExpenseAccountSummaryItem? account = null)
        {
            var target = account ?? expenseManagementProberty.SelectedExpenseAccount;
            if (target == null || string.IsNullOrWhiteSpace(target.AccountName))
            {
                MessageBox.Show("يرجى اختيار الحساب المراد تعديل اسمه من القائمة أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            expenseManagementProberty.SelectedExpenseAccount = target;
            expenseManagementProberty.EditingAccountOriginalName = target.AccountName;
            expenseManagementProberty.EditingAccountNewName = target.AccountName;
            expenseManagementProberty.IsEditingAccountName = true;
        }

        public void CancelEditExpenseAccount()
        {
            expenseManagementProberty.IsEditingAccountName = false;
            expenseManagementProberty.EditingAccountOriginalName = string.Empty;
            expenseManagementProberty.EditingAccountNewName = string.Empty;
        }

        public async Task SaveEditExpenseAccountAsync()
        {
            try
            {
                string oldName = expenseManagementProberty.EditingAccountOriginalName?.Trim() ?? string.Empty;
                string newName = expenseManagementProberty.EditingAccountNewName?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(oldName))
                {
                    CancelEditExpenseAccount();
                    return;
                }

                if (string.IsNullOrWhiteSpace(newName))
                {
                    MessageBox.Show("يرجى إدخال الاسم الجديد للحساب.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (oldName.Equals(newName, StringComparison.OrdinalIgnoreCase))
                {
                    CancelEditExpenseAccount();
                    return;
                }

                // Update in ExpenseCategories
                int existingIdx = -1;
                for (int i = 0; i < expenseManagementProberty.ExpenseCategories.Count; i++)
                {
                    if (expenseManagementProberty.ExpenseCategories[i].Equals(oldName, StringComparison.OrdinalIgnoreCase))
                    {
                        existingIdx = i;
                        break;
                    }
                }

                if (existingIdx >= 0)
                {
                    if (expenseManagementProberty.ExpenseCategories.Any(c => c.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                        expenseManagementProberty.ExpenseCategories.RemoveAt(existingIdx);
                    else
                        expenseManagementProberty.ExpenseCategories[existingIdx] = newName;
                }
                else if (!expenseManagementProberty.ExpenseCategories.Any(c => c.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                {
                    expenseManagementProberty.ExpenseCategories.Add(newName);
                }

                PersistCustomAccountsList();

                // Update any existing expenses in DB that belong to oldName
                var allExpenses = await _expenseService.GetAllExpensesAsync();
                if (allExpenses != null)
                {
                    foreach (var exp in allExpenses.Where(e => e != null && NormalizeAccountName(e.Category).Equals(oldName, StringComparison.OrdinalIgnoreCase)))
                    {
                        exp.Category = newName;
                        await _expenseService.UpdateExpenseAsync(exp);
                    }
                }

                CancelEditExpenseAccount();
                await LoadExpensesAsync();
                await expenseManagementProberty.LoadExpensesAsync();

                var updatedItem = expenseManagementProberty.ExpenseAccountsSummary
                    .FirstOrDefault(x => x.AccountName.Equals(newName, StringComparison.OrdinalIgnoreCase));
                if (updatedItem != null)
                    expenseManagementProberty.SelectedExpenseAccount = updatedItem;

                MessageBox.Show($"تم تعديل اسم الحساب من '{oldName}' إلى '{newName}' بنجاح وتحديث كافة الحركات المرتبطة به.", "تم تعديل الحساب", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تعديل اسم الحساب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task DeleteExpenseAccountAsync(ExpenseAccountSummaryItem? account = null)
        {
            try
            {
                var target = account ?? expenseManagementProberty.SelectedExpenseAccount;
                if (target == null || string.IsNullOrWhiteSpace(target.AccountName))
                    return;

                string accName = target.AccountName.Trim();
                if (target.TransactionsCount > 0)
                {
                    MessageBox.Show($"لا يمكن حذف الحساب '{accName}' لوجود ({target.TransactionsCount}) عملية صرف مسجلة عليه.\nيمكنك تعديل اسم الحساب من زر (تعديل) أو حذف المصروفات المرتبطة به أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var confirm = MessageBox.Show($"هل تريد حذف الحساب '{accName}' من قائمة الحسابات؟", "تأكيد حذف الحساب", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes)
                    return;

                var toRemove = expenseManagementProberty.ExpenseCategories
                    .Where(c => c.Equals(accName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                foreach (var item in toRemove)
                {
                    expenseManagementProberty.ExpenseCategories.Remove(item);
                }

                PersistCustomAccountsList();
                RebuildExpenseAccountsSummary();
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء حذف الحساب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void AddExpenseForAccount(ExpenseAccountSummaryItem? account = null)
        {
            try
            {
                string? targetAccount = (account ?? expenseManagementProberty.SelectedExpenseAccount)?.AccountName;
                var expenseControl = new Views.Popups.AddEditExpenseControl(this, null, targetAccount);
                var window = Application.Current.Windows.OfType<AdminDashboardWindow>().FirstOrDefault();
                if (window != null) window.ShowOverlay(expenseControl);
                else MessageBox.Show("لم يتم العثور على نافذة لوحة التحكم الرئيسية", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح نافذة إضافة المصروف للحساب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void AddExpenseForSelectedAccount()
        {
            AddExpenseForAccount(expenseManagementProberty.SelectedExpenseAccount);
        }

        private void ApplyExpenseAndVoucherSearchFilter()
        {
            var q = _globalSearchQuery;
            if (string.IsNullOrWhiteSpace(q))
            {
                expenseManagementProberty.Expenses = new ObservableCollection<Expense>(_allExpensesCache);
                expenseManagementProberty.Vouchers = new ObservableCollection<ReceiptVoucher>(_allVouchersCache);
            }
            else
            {
                var filteredExp = _allExpensesCache.Where(e =>
                    (e.Name != null && e.Name.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (e.Category != null && e.Category.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (e.SupplierName != null && e.SupplierName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (e.InvoiceNumber != null && e.InvoiceNumber.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (e.Notes != null && e.Notes.Contains(q, StringComparison.OrdinalIgnoreCase))).ToList();

                var filteredVouch = _allVouchersCache.Where(v =>
                    (v.VoucherNumber != null && v.VoucherNumber.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (v.CustomerName != null && v.CustomerName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (v.Name != null && v.Name.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (v.Notes != null && v.Notes.Contains(q, StringComparison.OrdinalIgnoreCase))).ToList();

                expenseManagementProberty.Expenses = new ObservableCollection<Expense>(filteredExp);
                expenseManagementProberty.Vouchers = new ObservableCollection<ReceiptVoucher>(filteredVouch);
            }
            OnPropertyChanged(nameof(expenseManagementProberty.TotalExpenses));
            OnPropertyChanged(nameof(expenseManagementProberty.TotalReceipts));
            OnPropertyChanged(nameof(expenseManagementProberty.TotalPayments));
            OnPropertyChanged(nameof(expenseManagementProberty.NetAmount));
        }

        private async Task EnsurePaidSalariesSyncedToExpensesAsync()
        {
            try
            {
                var uow = ServiceLocator.UnitOfWork;
                var ctx = uow.GetContext();
                var paidSalaries = await ctx.EmployeeSalaries
                    .AsNoTracking()
                    .Include(s => s.Employee)
                    .Where(s => s.IsPaid && s.NetSalary > 0)
                    .ToListAsync();

                if (paidSalaries.Count == 0) return;

                var existingSalaryNotes = await ctx.Expenses
                    .AsNoTracking()
                    .Where(e => e.Notes != null && e.Notes.Contains("[راتب:"))
                    .Select(e => e.Notes!)
                    .ToListAsync();

                bool addedAny = false;
                foreach (var sal in paidSalaries)
                {
                    string tag = $"[راتب: {sal.Id}]";
                    if (existingSalaryNotes.Any(n => n.Contains(tag)))
                        continue;

                    string empName = sal.Employee?.Name ?? "موظف";
                    var payDate = sal.PaymentDate == default ? sal.SalaryDate : sal.PaymentDate;
                    await ctx.Expenses.AddAsync(new Expense
                    {
                        Name = $"صرف راتب شهر {sal.SalaryDate:yyyy/MM} - {empName}",
                        Category = "حساب رواتب وموظفين",
                        Amount = sal.NetSalary,
                        Type = ExpenseType.Monthly,
                        ExpenseDate = payDate,
                        PaymentMethod = sal.PaymentMethod,
                        SupplierName = empName,
                        UserId = sal.EmployeeId,
                        Notes = $"{tag} {sal.Notes}".Trim(),
                        CreatedAt = payDate,
                        UpdatedAt = DateTime.Now
                    });
                    addedAny = true;
                }

                if (addedAny)
                {
                    await ctx.SaveChangesAsync();
                }
            }
            catch { }
        }

        public async Task LoadExpensesAsync()
        {
            try
            {
                await EnsurePaidSalariesSyncedToExpensesAsync();
                IEnumerable<Expense> expenses = await _expenseService.GetByDateRangeAsync(expenseManagementProberty.StartDate, expenseManagementProberty.EndDate);
                var periodList = expenses?.Where(e => e != null).ToList() ?? new List<Expense>();

                if (!periodList.Any())
                {
                    var all = await _expenseService.GetAllExpensesAsync();
                    periodList = all?.Where(e => e != null).ToList() ?? new List<Expense>();
                }

                // Store full period expenses for the isolated Accounts tab
                _periodExpensesForAccounts = periodList.ToList();

                IEnumerable<Expense> filteredForMonthlyTab = periodList;
                if (!string.IsNullOrWhiteSpace(expenseManagementProberty.SelectedCategory) && expenseManagementProberty.SelectedCategory != "الكل (جميع الحسابات)")
                    filteredForMonthlyTab = filteredForMonthlyTab.Where(e => e != null && e.Category == expenseManagementProberty.SelectedCategory);
                
                if (expenseManagementProberty.SelectedExpenseType.HasValue)
                    filteredForMonthlyTab = filteredForMonthlyTab.Where(e => e != null && e.Type == expenseManagementProberty.SelectedExpenseType.Value);
                
                _allExpensesCache = filteredForMonthlyTab.ToList();
                RebuildExpenseAccountsSummary();
                ApplyExpenseAndVoucherSearchFilter();
                OnPropertyChanged(nameof(expenseManagementProberty.TotalExpenses));
                OnPropertyChanged(nameof(expenseManagementProberty.NetAmount));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading expenses: {ex.Message}\n{ex.StackTrace}");
                expenseManagementProberty.Expenses = new ObservableCollection<Expense>();
                OnPropertyChanged(nameof(expenseManagementProberty.TotalExpenses));
                OnPropertyChanged(nameof(expenseManagementProberty.NetAmount));
                
                string? logPath = null;
                try
                {
                    var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                    Directory.CreateDirectory(logDir);
                    logPath = Path.Combine(logDir, "errors.log");
                    File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error loading expenses:\n{ex}\n\n");
                }
                catch { }

#if DEBUG
                var debugMsg = $"حدث خطأ أثناء تحميل المصروفات:\n{ex}";
                if (!string.IsNullOrWhiteSpace(logPath)) debugMsg += $"\n\nتم تسجيل التفاصيل في: {logPath}";
                MessageBox.Show(debugMsg, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
#else
                var userMsg = "حدث خطأ أثناء تحميل المصروفات. يرجى المحاولة مرة أخرى.";
                if (!string.IsNullOrWhiteSpace(logPath)) userMsg += $"\n(تفاصيل محفوظة في: {logPath})";
                MessageBox.Show(userMsg, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        public async Task LoadVouchersAsync()
        {
            try
            {
                var vouchers = (await _receiptVoucherService.GetByDateRangeAsync(expenseManagementProberty.StartDate, expenseManagementProberty.EndDate))?.ToList() ?? new List<ReceiptVoucher>();

                if (!vouchers.Any())
                {
                    var allV = await _receiptVoucherService.GetAllVouchersAsync();
                    vouchers = allV?.ToList() ?? new List<ReceiptVoucher>();
                }

                if (expenseManagementProberty.SelectedVoucherType.HasValue)
                {
                    vouchers = vouchers.Where(v => v.Type == expenseManagementProberty.SelectedVoucherType.Value).ToList();
                }

                _allVouchersCache = vouchers;
                ApplyExpenseAndVoucherSearchFilter();
                OnPropertyChanged(nameof(expenseManagementProberty.TotalReceipts));
                OnPropertyChanged(nameof(expenseManagementProberty.TotalPayments));
                OnPropertyChanged(nameof(expenseManagementProberty.NetAmount));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل السندات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ClearVoucherFilters()
        {
            expenseManagementProberty.SelectedVoucherTypeItem = expenseManagementProberty.VoucherTypeFilterItems.FirstOrDefault();
            expenseManagementProberty.StartDate = DateTime.Today.AddMonths(-1);
            expenseManagementProberty.EndDate = DateTime.Today;
            LoadVouchersAsync().SafeFireAndForget();
        }

        public void AddExpense()
        {
            try
            {
                var expenseControl = new Views.Popups.AddEditExpenseControl(this);
                var window = Application.Current.Windows.OfType<AdminDashboardWindow>().FirstOrDefault();
                if (window != null) window.ShowOverlay(expenseControl);
                else MessageBox.Show("لم يتم العثور على نافذة لوحة التحكم الرئيسية", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في فتح نافذة إضافة المصروف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public void EditExpense(object? parameter = null)
        {
            try
            {
                if (parameter is Expense expense) SelectedExpense = expense;
                if (SelectedExpense == null) return;
                
                var expenseControl = new Views.Popups.AddEditExpenseControl(this, SelectedExpense);
                var window = Application.Current.Windows.OfType<AdminDashboardWindow>().FirstOrDefault();
                if (window != null) window.ShowOverlay(expenseControl);
                else MessageBox.Show("لم يتم العثور على نافذة لوحة التحكم الرئيسية", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في فتح نافذة تعديل المصروف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public async Task DeleteExpenseAsync(object? parameter = null)
        {
            try
            {
                if (parameter is Expense expense) SelectedExpense = expense;
                if (SelectedExpense == null) return;
                
                var result = MessageBox.Show($"هل أنت متأكد من حذف المصروف '{SelectedExpense.Name}'؟", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    await _expenseService.DeleteExpenseAsync(SelectedExpense.Id);
                    MessageBox.Show("تم حذف المصروف بنجاح!", "تم الحذف", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadExpensesAsync();
                    await expenseManagementProberty.LoadExpensesAsync();
                }
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في حذف المصروف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public async Task AddExpenseAsync(Expense expense)
        {
            try
            {
                var savedExpense = await _expenseService.CreateExpenseAsync(expense);
                await LoadExpensesAsync();
                await expenseManagementProberty.LoadExpensesAsync();
                await LoadVouchersAsync();
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في إضافة المصروف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); throw; }
        }

        public async Task UpdateExpenseAsync(Expense expense)
        {
            try
            {
                await _expenseService.UpdateExpenseAsync(expense);
                await LoadExpensesAsync();
                await expenseManagementProberty.LoadExpensesAsync();
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في تحديث المصروف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); throw; }
        }

        public void AddVoucher()
        {
            try
            {
                var voucherControl = new Views.Popups.AddEditVoucherControl(this);
                var window = Application.Current.Windows.OfType<AdminDashboardWindow>().FirstOrDefault();
                if (window != null) window.ShowOverlay(voucherControl);
                else MessageBox.Show("لم يتم العثور على نافذة لوحة التحكم الرئيسية", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في فتح نافذة إضافة السند: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public void EditVoucher(object? parameter = null)
        {
            try
            {
                if (parameter is ReceiptVoucher voucher) SelectedVoucher = voucher;
                if (SelectedVoucher == null) return;
                
                var voucherControl = new Views.Popups.AddEditVoucherControl(this, SelectedVoucher);
                var window = Application.Current.Windows.OfType<AdminDashboardWindow>().FirstOrDefault();
                if (window != null) window.ShowOverlay(voucherControl);
                else MessageBox.Show("لم يتم العثور على نافذة لوحة التحكم الرئيسية", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في فتح نافذة تعديل السند: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public async Task DeleteVoucherAsync(object? parameter = null)
        {
            try
            {
                if (parameter is ReceiptVoucher voucher) SelectedVoucher = voucher;
                if (SelectedVoucher == null) return;
                
                string extraWarning = "";
                if (SelectedVoucher.Type == VoucherType.Payment)
                {
                    if (SelectedVoucher.EmployeeAdvanceId.HasValue ||
                        (SelectedVoucher.VoucherNumber != null && SelectedVoucher.VoucherNumber.StartsWith("PV-ADV-")) ||
                        (SelectedVoucher.Notes != null && SelectedVoucher.Notes.Contains("سلفة")) ||
                        (SelectedVoucher.Name != null && SelectedVoucher.Name.Contains("سلفة")))
                    {
                        extraWarning = $"\n\nتنبيه: هذا السند مرتبط بسلفة موظف ({SelectedVoucher.CustomerName}).\n" +
                                       "حذف السند سيؤدي تلقائياً إلى:\n" +
                                       "• إلغاء السلفة المسجلة على الموظف فوراً.\n" +
                                       "• عكس حركة الصرف واسترجاع الرصيد إلى الصندوق أو المحفظة.\n" +
                                       "• إعادة احتساب استحقاقات وراتب الموظف.";
                    }
                }

                var result = MessageBox.Show(
                    $"هل أنت متأكد من حذف السند '{SelectedVoucher.VoucherNumber}' بمبلغ ({SelectedVoucher.Amount:N0} ريال)؟{extraWarning}",
                    "تأكيد حذف السند",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    await _receiptVoucherService.DeleteVoucherAsync(SelectedVoucher.Id);
                    MessageBox.Show("تم حذف السند وإلغاء كافة الارتباطات المالية بنجاح!", "تم الحذف", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadVouchersAsync();
                    await expenseManagementProberty.LoadVouchersAsync();
                }
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في حذف السند: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public async Task AddVoucherAsync(ReceiptVoucher voucher)
        {
            try
            {
                var savedVoucher = await _receiptVoucherService.CreateVoucherAsync(voucher);
                await LoadVouchersAsync();
                await expenseManagementProberty.LoadVouchersAsync();
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في إضافة السند: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); throw; }
        }

        public async Task UpdateVoucherAsync(ReceiptVoucher voucher)
        {
            try
            {
                await _receiptVoucherService.UpdateVoucherAsync(voucher);
                await LoadVouchersAsync();
                await expenseManagementProberty.LoadVouchersAsync();
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في تحديث السند: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); throw; }
        }

        public async Task RefreshAsync() { await LoadExpensesAsync(); await LoadVouchersAsync(); }

        public void ShowExpenses()
        {
            expenseManagementProberty.IsExpensesVisible = true;
            expenseManagementProberty.IsAccountsVisible = false;
            expenseManagementProberty.IsVouchersVisible = false;
        }

        public void ShowAccounts()
        {
            expenseManagementProberty.IsExpensesVisible = false;
            expenseManagementProberty.IsAccountsVisible = true;
            expenseManagementProberty.IsVouchersVisible = false;
            RebuildExpenseAccountsSummary();
        }

        public void ShowVouchers() 
        { 
            expenseManagementProberty.IsExpensesVisible = false;
            expenseManagementProberty.IsAccountsVisible = false;
            expenseManagementProberty.IsVouchersVisible = true; 
            LoadVouchersAsync().SafeFireAndForget();
        }

        public void ClearFilters()
        {
            expenseManagementProberty.SelectedCategory = "";
            expenseManagementProberty.SelectedExpenseType = null;
            expenseManagementProberty.AccountSearchText = "";
            expenseManagementProberty.StartDate = DateTime.Today.AddMonths(-1);
            expenseManagementProberty.EndDate = DateTime.Today;
        }

        public void SetPopupOwner(Window popup)
        {
            try
            {
                var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
                if (activeWindow != null && activeWindow != popup) { popup.Owner = activeWindow; popup.WindowStartupLocation = WindowStartupLocation.CenterOwner; }
                else if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsLoaded) { popup.Owner = Application.Current.MainWindow; popup.WindowStartupLocation = WindowStartupLocation.CenterOwner; }
                else popup.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            catch { popup.WindowStartupLocation = WindowStartupLocation.CenterScreen; }
        }
    }
}
