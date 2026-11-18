

namespace POSSystem.WPF.Models
{
    public class Addon: BaseModel
    {



        

        private decimal _price;
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        private decimal _buyingPrice;
        [Column(TypeName = "decimal(10,2)")]
        public decimal BuyingPrice
        {
            get => _buyingPrice;
            set => SetProperty(ref _buyingPrice, value);
        }

        private bool _isInHouse;
        public bool IsInHouse
        {
            get => _isInHouse;
            set => SetProperty(ref _isInHouse, value);
        }

        private decimal _quantity;
        [Required]
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    OnPropertyChanged(nameof(TotalValue));
                    OnPropertyChanged(nameof(IsLowStock));
                }
            }
        }

        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        private int? _baseAddonId;
        public int? BaseAddonId
        {
            get => _baseAddonId;
            set => SetProperty(ref _baseAddonId, value);
        }

        private decimal _baseAddonQuantity;
        public decimal BaseAddonQuantity
        {
            get => _baseAddonQuantity;
            set => SetProperty(ref _baseAddonQuantity, value);
        }

        [ForeignKey("BaseAddonId")]
        public virtual Addon? BaseAddon { get; set; }

        private bool _deductOnSale = true;
        public bool DeductOnSale
        {
            get => _deductOnSale;
            set => SetProperty(ref _deductOnSale, value);
        }

        // Navigation property for order items
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        // Computed property for total value
        [NotMapped]
        public decimal TotalValue => Price * Quantity;


        [NotMapped]
        public decimal TotalAmount { get; set; }
        // Computed property for low stock warning
        [NotMapped]
        public bool IsLowStock => Quantity <= 5; // Warning when quantity is 5 or less

        public ICollection<AddonAndOrderItem> addonAndOrderItems { get; set; } = new List<AddonAndOrderItem>();

        // NOTE: Do not redeclare PropertyChanged or OnPropertyChanged here —
        // BaseViewModel already exposes INotifyPropertyChanged's event and the
        // helper OnPropertyChanged method. Redeclaring them in this class
        // hides the base implementation and prevents subscribers (like the
        // ViewModel) from receiving notifications. Use the inherited
        // OnPropertyChanged from BaseViewModel instead.
    }
}
