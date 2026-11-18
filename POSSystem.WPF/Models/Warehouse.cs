

namespace POSSystem.WPF.Models
{
    public class Warehouse : BaseModel
    {

      
        
        public string Location { get; set; } = string.Empty;
        
        public string ManagerName { get; set; } = string.Empty;
        
        public string ContactInfo { get; set; } = string.Empty;
        
        
      
        
        // Navigation properties
        public List<Product> Products { get; set; } = new List<Product>();
        //public ICollection<Product> Product { get; set; } = new List<Product>();
        public ICollection<WarehouseFromProduct> warehouseFromProducts { get; set; } = new List<WarehouseFromProduct>();
        
        // Computed properties
        [NotMapped]
        public int TotalProducts => Products?.Count ?? 0;
        
        [NotMapped]
        public decimal TotalStockQuantity => Products?.Sum(p => p.StockQuantity) ?? 0;
        
        [NotMapped]
        public decimal TotalValue => Products?.Sum(p => p.Price * p.StockQuantity) ?? 0;
        
        [NotMapped]
        public int LowStockProducts => (warehouseFromProducts != null && warehouseFromProducts.Any())
            ? warehouseFromProducts.Count(p => p.StockQuantity > 0 && p.StockQuantity <= (p.MinimumStockLevel > 0 ? p.MinimumStockLevel : 5m))
            : (Products?.Count(p => p.StockQuantity <= 5) ?? 0);
        
        [NotMapped]
        public int OutOfStockProducts => (warehouseFromProducts != null && warehouseFromProducts.Any())
            ? warehouseFromProducts.Count(p => p.StockQuantity == 0)
            : (Products?.Count(p => p.StockQuantity == 0) ?? 0);
        [NotMapped]
        public int CountToProduct => warehouseFromProducts.Count();
    }
}
