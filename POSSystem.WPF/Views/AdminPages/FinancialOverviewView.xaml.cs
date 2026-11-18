using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using POSSystem.WPF.ViewModels;
using POSSystem.WPF.Views.Popups;
using POSSystem.WPF.Models;
using POSSystem.WPF.Business;
using POSSystem.WPF.Helpers;
using System.Threading.Tasks;

namespace POSSystem.WPF.Views.AdminPages
{
    public class BankReconciliationItem
    {
        public string DocumentNumber { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string PaymentMethodText { get; set; } = string.Empty;
        public string BankAccountName { get; set; } = string.Empty;
        public decimal RecordedSystemAmount { get; set; }
        public decimal ActualDepositedAmount { get; set; }
        public decimal DiscrepancyAmount => RecordedSystemAmount - ActualDepositedAmount;
        public string UserName { get; set; } = string.Empty;
        public string MatchStatusText { get; set; } = string.Empty;
    }

    public sealed class FinancialOverviewViewModel : BaseViewModel
    {
        private string _formAmountText = "";
        private string _formDepositorName = "";
        private string? _formSelectedSource;
        private string _formReferenceNumber = "";
        private DateTime? _formDepositDate = DateTime.Today;
        private string? _formAttachmentPath;
        private string? _formAttachmentFileName;
        private string _formNotes = "";
        private string _formErrorText = "";

        private decimal _todayDeposits;
        private decimal _weeklyDeposits;
        private decimal _monthlyDeposits;
        private decimal _totalApprovedDepositsAll;
        private int _approvedDepositsCount;
        private decimal _todayDiscrepancy;
        private decimal _weeklyDiscrepancy;
        private decimal _monthlyDiscrepancy;
        private Brush _todayDiscrepancyBrush = Brushes.Gray;
        private Brush _weeklyDiscrepancyBrush = Brushes.Gray;
        private Brush _monthlyDiscrepancyBrush = Brushes.Gray;
        private string _todayConversionDisplay = "—";
        private string _weeklyConversionDisplay = "—";
        private string _monthlyConversionDisplay = "—";
        private decimal _totalSystemDiscrepancy;
        private Brush _totalSystemDiscrepancyBrush = Brushes.Gray;
        private string _totalSystemConversionDisplay = "—";

        private decimal _todayRevenue;
        private decimal _weeklyRevenue;
        private decimal _monthlyRevenue;
        private decimal _totalSystemRevenue;

        // 9 Cards for Period Breakdown & Bank Reconciliation
        private decimal _periodTotalSalesRevenue;
        private decimal _periodCashSales;
        private decimal _periodBankDepositSales;
        private decimal _periodCreditSales;
        private decimal _periodCollectedDebtsTotal;
        private string _periodCollectedDebtsBreakdown = "";
        private decimal _periodExpectedCustomerBankDeposits;
        private decimal _periodInternalCashToBankTransfers;
        private decimal _periodBankReconciliationDiscrepancy;

        public decimal PeriodTotalSalesRevenue { get => _periodTotalSalesRevenue; private set => SetProperty(ref _periodTotalSalesRevenue, value); }
        public decimal PeriodCashSales { get => _periodCashSales; private set => SetProperty(ref _periodCashSales, value); }
        public decimal PeriodBankDepositSales { get => _periodBankDepositSales; private set => SetProperty(ref _periodBankDepositSales, value); }
        public decimal PeriodCreditSales { get => _periodCreditSales; private set => SetProperty(ref _periodCreditSales, value); }
        public decimal PeriodCollectedDebtsTotal { get => _periodCollectedDebtsTotal; private set => SetProperty(ref _periodCollectedDebtsTotal, value); }
        public string PeriodCollectedDebtsBreakdown { get => _periodCollectedDebtsBreakdown; private set => SetProperty(ref _periodCollectedDebtsBreakdown, value); }
        public decimal PeriodExpectedCustomerBankDeposits { get => _periodExpectedCustomerBankDeposits; private set => SetProperty(ref _periodExpectedCustomerBankDeposits, value); }
        public decimal PeriodInternalCashToBankTransfers { get => _periodInternalCashToBankTransfers; private set => SetProperty(ref _periodInternalCashToBankTransfers, value); }
        public decimal PeriodBankReconciliationDiscrepancy { get => _periodBankReconciliationDiscrepancy; private set => SetProperty(ref _periodBankReconciliationDiscrepancy, value); }

        // Filters
        public IReadOnlyList<string> PeriodFilterOptions { get; } = new[] { "اليوم", "الأسبوع", "الشهر", "فترة مخصصة" };
        private string _selectedPeriodFilter = "الشهر";
        public string SelectedPeriodFilter
        {
            get => _selectedPeriodFilter;
            set
            {
                if (SetProperty(ref _selectedPeriodFilter, value))
                {
                    ApplyPeriodShortcut(value);
                    _ = ReloadDataAsync();
                }
            }
        }

        private DateTime _filterFromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        public DateTime FilterFromDate
        {
            get => _filterFromDate;
            set { if (SetProperty(ref _filterFromDate, value)) _ = ReloadDataAsync(); }
        }

        private DateTime _filterToDate = DateTime.Today;
        public DateTime FilterToDate
        {
            get => _filterToDate;
            set { if (SetProperty(ref _filterToDate, value)) _ = ReloadDataAsync(); }
        }

        public ObservableCollection<string> BankFilterOptions { get; } = new() { "الكل" };
        private string _selectedBankFilter = "الكل";
        public string SelectedBankFilter
        {
            get => _selectedBankFilter;
            set { if (SetProperty(ref _selectedBankFilter, value)) _ = ReloadDataAsync(); }
        }

        public IReadOnlyList<string> MatchStatusFilterOptions { get; } = new[] { "الكل", "مطابق", "غير مطابق / فرق", "تحويل داخلي" };
        private string _selectedMatchStatusFilter = "الكل";
        public string SelectedMatchStatusFilter
        {
            get => _selectedMatchStatusFilter;
            set { if (SetProperty(ref _selectedMatchStatusFilter, value)) _ = ReloadDataAsync(); }
        }

        public ObservableCollection<BankReconciliationItem> ReconciliationItems { get; } = new();
        public ObservableCollection<WalletDepositSummary> WalletSummaries { get; } = new();

        private bool _hasWalletSummaries;
        public bool HasWalletSummaries
        {
            get => _hasWalletSummaries;
            private set => SetProperty(ref _hasWalletSummaries, value);
        }

        private decimal _totalWalletAmount;
        public decimal TotalWalletAmount
        {
            get => _totalWalletAmount;
            private set => SetProperty(ref _totalWalletAmount, value);
        }

        private int _totalWalletCount;
        public int TotalWalletCount
        {
            get => _totalWalletCount;
            private set => SetProperty(ref _totalWalletCount, value);
        }

        private string _totalWalletSummaryText = "";
        public string TotalWalletSummaryText
        {
            get => _totalWalletSummaryText;
            private set => SetProperty(ref _totalWalletSummaryText, value);
        }

        public decimal TotalSystemRevenue
        {
            get => _totalSystemRevenue;
            private set => SetProperty(ref _totalSystemRevenue, value);
        }

        public decimal TodayRevenue
        {
            get => _todayRevenue;
            private set => SetProperty(ref _todayRevenue, value);
        }

        public decimal WeeklyRevenue
        {
            get => _weeklyRevenue;
            private set => SetProperty(ref _weeklyRevenue, value);
        }

        public decimal MonthlyRevenue
        {
            get => _monthlyRevenue;
            private set => SetProperty(ref _monthlyRevenue, value);
        }

        public decimal TodayDeposits
        {
            get => _todayDeposits;
            private set => SetProperty(ref _todayDeposits, value);
        }

        public decimal WeeklyDeposits
        {
            get => _weeklyDeposits;
            private set => SetProperty(ref _weeklyDeposits, value);
        }

        public decimal MonthlyDeposits
        {
            get => _monthlyDeposits;
            private set => SetProperty(ref _monthlyDeposits, value);
        }

        public decimal TotalApprovedDepositsAll
        {
            get => _totalApprovedDepositsAll;
            private set => SetProperty(ref _totalApprovedDepositsAll, value);
        }

        public int ApprovedDepositsCount
        {
            get => _approvedDepositsCount;
            private set => SetProperty(ref _approvedDepositsCount, value);
        }

        public decimal TodayDiscrepancy
        {
            get => _todayDiscrepancy;
            private set => SetProperty(ref _todayDiscrepancy, value);
        }

        public decimal WeeklyDiscrepancy
        {
            get => _weeklyDiscrepancy;
            private set => SetProperty(ref _weeklyDiscrepancy, value);
        }

        public decimal MonthlyDiscrepancy
        {
            get => _monthlyDiscrepancy;
            private set => SetProperty(ref _monthlyDiscrepancy, value);
        }

        public Brush TodayDiscrepancyBrush
        {
            get => _todayDiscrepancyBrush;
            private set => SetProperty(ref _todayDiscrepancyBrush, value);
        }

        public Brush WeeklyDiscrepancyBrush
        {
            get => _weeklyDiscrepancyBrush;
            private set => SetProperty(ref _weeklyDiscrepancyBrush, value);
        }

        public Brush MonthlyDiscrepancyBrush
        {
            get => _monthlyDiscrepancyBrush;
            private set => SetProperty(ref _monthlyDiscrepancyBrush, value);
        }

        public decimal TotalSystemDiscrepancy
        {
            get => _totalSystemDiscrepancy;
            private set => SetProperty(ref _totalSystemDiscrepancy, value);
        }

        public Brush TotalSystemDiscrepancyBrush
        {
            get => _totalSystemDiscrepancyBrush;
            private set => SetProperty(ref _totalSystemDiscrepancyBrush, value);
        }

        public string TodayConversionDisplay
        {
            get => _todayConversionDisplay;
            private set => SetProperty(ref _todayConversionDisplay, value);
        }

        public string WeeklyConversionDisplay
        {
            get => _weeklyConversionDisplay;
            private set => SetProperty(ref _weeklyConversionDisplay, value);
        }

        public string MonthlyConversionDisplay
        {
            get => _monthlyConversionDisplay;
            private set => SetProperty(ref _monthlyConversionDisplay, value);
        }

        public string TotalSystemConversionDisplay
        {
            get => _totalSystemConversionDisplay;
            private set => SetProperty(ref _totalSystemConversionDisplay, value);
        }

        public ObservableCollection<BankDeposit> DepositRecords { get; } = new();

        public IReadOnlyList<string> DepositSourceOptions => FinancialChannelHelper.GetActiveWalletsList();

        public Action? RequestShowDepositForm { get; set; }
        public Action<BankDeposit>? RequestShowDepositDetail { get; set; }

        public string FormAmountText
        {
            get => _formAmountText;
            set => SetProperty(ref _formAmountText, value);
        }

        public string FormDepositorName
        {
            get => _formDepositorName;
            set => SetProperty(ref _formDepositorName, value);
        }

        public string? FormSelectedSource
        {
            get => _formSelectedSource;
            set => SetProperty(ref _formSelectedSource, value);
        }

        public string FormReferenceNumber
        {
            get => _formReferenceNumber;
            set => SetProperty(ref _formReferenceNumber, value);
        }

        public DateTime? FormDepositDate
        {
            get => _formDepositDate;
            set => SetProperty(ref _formDepositDate, value);
        }

        public string? FormAttachmentFileName
        {
            get => _formAttachmentFileName;
            set => SetProperty(ref _formAttachmentFileName, value);
        }

        public string? FormAttachmentPath
        {
            get => _formAttachmentPath;
            set => SetProperty(ref _formAttachmentPath, value);
        }

        public string FormNotes
        {
            get => _formNotes;
            set => SetProperty(ref _formNotes, value);
        }

        public string FormErrorText
        {
            get => _formErrorText;
            set => SetProperty(ref _formErrorText, value);
        }

        public RelayCommand OpenDepositModalCommand { get; }
        public RelayCommand BrowseAttachmentCommand { get; }
        public RelayCommand<BankDeposit> ShowDepositDetailCommand { get; }
        public RelayCommand<BankDeposit> ApproveDepositCommand { get; }
        public RelayCommand<BankDeposit> RejectDepositCommand { get; }
        public RelayCommand RefreshCommand { get; }

        public FinancialOverviewViewModel()
        {
            TodayRevenue = 0;
            WeeklyRevenue = 0;
            MonthlyRevenue = 0;
            TotalSystemRevenue = 0;

            OpenDepositModalCommand = new RelayCommand(OpenDepositModal);
            BrowseAttachmentCommand = new RelayCommand(BrowseAttachment);
            ShowDepositDetailCommand = new RelayCommand<BankDeposit>(ShowDepositDetail, r => r != null);
            ApproveDepositCommand = new RelayCommand<BankDeposit>(ApproveDeposit, r => r is { Status: "Pending" });
            RejectDepositCommand = new RelayCommand<BankDeposit>(RejectDeposit, r => r is { Status: "Pending" });

            DepositRecords.CollectionChanged += OnDepositsChanged;
            RefreshCommand = new RelayCommand(async () => await ReloadDataAsync());
            try { ServiceLocator.ShiftService.ShiftEnded += OnShiftEnded; } catch { }
            _ = InitializeAsync();
        }

        private void ApplyPeriodShortcut(string period)
        {
            var today = DateTime.Today;
            if (period == "اليوم")
            {
                _filterFromDate = today;
                _filterToDate = today;
            }
            else if (period == "الأسبوع")
            {
                _filterFromDate = today.AddDays(-6);
                _filterToDate = today;
            }
            else if (period == "الشهر")
            {
                _filterFromDate = new DateTime(today.Year, today.Month, 1);
                _filterToDate = today;
            }
            OnPropertyChanged(nameof(FilterFromDate));
            OnPropertyChanged(nameof(FilterToDate));
        }

        private void OnShiftEnded(object? sender, EventArgs e)
        {
            Application.Current?.Dispatcher?.InvokeAsync(async () => await ReloadDataAsync());
        }

        public async Task ReloadDataAsync()
        {
            await InitializeAsync();
        }

        private static bool IsInternalTransferDeposit(BankDeposit d)
        {
            return (d.Notes != null && (d.Notes.Contains("[تحويل داخلي") || d.Notes.Contains("تحويل من الصندوق"))) ||
                   (d.DepositorName != null && (d.DepositorName.Contains("من الصندوق") || d.DepositorName.Contains("تحويل داخلي")));
        }

        private static bool IsBankOrDepositOrder(Order o)
        {
            return o.IsDepositPayment ||
                   o.PaymentMethod == PaymentMethod.MobilePayment ||
                   o.PaymentMethod == PaymentMethod.BankTransfer ||
                   o.PaymentMethod == PaymentMethod.DebitCard ||
                   o.PaymentMethod == PaymentMethod.CreditCard ||
                   (o.Notes != null && o.Notes.Contains("[إيداع:"));
        }

        private async Task InitializeAsync()
        {
            try
            {
                var today = DateTime.Today;
                var weekStart = today.AddDays(-6);
                var monthStart = new DateTime(today.Year, today.Month, 1);
                var systemStartDate = today.AddYears(-3);

                var context = ServiceLocator.UnitOfWork.GetContext();
                var depositService = ServiceLocator.BankDepositService;

                // Load all countable orders & returns & debt collection vouchers in last 3 years
                var allOrders = await context.Orders.AsNoTracking()
                    .Where(o => o.CreatedAt >= systemStartDate &&
                                (o.Status == OrderStatus.Completed || o.Status == OrderStatus.PartiallyReturned || o.Status == OrderStatus.Returned))
                    .ToListAsync();

                var allReturns = await context.ReturnOrders.AsNoTracking()
                    .Where(r => r.ReturnDate >= systemStartDate)
                    .ToListAsync();

                var allReceiptVouchers = await context.ReceiptVouchers.AsNoTracking()
                    .Where(v => v.Type == VoucherType.Receipt && v.VoucherDate >= systemStartDate)
                    .ToListAsync();

                var deposits = (await depositService.GetAllDepositsAsync()).OrderByDescending(x => x.CreatedAt).ToList();
                _allDepositRecords = deposits;
                DepositRecords.Clear();
                foreach (var d in deposits)
                {
                    DepositRecords.Add(d);
                }

                // Populate BankFilterOptions dynamically
                var distinctBanks = deposits.Select(d => d.Source)
                    .Concat(allOrders.Where(IsBankOrDepositOrder).Select(o => o.DepositBankOrWallet ?? "حساب بنكي / محفظة"))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s!.Trim())
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();

                foreach (var b in distinctBanks)
                {
                    if (!BankFilterOptions.Contains(b))
                        BankFilterOptions.Add(b);
                }

                // Helper to compute expected customer bank inflows (Bank Deposit Sales + Bank Debt Collections - Bank Returns)
                // NEVER mix Cash Sales or OnCredit Sales into Bank Deposit Reconciliation!
                decimal ComputeExpectedCustomerBankInflow(DateTime from, DateTime to)
                {
                    var s = from.Date;
                    var e = to.Date.AddDays(1).AddTicks(-1);

                    decimal bankOrderSales = allOrders
                        .Where(o => o.CreatedAt >= s && o.CreatedAt <= e)
                        .Sum(o =>
                        {
                            if (o.PaymentMethod == PaymentMethod.Split)
                                return OrderSplitHelper.ParseSplit(o.Notes)?.CardAmount ?? 0m;
                            if (IsBankOrDepositOrder(o))
                                return o.DepositAmount > 0 ? o.DepositAmount : o.Total;
                            return 0m;
                        });

                    decimal bankDebtCollections = allReceiptVouchers
                        .Where(v => v.VoucherDate >= s && v.VoucherDate <= e &&
                                    v.PaymentMethod != PaymentMethod.Cash &&
                                    (v.CustomerDebtId != null || (v.Notes != null && v.Notes.Contains("دين"))))
                        .Sum(v => v.Amount);

                    decimal bankSalesReturns = allReturns
                        .Where(r => r.ReturnDate >= s && r.ReturnDate <= e &&
                                    r.ReturnMethod != PaymentMethod.Cash &&
                                    r.ReturnMethod != PaymentMethod.OnCredit &&
                                    r.ReturnMethod != PaymentMethod.Remake)
                        .Sum(r => r.TotalAmount);

                    return Math.Max(0m, bankOrderSales + bankDebtCollections - bankSalesReturns);
                }

                TodayRevenue = ComputeExpectedCustomerBankInflow(today, today);
                WeeklyRevenue = ComputeExpectedCustomerBankInflow(weekStart, today);
                MonthlyRevenue = ComputeExpectedCustomerBankInflow(monthStart, today);
                TotalSystemRevenue = ComputeExpectedCustomerBankInflow(systemStartDate, today);

                // Compute Selected Period 9-Card Summary
                var pStart = FilterFromDate.Date;
                var pEnd = FilterToDate.Date.AddDays(1).AddTicks(-1);

                var periodOrders = allOrders.Where(o => o.CreatedAt >= pStart && o.CreatedAt <= pEnd).ToList();
                var periodReturns = allReturns.Where(r => r.ReturnDate >= pStart && r.ReturnDate <= pEnd).ToList();
                var periodVouchers = allReceiptVouchers.Where(v => v.VoucherDate >= pStart && v.VoucherDate <= pEnd).ToList();

                decimal pGross = periodOrders.Sum(o => o.Total);
                decimal pRet = periodReturns.Sum(r => r.TotalAmount);
                PeriodTotalSalesRevenue = pGross - pRet;

                PeriodCashSales = periodOrders.Where(o => o.PaymentMethod == PaymentMethod.Cash && !IsBankOrDepositOrder(o)).Sum(o => o.Total)
                    + periodOrders.Where(o => o.PaymentMethod == PaymentMethod.Split).Sum(o => OrderSplitHelper.ParseSplit(o.Notes)?.CashAmount ?? 0m)
                    - periodReturns.Where(r => r.ReturnMethod == PaymentMethod.Cash).Sum(r => r.TotalAmount);

                PeriodBankDepositSales = periodOrders.Where(IsBankOrDepositOrder).Sum(o => o.DepositAmount > 0 ? o.DepositAmount : o.Total)
                    + periodOrders.Where(o => o.PaymentMethod == PaymentMethod.Split).Sum(o => OrderSplitHelper.ParseSplit(o.Notes)?.CardAmount ?? 0m)
                    - periodReturns.Where(r => r.ReturnMethod != PaymentMethod.Cash && r.ReturnMethod != PaymentMethod.OnCredit && r.ReturnMethod != PaymentMethod.Remake).Sum(r => r.TotalAmount);

                PeriodCreditSales = periodOrders.Where(o => o.PaymentMethod == PaymentMethod.OnCredit).Sum(o => o.Total)
                    - periodReturns.Where(r => r.ReturnMethod == PaymentMethod.OnCredit).Sum(r => r.TotalAmount);

                var debtVouchers = periodVouchers.Where(v => v.CustomerDebtId != null || (v.Notes != null && v.Notes.Contains("سداد دين"))).ToList();
                decimal debtCash = debtVouchers.Where(v => v.PaymentMethod == PaymentMethod.Cash).Sum(v => v.Amount);
                decimal debtBank = debtVouchers.Where(v => v.PaymentMethod != PaymentMethod.Cash).Sum(v => v.Amount);
                PeriodCollectedDebtsTotal = debtCash + debtBank;
                PeriodCollectedDebtsBreakdown = $"نقداً: {debtCash:N0} | بنكياً: {debtBank:N0}";

                PeriodExpectedCustomerBankDeposits = Math.Max(0m, PeriodBankDepositSales + debtBank);

                var periodApprovedDeposits = deposits.Where(d => d.Status == "Approved" && d.DepositDate.Date >= pStart.Date && d.DepositDate.Date <= pEnd.Date).ToList();
                PeriodInternalCashToBankTransfers = periodApprovedDeposits.Where(IsInternalTransferDeposit).Sum(d => d.Amount);
                decimal actualCustomerBankDepositsInPeriod = periodApprovedDeposits.Where(d => !IsInternalTransferDeposit(d)).Sum(d => d.Amount)
                    + PeriodBankDepositSales; // Orders with inline verified deposit tags count as recorded customer bank deposits unless overridden

                // If explicit BankDeposits rows exist for customer sales (not internal transfers), compare PeriodExpectedCustomerBankDeposits against explicit customer BankDeposits + inline verified
                var explicitCustomerBankDeposits = periodApprovedDeposits.Where(d => !IsInternalTransferDeposit(d)).ToList();
                decimal actualCompared = explicitCustomerBankDeposits.Count > 0
                    ? explicitCustomerBankDeposits.Sum(d => d.Amount)
                    : PeriodBankDepositSales;

                PeriodBankReconciliationDiscrepancy = PeriodExpectedCustomerBankDeposits - actualCompared;

                BuildReconciliationItemsList(periodOrders, debtVouchers, periodApprovedDeposits);
                Recalculate();
                await LoadWalletSummariesAsync(pStart, pEnd, periodOrders);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BuildReconciliationItemsList(
            List<Order> periodOrders,
            List<ReceiptVoucher> debtVouchers,
            List<BankDeposit> periodDeposits)
        {
            var items = new List<BankReconciliationItem>();

            foreach (var o in periodOrders.Where(IsBankOrDepositOrder))
            {
                string bank = !string.IsNullOrWhiteSpace(o.DepositBankOrWallet) ? o.DepositBankOrWallet.Trim() : "حساب بنكي / محفظة";
                if (SelectedBankFilter != "الكل" && !bank.Contains(SelectedBankFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                decimal sysAmt = o.Total;
                decimal depAmt = o.DepositAmount > 0 ? o.DepositAmount : o.Total;
                string status = (sysAmt == depAmt) ? "مطابق" : "غير مطابق / فرق";

                if (SelectedMatchStatusFilter != "الكل" && SelectedMatchStatusFilter != status)
                    continue;

                items.Add(new BankReconciliationItem
                {
                    DocumentNumber = $"فاتورة #{o.OrderNumber}",
                    TransactionDate = o.CreatedAt,
                    PaymentMethodText = "مبيعات إيداع بنكي",
                    BankAccountName = bank,
                    RecordedSystemAmount = sysAmt,
                    ActualDepositedAmount = depAmt,
                    UserName = $"مستخدم #{o.UserId}",
                    MatchStatusText = status
                });
            }

            foreach (var v in debtVouchers.Where(x => x.PaymentMethod != PaymentMethod.Cash))
            {
                string bank = FinancialChannelHelper.GetChannelDisplayName(FinancialChannelHelper.ExtractChannel(v.PaymentMethod, v.Notes));
                if (SelectedBankFilter != "الكل" && !bank.Contains(SelectedBankFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                string status = "مطابق";
                if (SelectedMatchStatusFilter != "الكل" && SelectedMatchStatusFilter != status)
                    continue;

                items.Add(new BankReconciliationItem
                {
                    DocumentNumber = $"سند قبض دين #{v.VoucherNumber}",
                    TransactionDate = v.VoucherDate,
                    PaymentMethodText = "تحصيل دين عن طريق البنك",
                    BankAccountName = bank,
                    RecordedSystemAmount = v.Amount,
                    ActualDepositedAmount = v.Amount,
                    UserName = v.CustomerName,
                    MatchStatusText = status
                });
            }

            foreach (var d in periodDeposits)
            {
                string bank = d.Source ?? "حساب بنكي";
                if (SelectedBankFilter != "الكل" && !bank.Contains(SelectedBankFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                bool isInternal = IsInternalTransferDeposit(d);
                string status = isInternal ? "تحويل داخلي" : "سند إيداع معتمد";
                if (SelectedMatchStatusFilter == "تحويل داخلي" && !isInternal)
                    continue;
                if (SelectedMatchStatusFilter == "مطابق" && isInternal)
                    continue;

                items.Add(new BankReconciliationItem
                {
                    DocumentNumber = $"إيداع #{d.ReferenceNumber}",
                    TransactionDate = d.DepositDate,
                    PaymentMethodText = isInternal ? "توريد من الصندوق للبنك (داخلي)" : "سند إيداع بنكي",
                    BankAccountName = bank,
                    RecordedSystemAmount = isInternal ? 0m : d.Amount,
                    ActualDepositedAmount = d.Amount,
                    UserName = d.DepositorName,
                    MatchStatusText = status
                });
            }

            _allReconciliationItems = items.OrderByDescending(i => i.TransactionDate).ToList();
            ApplySearchFilter(_searchQuery);
        }

        private List<BankReconciliationItem> _allReconciliationItems = new();
        private List<BankDeposit> _allDepositRecords = new();
        private string _searchQuery = string.Empty;

        public void ApplySearchFilter(string query)
        {
            _searchQuery = query?.Trim() ?? string.Empty;
            var q = _searchQuery;

            ReconciliationItems.Clear();
            var filteredRec = string.IsNullOrWhiteSpace(q)
                ? _allReconciliationItems
                : _allReconciliationItems.Where(i =>
                    (i.DocumentNumber != null && i.DocumentNumber.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (i.BankAccountName != null && i.BankAccountName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (i.PaymentMethodText != null && i.PaymentMethodText.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (i.UserName != null && i.UserName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (i.MatchStatusText != null && i.MatchStatusText.Contains(q, StringComparison.OrdinalIgnoreCase))).ToList();

            foreach (var item in filteredRec)
            {
                ReconciliationItems.Add(item);
            }

            if (_allDepositRecords.Count > 0)
            {
                DepositRecords.Clear();
                var filteredDeps = string.IsNullOrWhiteSpace(q)
                    ? _allDepositRecords
                    : _allDepositRecords.Where(d =>
                        (d.DepositorName != null && d.DepositorName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                        (d.Source != null && d.Source.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                        (d.ReferenceNumber != null && d.ReferenceNumber.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                        (d.Notes != null && d.Notes.Contains(q, StringComparison.OrdinalIgnoreCase))).ToList();
                foreach (var d in filteredDeps)
                {
                    DepositRecords.Add(d);
                }
            }
        }

        private Task LoadWalletSummariesAsync(DateTime startDate, DateTime endDate, List<Order> periodOrders)
        {
            try
            {
                var depositOrders = periodOrders
                    .Where(IsBankOrDepositOrder)
                    .ToList();

                WalletSummaries.Clear();
                if (depositOrders.Count > 0)
                {
                    var groups = depositOrders
                        .GroupBy(o => new
                        {
                            Bank = !string.IsNullOrWhiteSpace(o.DepositBankOrWallet) ? o.DepositBankOrWallet.Trim() : "حساب بنكي / محفظة",
                            Currency = !string.IsNullOrWhiteSpace(o.DepositCurrency) ? o.DepositCurrency.Trim() : "ريال يمني"
                        })
                        .OrderBy(g => g.Key.Bank);

                    decimal total = 0;
                    int count = 0;
                    foreach (var g in groups)
                    {
                        decimal sum = g.Sum(x => x.DepositAmount > 0 ? x.DepositAmount : x.Total);
                        int c = g.Count();
                        total += sum;
                        count += c;

                        WalletSummaries.Add(new WalletDepositSummary
                        {
                            BankOrWallet = g.Key.Bank,
                            Currency = g.Key.Currency,
                            TotalAmount = sum,
                            OrderCount = c
                        });
                    }

                    TotalWalletAmount = total;
                    TotalWalletCount = count;
                    TotalWalletSummaryText = $"إجمالي الإيداعات: {total:N0} ريال ({count} عملية)";
                    HasWalletSummaries = true;
                }
                else
                {
                    TotalWalletAmount = 0;
                    TotalWalletCount = 0;
                    TotalWalletSummaryText = "لا توجد إيداعات محافظ إلكترونية";
                    HasWalletSummaries = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading wallet summaries in FinancialOverview: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        private void OnDepositsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Recalculate();

        private void ShowDepositDetail(BankDeposit? r)
        {
            if (r == null) return;
            RequestShowDepositDetail?.Invoke(r);
        }

        private void OpenDepositModal()
        {
            FormErrorText = "";
            FormAmountText = "";
            FormDepositorName = "";
            FormSelectedSource = null;
            FormReferenceNumber = "";
            FormDepositDate = DateTime.Today;
            FormAttachmentPath = null;
            FormAttachmentFileName = null;
            FormNotes = "";
            RequestShowDepositForm?.Invoke();
        }

        private void BrowseAttachment()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "صور وملفات|*.png;*.jpg;*.jpeg;*.pdf;*.bmp|جميع الملفات|*.*",
                Title = "اختر مرفق السند"
            };
            if (dlg.ShowDialog() == true)
            {
                FormAttachmentPath = dlg.FileName;
                FormAttachmentFileName = dlg.SafeFileName;
            }
        }

        private async void ApproveDeposit(BankDeposit? r)
        {
            if (r == null || r.Status != "Pending") return;
            var confirm = MessageBox.Show(
                "هل أنت متأكد من اعتماد هذا السند؟",
                "تأكيد الاعتماد",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No,
                MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            if (confirm != MessageBoxResult.Yes) return;
            r.Status = "Approved";
            await ServiceLocator.BankDepositService.UpdateDepositAsync(r);
            await ReloadDataAsync();
            ApproveDepositCommand.RaiseCanExecuteChanged();
            RejectDepositCommand.RaiseCanExecuteChanged();
        }

        private async void RejectDeposit(BankDeposit? r)
        {
            if (r == null || r.Status != "Pending") return;
            r.Status = "Rejected";
            await ServiceLocator.BankDepositService.UpdateDepositAsync(r);
            await ReloadDataAsync();
            ApproveDepositCommand.RaiseCanExecuteChanged();
            RejectDepositCommand.RaiseCanExecuteChanged();
        }

        private static string NormalizeArabicDigits(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var chars = input.Trim().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] >= '\u0660' && chars[i] <= '\u0669')
                    chars[i] = (char)('0' + (chars[i] - '\u0660'));
                else if (chars[i] >= '\u06F0' && chars[i] <= '\u06F9')
                    chars[i] = (char)('0' + (chars[i] - '\u06F0'));
                else if (chars[i] == '\u066B')
                    chars[i] = '.';
                else if (chars[i] == '\u066C')
                    chars[i] = ',';
            }
            return new string(chars).Replace(",", "");
        }

        public async Task<bool> TrySaveDepositAsync()
        {
            FormErrorText = "";

            var normalizedAmt = NormalizeArabicDigits(FormAmountText);
            if (string.IsNullOrWhiteSpace(normalizedAmt) ||
                (!decimal.TryParse(normalizedAmt, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) &&
                 !decimal.TryParse(normalizedAmt, NumberStyles.Any, CultureInfo.CurrentCulture, out amount)) ||
                amount <= 0)
            {
                FormErrorText = "يرجى إدخال مبلغ صحيح أكبر من صفر.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(FormDepositorName))
            {
                FormErrorText = "اسم المودع مطلوب.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(FormSelectedSource))
            {
                FormErrorText = "يرجى اختيار المحفظة أو مصدر الإيداع.";
                return false;
            }

            var refTrim = string.IsNullOrWhiteSpace(FormReferenceNumber)
                ? $"DEP-{DateTime.Now:yyyyMMddHHmmss}"
                : FormReferenceNumber.Trim();

            var depDate = FormDepositDate ?? DateTime.Today;

            if (DepositRecords.Any(x => !string.IsNullOrWhiteSpace(x.ReferenceNumber) && string.Equals(x.ReferenceNumber.Trim(), refTrim, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(
                    "تنبيه: يوجد سند مسجّل مسبقاً بنفس رقم المرجع. يرجى التحقق. يمكنك المتابعة بالحفظ إذا كان ذلك مقصوداً.",
                    "رقم مرجع مكرر",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning,
                    MessageBoxResult.OK,
                    MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            }

            var rec = new BankDeposit
            {
                DepositDate = depDate.Date,
                Amount = amount,
                DepositorName = FormDepositorName.Trim(),
                Source = FormSelectedSource.Trim(),
                ReferenceNumber = refTrim,
                Status = "Approved",
                CreatedAt = DateTime.Now,
                CreatedBy = AdminDashboardWindow._currentUser?.Name ?? "مدير النظام",
                AttachmentPath = FormAttachmentPath,
                AttachmentFileName = FormAttachmentFileName,
                Notes = string.IsNullOrWhiteSpace(FormNotes) ? null : FormNotes.Trim()
            };

            await ServiceLocator.BankDepositService.CreateDepositAsync(rec);
            await ReloadDataAsync();
            return true;
        }

        private void Recalculate()
        {
            var today = DateTime.Today;
            var weekStart = today.AddDays(-6);
            // Exclude internal cashbox-to-bank transfers from customer deposit reconciliation
            var approvedCustomerDeposits = DepositRecords
                .Where(r => r.Status == "Approved" && !IsInternalTransferDeposit(r))
                .ToList();

            decimal SumToday(IEnumerable<BankDeposit> q) =>
                q.Where(r => r.DepositDate.Date == today).Sum(r => r.Amount);

            decimal SumWeek(IEnumerable<BankDeposit> q) =>
                q.Where(r => r.DepositDate.Date >= weekStart && r.DepositDate.Date <= today).Sum(r => r.Amount);

            decimal SumMonth(IEnumerable<BankDeposit> q) =>
                q.Where(r => r.DepositDate.Year == today.Year && r.DepositDate.Month == today.Month).Sum(r => r.Amount);

            TodayDeposits = SumToday(approvedCustomerDeposits);
            WeeklyDeposits = SumWeek(approvedCustomerDeposits);
            MonthlyDeposits = SumMonth(approvedCustomerDeposits);

            TotalApprovedDepositsAll = approvedCustomerDeposits.Sum(r => r.Amount);
            ApprovedDepositsCount = approvedCustomerDeposits.Count;

            TodayDiscrepancy = TodayRevenue - TodayDeposits;
            WeeklyDiscrepancy = WeeklyRevenue - WeeklyDeposits;
            MonthlyDiscrepancy = MonthlyRevenue - MonthlyDeposits;
            TotalSystemDiscrepancy = TotalSystemRevenue - TotalApprovedDepositsAll;

            TodayDiscrepancyBrush = BrushForDiscrepancy(TodayDiscrepancy);
            WeeklyDiscrepancyBrush = BrushForDiscrepancy(WeeklyDiscrepancy);
            MonthlyDiscrepancyBrush = BrushForDiscrepancy(MonthlyDiscrepancy);
            TotalSystemDiscrepancyBrush = BrushForDiscrepancy(TotalSystemDiscrepancy);

            TodayConversionDisplay = FormatConversion(TodayDeposits, TodayRevenue);
            WeeklyConversionDisplay = FormatConversion(WeeklyDeposits, WeeklyRevenue);
            MonthlyConversionDisplay = FormatConversion(MonthlyDeposits, MonthlyRevenue);
            TotalSystemConversionDisplay = FormatConversion(TotalApprovedDepositsAll, TotalSystemRevenue);
        }

        private static string FormatConversion(decimal deposits, decimal revenue)
        {
            if (revenue == 0) return "—";
            return $"{(deposits / revenue * 100m):N1}٪";
        }

        private static Brush BrushForDiscrepancy(decimal discrepancy)
        {
            SolidColorBrush b;
            if (discrepancy > 0)
                b = new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x06));
            else if (discrepancy < 0)
                b = new SolidColorBrush(Color.FromRgb(0xDC, 0x35, 0x45));
            else
                b = new SolidColorBrush(Color.FromRgb(0x6C, 0x75, 0x7D));
            if (b.CanFreeze) b.Freeze();
            return b;
        }
    }

    public partial class FinancialOverviewView : UserControl, POSSystem.WPF.Services.ISearchable
    {
        public FinancialOverviewView()
        {
            InitializeComponent();
            var vm = new FinancialOverviewViewModel();
            DataContext = vm;
            Loaded += async (s, e) => { if (DataContext is FinancialOverviewViewModel vm) await vm.ReloadDataAsync(); };
            vm.RequestShowDepositForm = () =>
            {
                if (Window.GetWindow(this) is AdminDashboardWindow w)
                {
                    var form = new BankDepositFormControl(vm, () => w.CloseOverlay());
                    w.ShowOverlay(form);
                }
            };
            vm.RequestShowDepositDetail = r =>
            {
                if (Window.GetWindow(this) is AdminDashboardWindow w)
                    w.ShowOverlay(new BankDepositDetailControl(r, () => w.CloseOverlay()));
            };
        }

        public void ApplySearch(string query)
        {
            if (DataContext is FinancialOverviewViewModel vm)
            {
                vm.ApplySearchFilter(query);
            }
        }

        private void DiscrepancyCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
        }

        private static bool IsInsideDataGridInteractiveChrome(DependencyObject? src)
        {
            while (src != null)
            {
                switch (src)
                {
                    case Button:
                    case System.Windows.Controls.Primitives.ScrollBar:
                    case System.Windows.Controls.Primitives.Thumb:
                        return true;
                }
                src = VisualTreeHelper.GetParent(src);
            }
            return false;
        }

        private static DataGridRow? FindDataGridRow(DependencyObject? src)
        {
            while (src != null)
            {
                if (src is DataGridRow row) return row;
                src = VisualTreeHelper.GetParent(src);
            }
            return null;
        }

        private void DepositsGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not FinancialOverviewViewModel vm) return;
            if (IsInsideDataGridInteractiveChrome(e.OriginalSource as DependencyObject)) return;
            var row = FindDataGridRow(e.OriginalSource as DependencyObject);
            if (row?.Item is BankDeposit rec)
                vm.RequestShowDepositDetail?.Invoke(rec);
        }

        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent?.RaiseEvent(eventArg);
            }
        }
    }
}
