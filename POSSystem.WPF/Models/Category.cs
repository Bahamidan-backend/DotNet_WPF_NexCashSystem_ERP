
using static POSSystem.WPF.Models.Category;

namespace POSSystem.WPF.Models
{
    public class Category: BaseModel
    {
       

        private PortionSize _portionSize;
        public PortionSize portionSize
        {
            get => _portionSize;
            set
            {
                if (_portionSize != value)
                {
                    _portionSize = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSubCategory { get; set; } = false;

        // New: Color associated with the category for visual distinction in UI
        public string? Color { get; set; } = "#3498DB"; // Default blue

        // Navigation property for addons
        public virtual ICollection<Addon> Addons { get; set; } = new List<Addon>();
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        
        public int? KitchenId { get; set; }
        public virtual Kitchen? Kitchen { get; set; }
        //public virtual ICollection<WarehouseFromProduct> WarehouseFromProducts { get; set; } = new List<WarehouseFromProduct>();




        public enum PortionSize
        {
            Single = 1,
            Piece = 2,
            Large = 3
        }

        private int _productCount;
        [NotMapped]
        public int ProductCount
        {
            get => _productCount;
            set
            {
                if (_productCount != value)
                {
                    _productCount = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProductCountText));
                }
            }
        }

        [NotMapped]
        public string ProductCountText => $"{ProductCount} منتجات";







    }
}
