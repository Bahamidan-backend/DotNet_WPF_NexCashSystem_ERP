using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSSystem.WPF.Models
{
    public enum StockMovementType
    {
        Purchase = 1,
        Sale = 2,
        SaleReturn = 3,
        PurchaseReturn = 4,
        TransferIn = 5,
        TransferOut = 6,
        Adjustment = 7,
        RecipeCooking = 8,
        InitialStock = 9,
        Waste = 10
    }

    [Table("StockMovements")]
    public class StockMovement
    {
        [Key]
        public int Id { get; set; }

        public int WarehouseFromProductId { get; set; }
        
        [ForeignKey("WarehouseFromProductId")]
        public virtual WarehouseFromProduct? WarehouseFromProduct { get; set; }

        public int? WarehouseId { get; set; }

        [ForeignKey("WarehouseId")]
        public virtual Warehouse? Warehouse { get; set; }

        public StockMovementType MovementType { get; set; }

        /// <summary>
        /// الكمية المحركة (موجبة للإضافة، سالبة للخصم)
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// الرصيد قبل الحركة
        /// </summary>
        public decimal BalanceBefore { get; set; }

        /// <summary>
        /// الرصيد بعد الحركة
        /// </summary>
        public decimal BalanceAfter { get; set; }

        /// <summary>
        /// تكلفة الوحدة عند الحركة
        /// </summary>
        public decimal UnitCost { get; set; }

        /// <summary>
        /// القيمة الإجمالية للحركة
        /// </summary>
        public decimal TotalCost { get; set; }

        /// <summary>
        /// رقم المستند المرجعي (فاتورة، أمر شراء، رقم تحويل)
        /// </summary>
        public string? ReferenceNumber { get; set; }

        public int? OrderId { get; set; }
        public int? PurchaseId { get; set; }
        public int? UserId { get; set; }

        public string? Notes { get; set; }

        public DateTime MovementDate { get; set; } = DateTime.Now;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string MovementTypeText => MovementType switch
        {
            StockMovementType.Purchase => "شراء من مورد",
            StockMovementType.Sale => "مبيعات (استهلاك)",
            StockMovementType.SaleReturn => "مرتجع مبيعات",
            StockMovementType.PurchaseReturn => "مرتجع لمورد",
            StockMovementType.TransferIn => "تحويل وارد",
            StockMovementType.TransferOut => "تحويل صادر",
            StockMovementType.Adjustment => "تسوية جرد",
            StockMovementType.RecipeCooking => "استهلاك طهي",
            StockMovementType.InitialStock => "رصيد افتتاحي",
            StockMovementType.Waste => "هدر / تالف مطبخ",
            _ => "حركة مخزنية"
        };
    }
}

