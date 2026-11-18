using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSSystem.WPF.Models
{
    public enum WasteReason
    {
        CookingBurn = 1,        // حرق أثناء الطهي
        Expired = 2,            // انتهاء صلاحية / تلف تخزين
        PreparationLoss = 3,    // هدر تجهيز وسلخ
        SpillageOrBreakage = 4, // انسكاب أو كسر في المطبخ
        CustomerReturn = 5,     // مرتجع تالف من عميل
        GeneralWaste = 6,       // هدر عام / أخرى
        StaffMeal = 7,          // غداء / إطعام العمال والموظفين
        Charity = 8             // صدقة / تبرع خيري
    }

    public static class WasteReasonHelper
    {
        public static string GetArabicText(WasteReason reason) => reason switch
        {
            WasteReason.CookingBurn => "حرق أثناء الطهي / إعداد خاطئ",
            WasteReason.Expired => "تلف تخزين / انتهاء الصلاحية",
            WasteReason.PreparationLoss => "فاقد تنظيف وتجهيز / سلخ",
            WasteReason.SpillageOrBreakage => "انسكاب أو كسر في المطبخ",
            WasteReason.CustomerReturn => "تالف راجع من العميل",
            WasteReason.StaffMeal => "إطعام العمال / وجبات الموظفين",
            WasteReason.Charity => "صدقة / تبرع خيري",
            _ => "هدر عام / تالف نهاية اليوم"
        };
    }

    [Table("StockWastes")]
    public class StockWaste : BaseModel
    {
        public int WarehouseFromProductId { get; set; }
        [ForeignKey("WarehouseFromProductId")]
        public virtual WarehouseFromProduct? WarehouseFromProduct { get; set; }

        public int? WarehouseId { get; set; }
        [ForeignKey("WarehouseId")]
        public virtual Warehouse? Warehouse { get; set; }

        public decimal Quantity { get; set; }
        public Recipe.PortionUnit? PortionUnit { get; set; }

        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }

        public WasteReason Reason { get; set; } = WasteReason.CookingBurn;
        public DateTime WasteDate { get; set; } = DateTime.Now;

        public int? ReportedByUserId { get; set; }
        [ForeignKey("ReportedByUserId")]
        public virtual User? ReportedByUser { get; set; }

        public string? Notes { get; set; }

        [NotMapped]
        public string ReasonText => WasteReasonHelper.GetArabicText(Reason);
    }
}
