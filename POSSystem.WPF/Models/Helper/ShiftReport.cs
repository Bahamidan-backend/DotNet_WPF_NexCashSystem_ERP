using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.Models.Helper
{
    public class ShiftReport
    {
        public string ShiftNumber { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal TotalSales { get; set; }
        public int InvoiceCount { get; set; }
        public int DiscountCodesUsed { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public int AddonsSold { get; set; }
        public decimal AddonsRevenue { get; set; }
        public decimal ProductsSold { get; set; }
        public int ReturnedOrdersCount { get; set; }
        public decimal ReturnedOrdersAmount { get; set; }
        public decimal CashReceived { get; set; }
        public decimal TotalDebtsCreated { get; set; }
        public List<ReturnDetail> ReturnDetails { get; set; } = new List<ReturnDetail>();

        // Cash drawer reconciliation properties
        public decimal StartingCash { get; set; }
        public decimal EndingCash { get; set; }
        public decimal ExpectedEndingCash { get; set; }
        public decimal CashDifference { get; set; }
        public string DifferenceStatusText { get; set; } = string.Empty;
        public string DifferenceColor => DifferenceStatusText switch
        {
            "مطابق" or "متطابق تماماً" => "#27ae60",
            var s when s.Contains("عجز") => "#e74c3c",
            var s when s.Contains("زيادة") => "#3498db",
            _ => "#2c3e50"
        };
    }
}
