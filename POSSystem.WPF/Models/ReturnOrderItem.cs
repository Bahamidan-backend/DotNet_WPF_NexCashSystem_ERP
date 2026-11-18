using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSSystem.WPF.Models
{
    public enum ReturnItemCondition
    {
        Waste,      // هدر / تالف (لا يعود للمخزون)
        Restock     // إعادة للمخزون (صالح للبيع)
    }

    public class ReturnOrderItem : INotifyPropertyChanged
    {
        private int _quantity;
        private decimal _totalAmount;

        public int Id { get; set; }
        
        public int ReturnOrderId { get; set; }
        public ReturnOrder ReturnOrder { get; set; } = null!;
        
        public int OriginalOrderItemId { get; set; }
        public OrderItem OriginalOrderItem { get; set; } = null!;
        
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged(nameof(Quantity));
                    UpdateTotalPrice();
                    OnPropertyChanged(nameof(TotalAmount));
                }
            }
        }
        
        public decimal UnitPrice { get; set; }
        
        public decimal TotalAmount
        {
            get => _totalAmount;
            private set
            {
                if (_totalAmount != value)
                {
                    _totalAmount = value;
                    OnPropertyChanged(nameof(TotalAmount));
                }
            }
        }
        
        public string ReasonCode { get; set; } = "[مخزون]";

        [NotMapped]
        public ReturnItemCondition Condition
        {
            get
            {
                if (ReasonCode?.Contains("[هدر]") == true || ReasonCode?.Contains("[تالف]") == true || ReasonCode?.Contains("[Waste]") == true)
                    return ReturnItemCondition.Waste;
                return ReturnItemCondition.Restock;
            }
            set
            {
                string tag = value == ReturnItemCondition.Restock ? "[مخزون]" : "[هدر]";
                if (string.IsNullOrWhiteSpace(ReasonCode))
                {
                    ReasonCode = tag;
                }
                else if (ReasonCode.Contains("[مخزون]") || ReasonCode.Contains("[هدر]"))
                {
                    ReasonCode = ReasonCode.Replace("[مخزون]", tag).Replace("[هدر]", tag);
                }
                else
                {
                    ReasonCode = $"{tag} {ReasonCode}".Trim();
                }
            }
        }

        [NotMapped]
        public bool IsWaste => Condition == ReturnItemCondition.Waste;
        
        private void UpdateTotalPrice()
        {
            TotalAmount = Quantity * UnitPrice;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void InitializeTotalAmount()
        {
            UpdateTotalPrice();
        }
    }
}
