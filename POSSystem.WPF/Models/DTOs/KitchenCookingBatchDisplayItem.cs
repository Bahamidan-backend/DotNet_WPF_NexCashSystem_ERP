using System;
using POSSystem.WPF.Helpers;

namespace POSSystem.WPF.Models.DTOs
{
    public class KitchenCookingBatchDisplayItem
    {
        public int Id { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public DateTime CookedAt { get; set; }
        public string CookedAtDisplay => CookedAt.ToString("yyyy/MM/dd hh:mm tt");
        public decimal MainQty { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string MainQtyDisplay => $"{MainQty:G29} {UnitName}";
        public decimal ExpectedPortions { get; set; }
        public decimal ActualPortions { get; set; }
        public decimal Variance => ActualPortions - ExpectedPortions;
        public string VarianceDisplay => Variance > 0 ? $"+{Variance:G29} نفر (وفر)" : (Variance < 0 ? $"{Variance:G29} نفر (عجز)" : "مطابق للمعيار");
        public string VarianceBadgeBackground => Variance > 0 ? "#dcfce7" : (Variance < 0 ? "#fee2e2" : "#f1f5f9");
        public string VarianceBadgeForeground => Variance > 0 ? "#15803d" : (Variance < 0 ? "#b91c1c" : "#475569");
        public string VarianceIcon => Variance > 0 ? "🟢" : (Variance < 0 ? "⚠️" : "⚪");
        public decimal TotalRawCost { get; set; }
        public string TotalRawCostDisplay => $"{TotalRawCost:N0} ريال";
        public string CookedByUserName { get; set; } = "غير محدد";
        public string Notes { get; set; } = string.Empty;
    }
}

