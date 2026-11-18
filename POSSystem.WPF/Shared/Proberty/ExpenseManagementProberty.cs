using Microsoft.EntityFrameworkCore.Diagnostics;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using POSSystem.WPF.Business.Services.interfase;

namespace POSSystem.WPF.Shered.Proberty
{
    public class ExpenseManagementProberty : BaseModel
    {
        private readonly IExpenseService _expenseService;
        private readonly IReceiptVoucherService _receiptVoucherService;

        private ObservableCollection<Expense> _expenses = new();
        private ObservableCollection<ReceiptVoucher> _vouchers = new();
        private DateTime _startDate = DateTime.Today.AddMonths(-1);
        private DateTime _endDate = DateTime.Today;
        private string _selectedCategory = "";
        private ExpenseType? _selectedExpenseType;
        private bool _isExpensesVisible = true;
        private bool _isAccountsVisible = false;
        private bool _isVouchersVisible = false;

        private ExpenseAccountSummaryItem? _selectedExpenseAccount;
        private string _newAccountName = "";
        private string _accountSearchText = "";
        private bool _showOnlyActiveAccounts = false;
        private bool _isEditingAccountName = false;
        private string _editingAccountOriginalName = "";
        private string _editingAccountNewName = "";

        public Action<ExpenseAccountSummaryItem?>? OnSelectedAccountChanged { get; set; }
        public Action? OnAccountsFilterChanged { get; set; }

        public ExpenseManagementProberty(IExpenseService expenseService, IReceiptVoucherService receiptVoucherService)
        {
            _expenseService = expenseService;
            _receiptVoucherService = receiptVoucherService;
        }

        public ObservableCollection<Expense> Expenses
        {
            get => _expenses;
            set => SetProperty(ref _expenses, value);
        }

        public ObservableCollection<ReceiptVoucher> Vouchers
        {
            get => _vouchers;
            set => SetProperty(ref _vouchers, value);
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    POSSystem.WPF.Helpers.TaskExtensions.RunSafe(async () =>
                    {
                        await LoadExpensesAsync();
                        await LoadVouchersAsync();
                        OnAccountsFilterChanged?.Invoke();
                    });
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    POSSystem.WPF.Helpers.TaskExtensions.RunSafe(async () =>
                    {
                        await LoadExpensesAsync();
                        await LoadVouchersAsync();
                        OnAccountsFilterChanged?.Invoke();
                    });
                }
            }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    LoadExpensesAsync().SafeFireAndForget();
                    OnPropertyChanged(nameof(TotalExpenses));
                }
            }
        }

        public ExpenseType? SelectedExpenseType
        {
            get => _selectedExpenseType;
            set
            {
                if (SetProperty(ref _selectedExpenseType, value))
                {
                    LoadExpensesAsync().SafeFireAndForget();
                    OnPropertyChanged(nameof(TotalExpenses));
                }
            }
        }

        public decimal TotalExpenses => Expenses.Sum(e => e.Amount);
        public decimal TotalReceipts => Vouchers.Where(v => v.Type == VoucherType.Receipt).Sum(v => v.Amount);
        public decimal TotalPayments => Vouchers.Where(v => v.Type == VoucherType.Payment).Sum(v => v.Amount);
        public decimal NetAmount => TotalReceipts - TotalPayments;

        public List<ExpenseType> ExpenseTypes => new()
        {
            ExpenseType.Monthly,
            ExpenseType.Weekly,
            ExpenseType.Daily,
            ExpenseType.OneTime
        };

        public ObservableCollection<string> ExpenseCategories { get; } = new()
        {
            "الكل (جميع الحسابات)",
            "حساب مصاريف المطعم والتشغيل",
            "حساب أدوات وقرطاسية",
            "حساب مصاريف الكهرباء",
            "حساب مصاريف الزكاة",
            "حساب مصاريف الضرائب والواجبات",
            "حساب إيجار",
            "حساب مياه",
            "حساب غاز ووقود",
            "حساب صيانة ومعدات",
            "حساب مواد نظافة",
            "حساب رواتب وموظفين",
            "حساب إنترنت واتصالات",
            "حساب نثريات وأخرى"
        };

        public ObservableCollection<ExpenseAccountSummaryItem> ExpenseAccountsSummary { get; } = new();
        public ObservableCollection<Expense> SelectedAccountExpenses { get; } = new();

        public ExpenseAccountSummaryItem? SelectedExpenseAccount
        {
            get => _selectedExpenseAccount;
            set
            {
                if (SetProperty(ref _selectedExpenseAccount, value))
                {
                    OnSelectedAccountChanged?.Invoke(value);
                    NotifySelectedAccountChanged();
                }
            }
        }

        public string NewAccountName
        {
            get => _newAccountName;
            set => SetProperty(ref _newAccountName, value);
        }

        public bool IsEditingAccountName
        {
            get => _isEditingAccountName;
            set => SetProperty(ref _isEditingAccountName, value);
        }

        public string EditingAccountOriginalName
        {
            get => _editingAccountOriginalName;
            set => SetProperty(ref _editingAccountOriginalName, value);
        }

        public string EditingAccountNewName
        {
            get => _editingAccountNewName;
            set => SetProperty(ref _editingAccountNewName, value);
        }

        public string AccountSearchText
        {
            get => _accountSearchText;
            set
            {
                if (SetProperty(ref _accountSearchText, value))
                {
                    OnAccountsFilterChanged?.Invoke();
                }
            }
        }

        public bool ShowOnlyActiveAccounts
        {
            get => _showOnlyActiveAccounts;
            set
            {
                if (SetProperty(ref _showOnlyActiveAccounts, value))
                {
                    OnAccountsFilterChanged?.Invoke();
                }
            }
        }

        public int TotalAccountsCount => ExpenseAccountsSummary?.Count ?? 0;
        public int ActiveAccountsCount => ExpenseAccountsSummary?.Count(x => x.TransactionsCount > 0) ?? 0;
        public decimal AccountsGrandTotal => ExpenseAccountsSummary?.Sum(x => x.TotalAmount) ?? 0m;
        public decimal AccountsCashTotal => ExpenseAccountsSummary?.Sum(x => x.CashAmount) ?? 0m;
        public decimal AccountsWalletTotal => ExpenseAccountsSummary?.Sum(x => x.WalletAmount) ?? 0m;

        public string SelectedAccountTitle => SelectedExpenseAccount != null
            ? $"📄 كشف وتفاصيل حركات: {SelectedExpenseAccount.AccountName}"
            : "📄 اختر حساباً من القائمة اليمنى لاستعراض تفاصيله وحركاته";

        public string SelectedAccountNameOnly => SelectedExpenseAccount?.AccountName ?? "غير محدد";
        public int SelectedAccountTransactionsCount => SelectedAccountExpenses?.Count ?? SelectedExpenseAccount?.TransactionsCount ?? 0;
        public decimal SelectedAccountCashAmount => SelectedAccountExpenses != null && SelectedAccountExpenses.Any()
            ? SelectedAccountExpenses.Where(e => !e.IsWalletExpense).Sum(e => e.Amount)
            : (SelectedExpenseAccount?.CashAmount ?? 0m);
        public decimal SelectedAccountWalletAmount => SelectedAccountExpenses != null && SelectedAccountExpenses.Any()
            ? SelectedAccountExpenses.Where(e => e.IsWalletExpense).Sum(e => e.Amount)
            : (SelectedExpenseAccount?.WalletAmount ?? 0m);
        public decimal SelectedAccountTotalAmount => SelectedAccountExpenses != null && SelectedAccountExpenses.Any()
            ? SelectedAccountExpenses.Sum(e => e.Amount)
            : (SelectedExpenseAccount?.TotalAmount ?? 0m);
        public string SelectedAccountPercentageText => SelectedExpenseAccount?.PercentageText ?? "0%";

        public void NotifyAccountsSummaryChanged()
        {
            OnPropertyChanged(nameof(TotalAccountsCount));
            OnPropertyChanged(nameof(ActiveAccountsCount));
            OnPropertyChanged(nameof(AccountsGrandTotal));
            OnPropertyChanged(nameof(AccountsCashTotal));
            OnPropertyChanged(nameof(AccountsWalletTotal));
            NotifySelectedAccountChanged();
        }

        public void NotifySelectedAccountChanged()
        {
            OnPropertyChanged(nameof(SelectedAccountTitle));
            OnPropertyChanged(nameof(SelectedAccountNameOnly));
            OnPropertyChanged(nameof(SelectedAccountTransactionsCount));
            OnPropertyChanged(nameof(SelectedAccountCashAmount));
            OnPropertyChanged(nameof(SelectedAccountWalletAmount));
            OnPropertyChanged(nameof(SelectedAccountTotalAmount));
            OnPropertyChanged(nameof(SelectedAccountPercentageText));
        }

        public bool IsExpensesVisible
        {
            get => _isExpensesVisible;
            set => SetProperty(ref _isExpensesVisible, value);
        }

        public bool IsAccountsVisible
        {
            get => _isAccountsVisible;
            set => SetProperty(ref _isAccountsVisible, value);
        }

        public bool IsVouchersVisible
        {
            get => _isVouchersVisible;
            set => SetProperty(ref _isVouchersVisible, value);
        }

        private VoucherType? _selectedVoucherType;
        private VoucherTypeFilterItem? _selectedVoucherTypeItem;

        public List<VoucherTypeFilterItem> VoucherTypeFilterItems { get; } = new()
        {
            new() { Value = null, DisplayName = "جميع السندات (الكل)" },
            new() { Value = VoucherType.Payment, DisplayName = "سندات صرف فقط" },
            new() { Value = VoucherType.Receipt, DisplayName = "سندات قبض فقط" }
        };

        public VoucherTypeFilterItem? SelectedVoucherTypeItem
        {
            get => _selectedVoucherTypeItem ?? VoucherTypeFilterItems[0];
            set
            {
                if (SetProperty(ref _selectedVoucherTypeItem, value))
                {
                    _selectedVoucherType = value?.Value;
                    OnPropertyChanged(nameof(SelectedVoucherType));
                    LoadVouchersAsync().SafeFireAndForget();
                }
            }
        }

        public VoucherType? SelectedVoucherType
        {
            get => _selectedVoucherType;
            set
            {
                if (SetProperty(ref _selectedVoucherType, value))
                {
                    _selectedVoucherTypeItem = VoucherTypeFilterItems.FirstOrDefault(i => i.Value == value);
                    OnPropertyChanged(nameof(SelectedVoucherTypeItem));
                    LoadVouchersAsync().SafeFireAndForget();
                }
            }
        }

        public async Task LoadExpensesAsync()
        {
            try
            {
                IEnumerable<Expense> expenses = await _expenseService.GetAllExpensesAsync();
                expenses = expenses?.Where(e => e != null) ?? Enumerable.Empty<Expense>();

                if (!string.IsNullOrWhiteSpace(SelectedCategory) && SelectedCategory != "الكل (جميع الحسابات)")
                    expenses = expenses.Where(e => e != null && e.Category == SelectedCategory);

                if (SelectedExpenseType.HasValue)
                    expenses = expenses.Where(e => e != null && e.Type == SelectedExpenseType.Value);

                var validExpenses = expenses.Where(e => e != null).ToList();
                Expenses = new ObservableCollection<Expense>(validExpenses);
                OnPropertyChanged(nameof(TotalExpenses));
                OnPropertyChanged(nameof(NetAmount));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading expenses: {ex.Message}\n{ex.StackTrace}");
                Expenses = new ObservableCollection<Expense>();
                OnPropertyChanged(nameof(TotalExpenses));
                OnPropertyChanged(nameof(NetAmount));
                MessageBox.Show("حدث خطأ أثناء تحميل المصروفات. يرجى المحاولة مرة أخرى.", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task LoadVouchersAsync()
        {
            try
            {
                var vouchers = (await _receiptVoucherService.GetByDateRangeAsync(StartDate, EndDate))?.ToList() ?? new List<ReceiptVoucher>();
                if (!vouchers.Any())
                {
                    var allV = await _receiptVoucherService.GetAllVouchersAsync();
                    vouchers = allV?.ToList() ?? new List<ReceiptVoucher>();
                }

                if (SelectedVoucherType.HasValue)
                {
                    vouchers = vouchers.Where(v => v.Type == SelectedVoucherType.Value).ToList();
                }

                Vouchers = new ObservableCollection<ReceiptVoucher>(vouchers);
                OnPropertyChanged(nameof(TotalReceipts));
                OnPropertyChanged(nameof(TotalPayments));
                OnPropertyChanged(nameof(NetAmount));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل السندات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class VoucherTypeFilterItem
    {
        public VoucherType? Value { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }
}