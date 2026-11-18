using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static POSSystem.WPF.Models.Recipe;

namespace POSSystem.WPF.Models
{
    [Table("KitchenCookingBatches")]
    public class KitchenCookingBatch
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(60)]
        public string BatchNumber { get; set; } = string.Empty;

        public int ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        public int WarehouseId { get; set; }
        [ForeignKey(nameof(WarehouseId))]
        public virtual Warehouse? Warehouse { get; set; }

        public int? MainIngredientId { get; set; }
        [ForeignKey(nameof(MainIngredientId))]
        public virtual WarehouseFromProduct? MainIngredient { get; set; }

        /// <summary>
        /// مضاعف الطبخة (مثلاً 1 قدر، 0.5 نصف قدر، 1.5 قدر)
        /// </summary>
        public decimal BatchMultiplier { get; set; } = 1.0m;

        /// <summary>
        /// الكمية المستهلكة من المكون الأساسي (مثلاً 1 كيس أو 1.5 كيس أو 20 كجم)
        /// </summary>
        public decimal MainIngredientQuantity { get; set; }

        public PortionUnit PortionUnit { get; set; }

        /// <summary>
        /// الأنفار المتوقعة بناءً على المعيار
        /// </summary>
        public decimal ExpectedPortions { get; set; }

        /// <summary>
        /// الأنفار الفعلية التي أُضيفت للكاشير
        /// </summary>
        public decimal ActualPortions { get; set; }

        /// <summary>
        /// إجمالي تكلفة المواد الخام المستهلكة في الطبخة
        /// </summary>
        public decimal TotalRawCost { get; set; }

        public int? CookedByUserId { get; set; }
        [ForeignKey(nameof(CookedByUserId))]
        public virtual User? CookedByUser { get; set; }

        public int? ShiftId { get; set; }
        [ForeignKey(nameof(ShiftId))]
        public virtual Shift? Shift { get; set; }

        public string? Notes { get; set; }

        public DateTime CookedAt { get; set; } = DateTime.Now;

        public string Status { get; set; } = "Completed";

        public virtual ICollection<KitchenCookingBatchItem> Items { get; set; } = new List<KitchenCookingBatchItem>();
    }

    [Table("KitchenCookingBatchItems")]
    public class KitchenCookingBatchItem
    {
        [Key]
        public int Id { get; set; }

        public int KitchenCookingBatchId { get; set; }
        [ForeignKey(nameof(KitchenCookingBatchId))]
        public virtual KitchenCookingBatch? KitchenCookingBatch { get; set; }

        public int WarehouseFromProductId { get; set; }
        [ForeignKey(nameof(WarehouseFromProductId))]
        public virtual WarehouseFromProduct? WarehouseFromProduct { get; set; }

        [MaxLength(150)]
        public string ItemName { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public PortionUnit PortionUnit { get; set; }

        public decimal UnitCost { get; set; }

        public decimal TotalCost { get; set; }
    }
}

