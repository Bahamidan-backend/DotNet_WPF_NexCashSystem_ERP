using System;
using System.Collections.Generic;
using System.Globalization;

namespace POSSystem.WPF.Models.Helper
{
    public class CashierDiscountAuditDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string TimeString { get; set; } = string.Empty;
        public string DateString { get; set; } = string.Empty;
        public string CashierName { get; set; } = "غير معروف";
        public string CustomerName { get; set; } = "عميل مباشر";
        public string OrderTypeText { get; set; } = "محلي";
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercentage { get; set; }
        public string DiscountPercentText => $"{DiscountPercentage:F1}%";
        public decimal Total { get; set; }
        public string PaymentMethodText { get; set; } = "نقدي";
        public string Notes { get; set; } = string.Empty;
    }

    public class CashierCancellationAuditDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime CancelledAt { get; set; }
        public string TimeString { get; set; } = string.Empty;
        public string DateString { get; set; } = string.Empty;
        public string CashierName { get; set; } = "غير معروف";
        public string CustomerName { get; set; } = "عميل مباشر";
        public string OrderTypeText { get; set; } = "محلي";
        public decimal TotalAmount { get; set; }
        public string ItemsSummary { get; set; } = string.Empty;
        public string Reason { get; set; } = "بدون سبب مدخل";
        public string DurationText { get; set; } = string.Empty;
        public string CancelledBy { get; set; } = "الكاشير";
    }

    public class CashierLeftoverDisposalAuditDto
    {
        public int WasteId { get; set; }
        public DateTime WasteDate { get; set; }
        public string DateString { get; set; } = string.Empty;
        public string TimeString { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string DisposalType { get; set; } = "غداء / إطعام العمال";
        public string BadgeBg { get; set; } = "#EFF6FF";
        public string BadgeFg { get; set; } = "#1D4ED8";
        public decimal Quantity { get; set; }
        public string QuantityText => Quantity.ToString("N2", CultureInfo.InvariantCulture).Replace('.', ',');
        public string BalanceFlowText { get; set; } = "-";
        public decimal EstimatedCost { get; set; }
        public string CashierName { get; set; } = "الكاشير";
        public string ApprovedBy { get; set; } = "بكلمة سر المناوبة";
        public string Notes { get; set; } = string.Empty;
    }

    public class CashierRiskRankingDto
    {
        public int CashierId { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public int DiscountInvoicesCount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public int CancelledOrdersCount { get; set; }
        public decimal TotalCancelledAmount { get; set; }
        public int ReturnsCount { get; set; }
        public decimal TotalReturnsAmount { get; set; }
        public string RiskLevel { get; set; } = "آمن ✅";
        public string RiskBadgeColor { get; set; } = "#27ae60";
    }

    public class CashierAuditSummaryDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public decimal TotalDiscountAmount { get; set; }
        public int DiscountInvoicesCount { get; set; }
        public decimal AverageDiscountAmount => DiscountInvoicesCount > 0 ? TotalDiscountAmount / DiscountInvoicesCount : 0m;
        public decimal MaxDiscountAmount { get; set; }

        public decimal TotalCancelledAmount { get; set; }
        public int CancelledOrdersCount { get; set; }

        public decimal TotalReturnsAmount { get; set; }
        public int ReturnsCount { get; set; }

        // إحصائيات وإجماليات صرف الأكل الزايد والهدر حسب كل نوع سبب صرف
        public int TotalLeftoverDisposalsCount { get; set; }
        public decimal TotalLeftoverDisposalsQty { get; set; }
        public decimal TotalLeftoverDisposalsCost { get; set; }

        public int StaffMealCount { get; set; }
        public decimal StaffMealQty { get; set; }
        public string StaffMealQtyText => StaffMealQty.ToString("N2", CultureInfo.InvariantCulture).Replace('.', ',');
        public decimal StaffMealCost { get; set; }

        public int CharityCount { get; set; }
        public decimal CharityQty { get; set; }
        public string CharityQtyText => CharityQty.ToString("N2", CultureInfo.InvariantCulture).Replace('.', ',');
        public decimal CharityCost { get; set; }

        public int EndOfDayWasteCount { get; set; }
        public decimal EndOfDayWasteQty { get; set; }
        public string EndOfDayWasteQtyText => EndOfDayWasteQty.ToString("N2", CultureInfo.InvariantCulture).Replace('.', ',');
        public decimal EndOfDayWasteCost { get; set; }

        public int HospitalityCount { get; set; }
        public decimal HospitalityQty { get; set; }
        public string HospitalityQtyText => HospitalityQty.ToString("N2", CultureInfo.InvariantCulture).Replace('.', ',');
        public decimal HospitalityCost { get; set; }

        public int TotalSensitiveOperationsCount => DiscountInvoicesCount + CancelledOrdersCount + ReturnsCount + TotalLeftoverDisposalsCount;

        public string SecurityStatus { get; set; } = "الوضع طبيعي ونظيف 100%";
        public string SecurityMessage { get; set; } = "لا توجد عمليات مشبوهة مسجلة في هذه الفترة.";
        public string SecurityBadgeColor { get; set; } = "#27ae60";

        public List<CashierDiscountAuditDto> Discounts { get; set; } = new();
        public List<CashierCancellationAuditDto> Cancellations { get; set; } = new();
        public List<CashierLeftoverDisposalAuditDto> LeftoverDisposals { get; set; } = new();
        public List<CashierRiskRankingDto> CashierRankings { get; set; } = new();
    }
}
