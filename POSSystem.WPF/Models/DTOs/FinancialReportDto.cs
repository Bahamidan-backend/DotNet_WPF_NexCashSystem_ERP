using System;
using System.Collections.Generic;
using POSSystem.WPF.Models.Helper;
using POSSystem.WPF.Shered.Proberty;

namespace POSSystem.WPF.Models.DTOs
{
    /// <summary>
    /// نموذج البيانات المالية الصافي لتقارير الأرباح والخسائر والتحليل المالي للمطعم
    /// (Restaurant Financial Statement & P&L Domain DTO)
    /// </summary>
    public class FinancialReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        #region 1. المبيعات والإيرادات (Revenues & Sales)

        /// <summary>
        /// إجمالي المبيعات قبل الخصومات والمرتجعات
        /// </summary>
        public decimal TotalGrossSales { get; set; }

        /// <summary>
        /// إجمالي الخصومات الممنوحة على الفواتير
        /// </summary>
        public decimal TotalDiscounts { get; set; }

        /// <summary>
        /// إجمالي المرتجعات
        /// </summary>
        public decimal TotalReturns { get; set; }

        /// <summary>
        /// صافي المبيعات (إجمالي المبيعات - الخصومات - المرتجعات)
        /// </summary>
        public decimal NetSales { get; set; }

        /// <summary>
        /// إجمالي الإيرادات المعتمدة في القوائم المالية (= NetSales)
        /// </summary>
        public decimal TotalRevenue => NetSales;

        /// <summary>
        /// عدد فواتير المبيعات
        /// </summary>
        public int SalesCount { get; set; }

        #endregion

        #region 2. تفصيل قنوات الدفع (Payment Methods Breakdown)

        public decimal CashSales { get; set; }
        public decimal ElectronicSales { get; set; }
        public decimal DebtSales { get; set; }
        public decimal PaidDebtSales { get; set; }
        public decimal UnpaidDebtSales { get; set; }

        #endregion

        #region 3. تكلفة البضاعة المباعة ومجمل الربح (COGS & Gross Profit)

        /// <summary>
        /// تكلفة البضاعة المباعة الفعلية من مستودع المواد الخام ومكونات الوجبات
        /// </summary>
        public decimal TotalCOGS { get; set; }

        /// <summary>
        /// مجمل الربح التجاري (صافي المبيعات - تكلفة البضاعة المباعة)
        /// </summary>
        public decimal GrossProfit => NetSales - TotalCOGS;

        /// <summary>
        /// نسبة هامش مجمل الربح (%)
        /// </summary>
        public decimal GrossMarginPercentage => NetSales > 0 ? (GrossProfit / NetSales) * 100 : 0;

        #endregion

        #region 4. المصروفات التشغيلية والرواتب (Operating Expenses & Payroll)

        /// <summary>
        /// المصروفات التشغيلية (إيجار، فواتير، صيانة، نثريات)
        /// </summary>
        public decimal TotalExpenses { get; set; }

        /// <summary>
        /// إجمالي رواتب الموظفين الشهرية الأساسية لجميع الموظفين النشطين
        /// </summary>
        public decimal MonthlyBaseSalaries { get; set; }

        /// <summary>
        /// إجمالي رواتب الموظفين المستحقة للفترة المحددة
        /// </summary>
        public decimal TotalSalaries { get; set; }

        /// <summary>
        /// استقطاعات وسدادات السلف خلال الفترة (معلومة تحليلية لا تُطرح من مصروف الراتب الأساسي)
        /// </summary>
        public decimal AdvanceDeductions { get; set; }

        /// <summary>
        /// خسائر عجز وهدر وتلف المخزون (تسويات الجرد السالبة، الهدر، والتالف)
        /// </summary>
        public decimal TotalStockWasteAndShortage { get; set; }

        /// <summary>
        /// إجمالي المصروفات التشغيلية (المصروفات العمومية + الرواتب + عجز وهدر المخزون)
        /// </summary>
        public decimal OperatingExpenses => TotalExpenses + TotalSalaries + TotalStockWasteAndShortage;

        /// <summary>
        /// إجمالي التكاليف التشغيلية الكلية شاملة تكلفة البضاعة المباعة (COGS + المصروفات + الرواتب + عجز وهدر المخزون)
        /// </summary>
        public decimal TotalOperatingCosts => TotalCOGS + TotalExpenses + TotalSalaries + TotalStockWasteAndShortage;

        #endregion

        #region 5. صافي الربح الحقيقي (Net Profit)

        /// <summary>
        /// صافي الربح النهائي بعد استبعاد كل التكاليف التشغيلية والمخزنية
        /// </summary>
        public decimal NetProfit => TotalRevenue - TotalOperatingCosts;

        /// <summary>
        /// هامش صافي الربح (%)
        /// </summary>
        public decimal NetProfitMarginPercentage => TotalRevenue > 0 ? (NetProfit / TotalRevenue) * 100 : 0;

        #endregion

        #region 6. المشتريات والمخزون الرأسمالي (Purchases)

        public decimal TotalPurchases { get; set; }
        public decimal TotalPurchaseReturns { get; set; }

        #endregion

        #region 7. ديون العملاء (Customer Receivables)

        public decimal TotalCustomerDebts { get; set; }
        public int CustomerDebtCount { get; set; }
        public int RegularCustomerCount { get; set; }
        public decimal TotalPaidDebts { get; set; }
        public decimal TotalUnpaidDebts { get; set; }

        #endregion

        #region 8. الموظفين والسلف (Payroll & Advances)

        public int TotalEmployees { get; set; }
        public decimal TotalAdvances { get; set; }

        #endregion

        #region 9. الجداول التحليلية (Breakdown Collections)

        public string TopSellingProduct { get; set; } = "لا يوجد";
        public List<DailySalesItem> DailySalesBreakdown { get; set; } = new();
        public List<CustomerReportItem> CustomersDetails { get; set; } = new();
        public List<EmployeeReportItem> EmployeesDetails { get; set; } = new();
        public List<ExpenseReportItem> ExpensesDetails { get; set; } = new();
        public decimal CashExpensesTotal { get; set; }
        public decimal WalletExpensesTotal { get; set; }

        #endregion
    }
}
