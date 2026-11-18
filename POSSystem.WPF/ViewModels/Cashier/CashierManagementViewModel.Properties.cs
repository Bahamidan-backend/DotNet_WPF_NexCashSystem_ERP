using System;
using System.Collections.ObjectModel;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Models.Helper;
using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.ViewModels.Reports;
using POSSystem.WPF.Data.UnitOfWork;

namespace POSSystem.WPF.ViewModels.Cashier
{
    public partial class CashierManagementViewModel
    {
        private readonly IReportService _reportService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly System.Threading.SemaphoreSlim _dbSemaphore = new System.Threading.SemaphoreSlim(1, 1);
        
        private ObservableCollection<CashierSalesReport> _cashierSalesReports;
        private ObservableCollection<DailySalesReport> _dailySalesReports;
        private ObservableCollection<DeliveryReport> _deliveryReports;
        private Pagination<ShiftReport> _shiftPager;
        private ObservableCollection<CashBoxReport> _cashBoxReports;
        
        private decimal _totalShiftSalesSum;
        private int _totalInvoiceCountSum;
        private int _totalReturnedOrdersCountSum;
        private decimal _totalReturnedOrdersAmountSum;
        private decimal _totalDebtsSum;
        private decimal _totalCashReceivedSum;

        private int _totalDailySalesCountSum;
        private decimal _totalDailyRevenueSum;
        private decimal _totalDailyCashSalesSum;
        private decimal _totalDailyCardSalesSum;
        private decimal _totalDailyDebtSalesSum;

        private int _totalCashierSalesCountSum;
        private decimal _totalCashierRevenueSum;
        private decimal _totalCashierCashSalesSum;
        private decimal _totalCashierCardSalesSum;
        private decimal _totalCashierDebtSalesSum;
        
        private DateTime _startDate = DateTime.Today.AddDays(-30);
        private DateTime _endDate = DateTime.Today;
        private string _currentReportType = "Sales"; 
        private ReportPeriodType _periodType = ReportPeriodType.Daily;

        public CashierProberty cashierProberty { get; }
        public Pagination<CashBoxReport> CashBoxPager { get; }

        public ObservableCollection<CashierSalesReport> CashierSalesReports => _cashierSalesReports;
        public ObservableCollection<DailySalesReport> DailySalesReports => _dailySalesReports;
        public ObservableCollection<DeliveryReport> DeliveryReports => _deliveryReports;
        public Pagination<ShiftReport> ShiftPager => _shiftPager;
        public ObservableCollection<ShiftReport> ShiftReports => _shiftPager.PagedItems;
        public ObservableCollection<CashBoxReport> CashBoxReports => CashBoxPager.PagedItems;

        public decimal TotalShiftSalesSum { get => _totalShiftSalesSum; private set => SetProperty(ref _totalShiftSalesSum, value); }
        public int TotalInvoiceCountSum { get => _totalInvoiceCountSum; private set => SetProperty(ref _totalInvoiceCountSum, value); }
        public int TotalReturnedOrdersCountSum { get => _totalReturnedOrdersCountSum; private set => SetProperty(ref _totalReturnedOrdersCountSum, value); }
        public decimal TotalReturnedOrdersAmountSum { get => _totalReturnedOrdersAmountSum; private set => SetProperty(ref _totalReturnedOrdersAmountSum, value); }
        public decimal TotalDebtsSum { get => _totalDebtsSum; private set => SetProperty(ref _totalDebtsSum, value); }
        public decimal TotalCashReceivedSum { get => _totalCashReceivedSum; private set => SetProperty(ref _totalCashReceivedSum, value); }

        public int TotalDailySalesCountSum { get => _totalDailySalesCountSum; private set => SetProperty(ref _totalDailySalesCountSum, value); }
        public decimal TotalDailyRevenueSum { get => _totalDailyRevenueSum; private set => SetProperty(ref _totalDailyRevenueSum, value); }
        public decimal TotalDailyCashSalesSum { get => _totalDailyCashSalesSum; private set => SetProperty(ref _totalDailyCashSalesSum, value); }
        public decimal TotalDailyCardSalesSum { get => _totalDailyCardSalesSum; private set => SetProperty(ref _totalDailyCardSalesSum, value); }
        public decimal TotalDailyDebtSalesSum { get => _totalDailyDebtSalesSum; private set => SetProperty(ref _totalDailyDebtSalesSum, value); }

        public int TotalCashierSalesCountSum { get => _totalCashierSalesCountSum; private set => SetProperty(ref _totalCashierSalesCountSum, value); }
        public decimal TotalCashierRevenueSum { get => _totalCashierRevenueSum; private set => SetProperty(ref _totalCashierRevenueSum, value); }
        public decimal TotalCashierCashSalesSum { get => _totalCashierCashSalesSum; private set => SetProperty(ref _totalCashierCashSalesSum, value); }
        public decimal TotalCashierCardSalesSum { get => _totalCashierCardSalesSum; private set => SetProperty(ref _totalCashierCardSalesSum, value); }
        public decimal TotalCashierDebtSalesSum { get => _totalCashierDebtSalesSum; private set => SetProperty(ref _totalCashierDebtSalesSum, value); }

        public DateTime StartDate { get => _startDate; set { if (SetProperty(ref _startDate, value)) RefreshCurrentReport(); } }
        public DateTime EndDate { get => _endDate; set { if (SetProperty(ref _endDate, value)) RefreshCurrentReport(); } }
        private CashierAuditSummaryDto? _auditSummary;
        private ObservableCollection<CashierDiscountAuditDto> _auditDiscounts = new();
        private ObservableCollection<CashierCancellationAuditDto> _auditCancellations = new();
        private ObservableCollection<CashierLeftoverDisposalAuditDto> _auditLeftoverDisposals = new();
        private ObservableCollection<CashierRiskRankingDto> _auditCashierRankings = new();
        private ObservableCollection<User> _cashiers = new();
        private User? _selectedCashier;

        public CashierAuditSummaryDto? AuditSummary { get => _auditSummary; set => SetProperty(ref _auditSummary, value); }
        public ObservableCollection<CashierDiscountAuditDto> AuditDiscounts => _auditDiscounts;
        public ObservableCollection<CashierCancellationAuditDto> AuditCancellations => _auditCancellations;
        public ObservableCollection<CashierLeftoverDisposalAuditDto> AuditLeftoverDisposals => _auditLeftoverDisposals;
        public ObservableCollection<CashierRiskRankingDto> AuditCashierRankings => _auditCashierRankings;
        public ObservableCollection<User> Cashiers => _cashiers;
        public User? SelectedCashier { get => _selectedCashier; set { if (SetProperty(ref _selectedCashier, value)) RefreshCurrentReport(); } }

        public string CurrentReportType
        {
            get => _currentReportType;
            set
            {
                if (SetProperty(ref _currentReportType, value))
                {
                    OnPropertyChanged(nameof(IsSalesReportActive));
                    OnPropertyChanged(nameof(IsDailyReportActive));
                    OnPropertyChanged(nameof(IsDeliveryReportActive));
                    OnPropertyChanged(nameof(IsShiftReportActive));
                    OnPropertyChanged(nameof(IsCashBoxReportActive));
                    OnPropertyChanged(nameof(IsAuditReportActive));
                }
            }
        }

        public ReportPeriodType PeriodType { get => _periodType; set { if (SetProperty(ref _periodType, value)) UpdateDateRange(); } }

        public bool IsSalesReportActive => CurrentReportType == "Sales";
        public bool IsDailyReportActive => CurrentReportType == "Daily";
        public bool IsDeliveryReportActive => CurrentReportType == "Delivery";
        public bool IsShiftReportActive => CurrentReportType == "Shift";
        public bool IsCashBoxReportActive => CurrentReportType == "CashBox";
        public bool IsAuditReportActive => CurrentReportType == "Audit";
    }
}
