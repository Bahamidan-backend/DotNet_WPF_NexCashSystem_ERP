namespace POSSystem.WPF.Models.Helper
{
    public class CashierSalesReport
    {
        public int UserId { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public int InvoiceCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal CashSales { get; set; }
        public decimal CardSales { get; set; }
        public decimal DebtSales { get; set; }
        public decimal AverageSaleAmount { get; set; }
    }

    // Daily summary by date
    public class DailySalesReport
    {
        public DateTime Date { get; set; }
        public int InvoiceCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal CashSales { get; set; }
        public decimal CardSales { get; set; }
        public decimal DebtSales { get; set; }
        public decimal AverageSaleAmount { get; set; }
    }

    //public class DeliveryReport
    //{
    //    public int UserId { get; set; }
    //    public string DeliveryPersonName { get; set; } = string.Empty;
    //    public int TotalOrders { get; set; }
    //    public decimal TotalOrderValue { get; set; }
    //    public int CompletedOrders { get; set; }
    //    public int PendingOrders { get; set; }
    //    public decimal AverageOrderValue { get; set; }
    //}

    //public class CashierProfitReport
    //{
    //    public int UserId { get; set; }
    //    public string CashierName { get; set; } = string.Empty;
    //    public decimal TotalRevenue { get; set; }
    //    public decimal TotalCost { get; set; }
    //    public decimal NetProfit { get; set; }
    //    public decimal ProfitPercentage { get; set; }
    //    public int TotalSales { get; set; }
    //}

    //public class CashierReturnsReport
    //{
    //    public int UserId { get; set; }
    //    public string CashierName { get; set; } = string.Empty;
    //    public int TotalReturns { get; set; }
    //    public decimal TotalReturnAmount { get; set; }
    //    public decimal AverageReturnAmount { get; set; }
    //    public List<ReturnDetail> ReturnDetails { get; set; } = new();
    //}

    //public class ReturnDetail
    //{
    //    public int SaleId { get; set; }
    //    public string InvoiceNumber { get; set; } = string.Empty;
    //    public decimal ReturnAmount { get; set; }
    //    public DateTime ReturnDate { get; set; }
    //    public string? Reason { get; set; }
    //}

    //public class CashBoxReport
    //{
    //    public int TransactionId { get; set; }
    //    public int ShiftId { get; set; }
    //    public string ShiftNumber { get; set; } = string.Empty;
    //    public string CashierName { get; set; } = string.Empty;
    //    public string TransactionType { get; set; } = string.Empty;
    //    public decimal Amount { get; set; }
    //    public string Description { get; set; } = string.Empty;
    //    public DateTime TransactionDate { get; set; }
    //    public string TransactionTime { get; set; } = string.Empty;
    //    public bool IsPersonal { get; set; }
    //    public string ExpenseTypeText { get; set; } = string.Empty;
    //    public string Notes { get; set; } = string.Empty;
    //}
}

