using POSSystem.WPF.Models;
using POSSystem.WPF.Models.Helper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.Shered.Proberty
{
    public class CategorySalesSummaryItem : BaseModel
    {
        public string CategoryName { get; set; } = string.Empty;
        public int ProductsCount { get; set; }
        public decimal TotalSoldQuantity { get; set; }
        public string TotalSoldQuantityText { get; set; } = string.Empty;
        public string ProductsDetailsSummaryText { get; set; } = string.Empty;
        public int OrdersCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class ProductSalesReportItem : BaseModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalSoldQuantity { get; set; }
        public string UnitLabel { get; set; } = "حبة / نفر";
        public string TotalSoldQuantityText => $"{TotalSoldQuantity:0.##} {UnitLabel}";
        public int TotalUnitsCount { get; set; }
        public string SizesBreakdownText { get; set; } = string.Empty;
        public int OrdersCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class ExpenseAccountSummaryItem : BaseModel
    {
        public string AccountName { get; set; } = string.Empty;
        public int TransactionsCount { get; set; }
        public decimal CashAmount { get; set; }
        public decimal WalletAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Percentage { get; set; }
        public string PercentageText => $"{Percentage:0.#}%";
        public DateTime? LastTransactionDate { get; set; }
        public string LastTransactionDateText => LastTransactionDate.HasValue ? LastTransactionDate.Value.ToString("yyyy-MM-dd") : "بدون حركة";
        public bool HasTransactions => TransactionsCount > 0;
    }

    public class GeneralReportProberty : BaseModel
    {
        private bool _isLoading = false;
        private decimal _totalSalaries;
        private decimal _totalAdvances;
        private decimal _totalCOGS;
        private decimal _totalPurchases;
        private decimal _totalPurchaseReturns;
        private decimal _totalOrderReturns;
        private decimal _netSales;
        private decimal _totalPaidDebts;
        private decimal _totalUnpaidDebts;
        private decimal _totalStockWasteAndShortage;
        private Dictionary<string, decimal> _expensesByCategory = new();

        private int _shiftSummaryShiftCount;
        private decimal _shiftSummaryTotalSales;
        private decimal _shiftSummaryCashReceived;
        private decimal _shiftSummaryTotalDebts;
        private int _shiftSummaryInvoiceCount;
        private decimal _shiftSummaryTotalDiscount;

        public ObservableCollection<ShiftReport> ShiftReports { get; } = new ObservableCollection<ShiftReport>();
        public ObservableCollection<CashBoxTransactionReport> CashBoxReports { get; } = new ObservableCollection<CashBoxTransactionReport>();

        public void RefreshShiftSummary()
        {
            ShiftSummaryShiftCount = ShiftReports?.Count ?? 0;
            ShiftSummaryTotalSales = ShiftReports?.Sum(s => s.TotalSales) ?? 0m;
            ShiftSummaryCashReceived = ShiftReports?.Sum(s => s.CashReceived) ?? 0m;
            ShiftSummaryTotalDebts = ShiftReports?.Sum(s => s.TotalDebtsCreated) ?? 0m;
            ShiftSummaryInvoiceCount = ShiftReports?.Sum(s => s.InvoiceCount) ?? 0;
            ShiftSummaryTotalDiscount = ShiftReports?.Sum(s => s.TotalDiscountAmount) ?? 0m;
        }

        public int ShiftSummaryShiftCount { get => _shiftSummaryShiftCount; set => SetProperty(ref _shiftSummaryShiftCount, value); }
        public decimal ShiftSummaryTotalSales { get => _shiftSummaryTotalSales; set => SetProperty(ref _shiftSummaryTotalSales, value); }
        public decimal ShiftSummaryCashReceived { get => _shiftSummaryCashReceived; set => SetProperty(ref _shiftSummaryCashReceived, value); }
        public decimal ShiftSummaryTotalDebts { get => _shiftSummaryTotalDebts; set => SetProperty(ref _shiftSummaryTotalDebts, value); }
        public int ShiftSummaryInvoiceCount { get => _shiftSummaryInvoiceCount; set => SetProperty(ref _shiftSummaryInvoiceCount, value); }
        public decimal ShiftSummaryTotalDiscount { get => _shiftSummaryTotalDiscount; set => SetProperty(ref _shiftSummaryTotalDiscount, value); }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // Profit & Loss Report
        private decimal _totalRevenue;
        public decimal TotalRevenue
        {
            get => _totalRevenue;
            set => SetProperty(ref _totalRevenue, value);
        }

        private decimal _totalCosts;
        public decimal TotalCosts
        {
            get => _totalCosts;
            set => SetProperty(ref _totalCosts, value);
        }

        private decimal _netProfit;
        public decimal NetProfit
        {
            get => _netProfit;
            set => SetProperty(ref _netProfit, value);
        }

        private decimal _profitMargin;
        public decimal ProfitMargin
        {
            get => _profitMargin;
            set => SetProperty(ref _profitMargin, value);
        }

        /// <summary>إجمالي الهدر والعجز (أكل العمال + الصدقة + الهدر والتالف + عجز الجرد) المحمل على المصاريف التشغيلية</summary>
        public decimal TotalStockWasteAndShortage
        {
            get => _totalStockWasteAndShortage;
            set => SetProperty(ref _totalStockWasteAndShortage, value);
        }

        public Dictionary<string, decimal> ExpensesByCategory
        {
            get => _expensesByCategory;
            set => SetProperty(ref _expensesByCategory, value);
        }

        // Shift Report
        private int _totalShifts;
        public int TotalShifts
        {
            get => _totalShifts;
            set => SetProperty(ref _totalShifts, value);
        }

        private int _openShifts;
        public int OpenShifts
        {
            get => _openShifts;
            set => SetProperty(ref _openShifts, value);
        }

        private int _closedShifts;
        public int ClosedShifts
        {
            get => _closedShifts;
            set => SetProperty(ref _closedShifts, value);
        }

        private decimal _totalCashDifference;
        public decimal TotalCashDifference
        {
            get => _totalCashDifference;
            set => SetProperty(ref _totalCashDifference, value);
        }

        public decimal TotalSalaries
        {
            get => _totalSalaries;
            set => SetProperty(ref _totalSalaries, value);
        }

        public decimal TotalAdvances
        {
            get => _totalAdvances;
            set => SetProperty(ref _totalAdvances, value);
        }

        // Sales Report
        private decimal _totalSales;
        public decimal TotalSales
        {
            get => _totalSales;
            set => SetProperty(ref _totalSales, value);
        }

        private decimal _totalCashSales;
        public decimal TotalCashSales
        {
            get => _totalCashSales;
            set => SetProperty(ref _totalCashSales, value);
        }

        private decimal _totalElectronicSales;
        public decimal TotalElectronicSales
        {
            get => _totalElectronicSales;
            set => SetProperty(ref _totalElectronicSales, value);
        }

        private decimal _totalDebtSales;
        public decimal TotalDebtSales
        {
            get => _totalDebtSales;
            set => SetProperty(ref _totalDebtSales, value);
        }

        private int _salesCount;
        public int SalesCount
        {
            get => _salesCount;
            set => SetProperty(ref _salesCount, value);
        }

        private string? _topProduct;
        public string? TopProduct
        {
            get => _topProduct;
            set => SetProperty(ref _topProduct, value);
        }
        
        public ObservableCollection<DailySalesItem> DailySalesBreakdown { get; } = new ObservableCollection<DailySalesItem>();
        public ObservableCollection<ProductSalesReportItem> ProductSalesBreakdown { get; } = new ObservableCollection<ProductSalesReportItem>();
        public ObservableCollection<ExpenseAccountSummaryItem> ExpenseAccountsBreakdown { get; } = new ObservableCollection<ExpenseAccountSummaryItem>();

        public int TotalSoldProductsTypesCount => ProductSalesBreakdown?.Count ?? 0;
        public decimal TotalSoldPortionsSum => ProductSalesBreakdown?.Sum(x => x.TotalSoldQuantity) ?? 0m;
        public decimal TotalProductsSalesRevenue => ProductSalesBreakdown?.Sum(x => x.TotalRevenue) ?? 0m;

        public ObservableCollection<CustomerReportItem> CustomersDetails { get; } = new ObservableCollection<CustomerReportItem>();
        public ObservableCollection<EmployeeReportItem> EmployeesDetails { get; } = new ObservableCollection<EmployeeReportItem>();
        public ObservableCollection<ExpenseReportItem> ExpensesDetails { get; } = new ObservableCollection<ExpenseReportItem>();

        private decimal _cashExpensesTotal;
        public decimal CashExpensesTotal { get => _cashExpensesTotal; set => SetProperty(ref _cashExpensesTotal, value); }

        private decimal _walletExpensesTotal;
        public decimal WalletExpensesTotal { get => _walletExpensesTotal; set => SetProperty(ref _walletExpensesTotal, value); }

        // Customer Report
        private decimal _totalDebts;
        public decimal TotalDebts
        {
            get => _totalDebts;
            set => SetProperty(ref _totalDebts, value);
        }

        private int _debtCount;
        public int DebtCount
        {
            get => _debtCount;
            set => SetProperty(ref _debtCount, value);
        }

        private int _regularCustomers;
        public int RegularCustomers
        {
            get => _regularCustomers;
            set => SetProperty(ref _regularCustomers, value);
        }

        // Employee Report
        private int _totalEmployees;
        public int TotalEmployees
        {
            get => _totalEmployees;
            set => SetProperty(ref _totalEmployees, value);
        }

        private int _activeEmployees;
        public int ActiveEmployees
        {
            get => _activeEmployees;
            set => SetProperty(ref _activeEmployees, value);
        }

        private decimal _totalPaidSalaries;
        public decimal TotalPaidSalaries
        {
            get => _totalPaidSalaries;
            set => SetProperty(ref _totalPaidSalaries, value);
        }

        // Expense Report
        private decimal _totalExpenses;
        public decimal TotalExpenses
        {
            get => _totalExpenses;
            set => SetProperty(ref _totalExpenses, value);
        }

        private decimal _monthlyExpenses;
        public decimal MonthlyExpenses
        {
            get => _monthlyExpenses;
            set => SetProperty(ref _monthlyExpenses, value);
        }

        private decimal _dailyExpenses;
        public decimal DailyExpenses
        {
            get => _dailyExpenses;
            set => SetProperty(ref _dailyExpenses, value);
        }

        /// <summary>تكلفة البضاعة المباعة (المشتريات − مرتجعات المشتريات) أو من الفواتير.</summary>
        public decimal TotalCOGS
        {
            get => _totalCOGS;
            set => SetProperty(ref _totalCOGS, value);
        }

        public decimal TotalPurchases
        {
            get => _totalPurchases;
            set => SetProperty(ref _totalPurchases, value);
        }

        public decimal TotalPurchaseReturns
        {
            get => _totalPurchaseReturns;
            set => SetProperty(ref _totalPurchaseReturns, value);
        }

        public decimal TotalOrderReturns
        {
            get => _totalOrderReturns;
            set => SetProperty(ref _totalOrderReturns, value);
        }

        /// <summary>صافي المبيعات = إجمالي المبيعات − المرتجعات.</summary>
        public decimal NetSales
        {
            get => _netSales;
            set => SetProperty(ref _netSales, value);
        }

        public decimal TotalPaidDebts
        {
            get => _totalPaidDebts;
            set => SetProperty(ref _totalPaidDebts, value);
        }

        public decimal TotalUnpaidDebts
        {
            get => _totalUnpaidDebts;
            set => SetProperty(ref _totalUnpaidDebts, value);
        }
    }

    public class CustomerReportItem
    {
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalDebt { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
    }

    public class EmployeeReportItem
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public decimal BaseSalary { get; set; }
        public decimal TotalAdvances { get; set; }
    }

    public class ExpenseReportItem
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string PaymentMethodText { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
