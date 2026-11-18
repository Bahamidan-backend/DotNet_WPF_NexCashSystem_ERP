using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace POSSystem.WPF.Models
{
    public class PurchaseItem : INotifyPropertyChanged
    {
        private int _quantity;
        private decimal _unitPrice;
        private decimal _totalPrice;

        public int Id { get; set; }

        public int PurchaseId { get; set; }
        public Purchase? Purchase { get; set; }

        public int? ProductId { get; set; }
        public WarehouseFromProduct? Product { get; set; }

        public int? AddonId { get; set; }
        public Addon? Addon { get; set; }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    _totalPrice = _quantity * _unitPrice;
                    OnPropertyChanged(nameof(Quantity));
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (_unitPrice != value)
                {
                    _unitPrice = value;
                    _totalPrice = _quantity * _unitPrice;
                    OnPropertyChanged(nameof(UnitPrice));
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        public decimal TotalPrice
        {
            get => _totalPrice;
            set
            {
                if (_totalPrice != value)
                {
                    _totalPrice = value;
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        public string? Notes { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string DisplayName
        {
            get
            {
                string baseName = Product != null ? (!string.IsNullOrEmpty(Product.Name) ? Product.Name : $"صنف #{Product.Id}") :
                                  (Addon != null ? (!string.IsNullOrEmpty(Addon.Name) ? Addon.Name : $"إضافة #{Addon.Id}") : "غير معروف");
                if (!string.IsNullOrWhiteSpace(Notes) &&
                    (Notes.Contains("أنفار") || Notes.Contains("نفر") || Notes.Contains("حجم الكيس") || Notes.Contains("حجم العبوة")))
                {
                    return $"{baseName} ({Notes})";
                }
                return baseName;
            }
        }

        public virtual List<PurchaseReturnItem> PurchaseReturnItems { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
