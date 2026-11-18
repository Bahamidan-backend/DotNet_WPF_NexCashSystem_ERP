using POSSystem.WPF.Models;
using POSSystem.WPF.Models.Helper;
using System;
using System.Collections.Generic;

namespace POSSystem.WPF.Models
{
    public class ShiftReportDto
    {
        public Shift? Shift { get; set; }
        public List<CashBoxTransaction> Transactions { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
        public List<CashBoxTransactionDetail> TransactionDetails { get; set; } = new();

        public int TotalInvoices { get; set; }
        public decimal TotalSales { get; set; }
        public decimal CashSales { get; set; }
        public decimal CardSales { get; set; }
        public decimal CreditSales { get; set; }
        public decimal AverageInvoice { get; set; }

        public int DiscountsUsed { get; set; }
        public decimal TotalDiscountAmount { get; set; }

        public int AddonsSold { get; set; }
        public decimal AddonsRevenue { get; set; }

        public int CustomerDebtsCount { get; set; }
        public decimal TotalCustomerDebts { get; set; }

        public int CashBoxTransactionsCount { get; set; }
        public decimal TotalCashOut { get; set; }
        public bool HasCashOut { get; set; }
        public decimal CurrentCashInBox { get; set; }

        // Cash drawer reconciliation properties
        public decimal StartingCash { get; set; }
        public decimal EndingCash { get; set; }
        public decimal ExpectedEndingCash { get; set; }
        public decimal CashDifference { get; set; }
        public string DifferenceStatusText { get; set; } = string.Empty;

        public int ReturnedOrdersCount { get; set; }
        public decimal ReturnedOrdersAmount { get; set; }
        public decimal TotalTax { get; set; }
    }
}
