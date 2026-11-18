
using static POSSystem.WPF.Models.Recipe;

namespace POSSystem.WPF.Models
{
    public class WarehouseFromProduct: BaseModel
    {

        //[ForeignKey("Categories")]
        //public int CategoryId { get; set; }
        //public Category Categories { get; set; }
        public int? WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }
        public decimal Price { get; set; }
        public decimal StockQuantity { get; set; }
        public decimal MinimumStockLevel { get; set; } = 5m;
        public PortionUnit? PortionUnit { get; set; }

        private bool _isSelected;

        [NotMapped]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        [NotMapped]
        public decimal TotalValue => Price * StockQuantity;

        [NotMapped]
        public bool IsLowStock => StockQuantity <= (MinimumStockLevel > 0 ? MinimumStockLevel : 5m);

        [NotMapped]
        public string StockStatus
        {
            get
            {
                if (StockQuantity <= 0) return "نفد المخزون";
                var threshold = MinimumStockLevel > 0 ? MinimumStockLevel : 5m;
                if (StockQuantity <= threshold) return "مخزون منخفض";
                return "متوفر";
            }
        }



    }
}
