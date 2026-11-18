using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static POSSystem.WPF.Models.Recipe;

namespace POSSystem.WPF.Models
{
    public class Product : BaseModel 
    {
        public const string UntrackedStockMarker = "NO_INVENTORY";

        [Required]
        public decimal Price { get; set; }

        [Required]
        public decimal BasePrice { get; set; }
        
        public decimal PersonNumberFood { get; set; }
        
        private decimal _availableQtyInPOS;
        public decimal AvailableQtyInPOS
        {
            get => _availableQtyInPOS;
            set
            {
                if (_availableQtyInPOS != value)
                {
                    _availableQtyInPOS = value;
                    OnPropertyChanged(nameof(AvailableQtyInPOS));
                    OnPropertyChanged(nameof(TotalValue));
                    OnPropertyChanged(nameof(StockDisplayText));
                    OnPropertyChanged(nameof(StockStatus));
                }
            }
        }

        public decimal LastCookedQty { get; set; }
        public int YieldRate { get; set; }
        
        [NotMapped]
        public decimal TotalValue
        {
            get => AvailableQtyInPOS;
            set => AvailableQtyInPOS = value;
        }
        
        [NotMapped]
        public decimal StockQuantity
        {
            get => PersonNumberFood > 0 ? AvailableQtyInPOS / PersonNumberFood : AvailableQtyInPOS;
            set => AvailableQtyInPOS = PersonNumberFood > 0 ? value * PersonNumberFood : value;
        }
        
        private string? _tempCategoryName;
        [NotMapped]
        public string? CategoryName 
        { 
            get => Category?.Name ?? _tempCategoryName; 
            set => _tempCategoryName = value; 
        }
       
        public string ImagePath { get; set; } = string.Empty;
        public string? TypeWarehoseFromProduct { get; set; } = string.Empty;
        
        public PortionUnit portionUnit { get; set; } 

        [ForeignKey(nameof(CategoryId))]
        public int CategoryId { get; set; } = 1; 
        public virtual Category Category { get; set; } 

        public bool IsAnimalComponent { get; set; } = false;
        public ComponentType? AnimalComponentType { get; set; }

        public int? AnimalBatchId { get; set; }
        [ForeignKey(nameof(AnimalBatchId))]
        public virtual AnimalBatch? AnimalBatch { get; set; }

        [NotMapped]
        public bool IsAddon { get; set; } = false;
        
        [NotMapped]
        public string? VirtualSizeName { get; set; }

        [NotMapped]
        public Product? ParentProduct { get; set; }

        /// <summary>
        /// يحدد ما إذا كان الصنف يباع بدون ارتباط بكمية مخزن (مثل الإيدامات والسلطات والمقبلات).
        /// </summary>
        [NotMapped]
        public bool IsUntrackedStock
        {
            get
            {
                if (ParentProduct != null && ParentProduct.IsUntrackedStock)
                    return true;
                if (string.Equals(TypeWarehoseFromProduct, UntrackedStockMarker, StringComparison.OrdinalIgnoreCase))
                    return true;
                var catName = (CategoryName ?? Category?.Name ?? string.Empty).Trim();
                if ((catName.Contains("ايدام") || catName.Contains("إيدام") || catName.Contains("سلط") || catName.Contains("مقبلات"))
                    && AvailableQtyInPOS <= 0 && (Recipe == null || Recipe.Count == 0))
                    return true;
                return false;
            }
            set
            {
                if (value)
                {
                    TypeWarehoseFromProduct = UntrackedStockMarker;
                }
                else if (string.Equals(TypeWarehoseFromProduct, UntrackedStockMarker, StringComparison.OrdinalIgnoreCase))
                {
                    TypeWarehoseFromProduct = string.Empty;
                }
                OnPropertyChanged(nameof(IsUntrackedStock));
                OnPropertyChanged(nameof(StockDisplayText));
                OnPropertyChanged(nameof(StockStatus));
            }
        }

        [NotMapped]
        public string StockDisplayText => IsUntrackedStock ? "مفتوح" : AvailableQtyInPOS.ToString("N2");
        
        // Navigation properties
        public virtual ICollection<ProductSize> ProductSizes { get; set; } = new List<ProductSize>();
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public List<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
        public virtual ICollection<Recipe> Recipe { get; set; } = new List<Recipe>();

        private bool _isInOrder = false;
        [NotMapped]
        public bool IsInOrder 
        { 
            get => _isInOrder; 
            set 
            { 
                if (_isInOrder != value)
                {
                    _isInOrder = value;
                    OnPropertyChanged(nameof(IsInOrder));
                }
            } 
        }

        private int _quantity = 0;
        [NotMapped]
        public int Quantity 
        { 
            get => _quantity; 
            set 
            { 
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged(nameof(Quantity));
                }
            } 
        }

        [NotMapped]
        public string StockStatus
        {
            get
            {
                if (IsUntrackedStock) return "In Stock";
                if (AvailableQtyInPOS == 0) return "Out of Stock";
                if (AvailableQtyInPOS <= 10) return "Low Stock";
                return "In Stock";
            }
        }

        [NotMapped]
        public List<Addon> Addons { get; set; } = new();

        public int? KitchenId { get; set; }
        public virtual Kitchen? Kitchen { get; set; }
    }

    public enum ProductCategory
    {
        Food,
        Drink,
        Dessert,
        Appetizer
    }
}
