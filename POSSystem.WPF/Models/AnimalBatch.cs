using System.ComponentModel.DataAnnotations.Schema;

namespace POSSystem.WPF.Models
{
    
    public class AnimalBatch : BaseModel
    {
        public AnimalType Type { get; set; }
        
        public decimal TotalWeight { get; set; } // الوزن الكلي للذبيحة
        
        public decimal RemainingWeight { get; set; } // الوزن المتبقي من اللحم
        
        public int HeadCount { get; set; } = 1; // عدد الرؤوس (1 للعادي)
        
        public decimal BackWeight { get; set; } = 0; // وزن الظهر بالكيلو
        
        public decimal LiverWeight { get; set; } = 0; // وزن الكبدة بالكيلو
        
        public bool IsWeightMode { get; set; } = true; // هل يتم تتبع اللحم بالكيلو أم بالأجزاء (ثمن)

        public bool IsFullAnimal => HeadCount > 0 && BackWeight > 0 && LiverWeight > 0 && RemainingWeight > 0;
        
        public DateTime BatchDate { get; set; } = DateTime.Now;
        
        public int? WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }
        
        public int? SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }

        public decimal PurchaseCost { get; set; } = 0; // تكلفة شراء الذبيحة
        
        // Navigation
        public virtual ICollection<BatchComponent> Components { get; set; } = new List<BatchComponent>();
    }
    
    /// <summary>
    /// Represents individual components from an animal batch
    /// </summary>
    public class BatchComponent : BaseModel
    {
        public int AnimalBatchId { get; set; }
        public virtual AnimalBatch AnimalBatch { get; set; }
        
        public ComponentType ComponentType { get; set; } // رأس، ظهر، رجل، لحم
        
        public decimal Quantity { get; set; } // الكمية المتبقية
        
        public decimal OriginalQuantity { get; set; } // الكمية الأصلية
        
        public string Unit { get; set; } = string.Empty; // الوحدة (حبة، كيلو، إلخ)
    }
    
 
    public enum AnimalType
    {
        Sheep = 1, // خروف
        Cow = 2,   // بقرة
        Camel = 3  // جمل
    }
    
 
    [Flags]
    public enum ComponentType
    {
        None = 0,
        Head = 1,    // رأس
        Back = 2,    // ظهر
        Meat = 8,    // لحم
        Liver = 16,   // كبدة
        FullAnimal = 32 // ذبيحة كاملة
    }
}
