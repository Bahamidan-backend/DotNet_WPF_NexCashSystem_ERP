using System;

namespace POSSystem.WPF.Models.Helper
{
    public class DailySalesItem
    {
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal CashSales { get; set; }
        public decimal ElectronicSales { get; set; }
        public decimal DebtSales { get; set; }
        public decimal PaidDebtSales { get; set; }
        public decimal UnpaidDebtSales { get; set; }
    }
}
