namespace POSSystem.WPF.Models
{
    public class WalletDepositSummary
    {
        public string BankOrWallet { get; set; } = string.Empty;
        public string Currency { get; set; } = "ريال يمني";
        public decimal TotalAmount { get; set; }
        public int OrderCount { get; set; }

        public string FormattedAmount => $"{TotalAmount:N0} {Currency}";
        public string DisplaySubtitle => $"{OrderCount} {(OrderCount == 1 ? "عملية" : OrderCount == 2 ? "عمليتان" : OrderCount <= 10 ? "عمليات" : "عملية")}";
    }
}

