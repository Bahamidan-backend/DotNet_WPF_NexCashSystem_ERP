namespace POSSystem.WPF.Models
{
    public class FinancialChannelInfo
    {
        public string ChannelKey { get; set; } = string.Empty;
        public string ChannelName { get; set; } = string.Empty;
        public PaymentMethod PaymentMethod { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal InflowTotal { get; set; }
        public decimal OutflowTotal { get; set; }
        public bool IsCashDrawer { get; set; }
        public int? ShiftId { get; set; }

        public string FormattedBalance => $"{CurrentBalance:N0} ريال";
        public string StatusColor => CurrentBalance > 0 ? "#16a34a" : (CurrentBalance < 0 ? "#dc2626" : "#64748b");
        public string StatusText => CurrentBalance > 0 ? "رصيد متاح" : (CurrentBalance < 0 ? "عجز / سالب" : "رصيد صفري");
    }
}

