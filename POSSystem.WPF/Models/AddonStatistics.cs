namespace POSSystem.WPF.Models
{
    public class AddonStatistics
    {
        public int TotalAddonTypes { get; set; }
        public decimal TotalAddonStock { get; set; }
        public decimal TotalPurchaseValue { get; set; }
        public int SoldAddonsCount { get; set; }
        public decimal TotalSoldValue { get; set; }
        public decimal NetProfit => TotalSoldValue - TotalPurchaseValue;
    }
}
