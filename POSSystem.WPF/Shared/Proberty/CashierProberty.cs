using POSSystem.WPF.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.Shered.Proberty
{
    public class CashierProberty : BaseModel
    {
        public CashierProberty()
        {
            OrderItems = new ObservableCollection<OrderItem>();

            OrderItems.CollectionChanged += (s, e) => RecalculateTotal();
            OrderItems.CollectionChanged += OrderItems_CollectionChanged;
            POSSystem.WPF.Services.SettingsService.OnSettingsChanged += () => RecalculateTotal();
        }

        public void RecalculateTotal()
        {
            OnPropertyChanged(nameof(SubTotal));
            OnPropertyChanged(nameof(CountProduct));
            OnPropertyChanged(nameof(TaxAmount));
            OnPropertyChanged(nameof(TotalAmount));
            CalculateChange();
        }

        private void OrderItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (OrderItem item in e.NewItems)
                {
                    item.PropertyChanged += OrderItem_PropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (OrderItem item in e.OldItems)
                {
                    item.PropertyChanged -= OrderItem_PropertyChanged;
                }
            }

            if (OrderItems.Count == 0)
            {
                DiscountCode = string.Empty;
                DiscountAmount = 0m;
                HasDiscount = false;
            }
            else if (HasDiscount && DiscountAmount > SubTotal)
            {
                DiscountAmount = SubTotal;
            }
            RecalculateTotal();
        }

        private void OrderItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OrderItem.TotalPrice) ||
                e.PropertyName == nameof(OrderItem.Quantity) ||
                e.PropertyName == nameof(OrderItem.UnitPrice))
            {
                RecalculateTotal();
            }
        }

        public ObservableCollection<string> _deliveryDrivers = new ObservableCollection<string>();

        private string _searchText { get; set; } = "";
        private string _orderType { get; set; } = "dinein";
        private string _paymentMethod { get; set; } = "qrcode";
        private string _discountCode { get; set; } = string.Empty;
        private decimal _discountAmount { get; set; } = 0;
        private bool _hasDiscount { get; set; } = false;
        private bool _allCategoriesSelected { get; set; } = true;
        private string _nextOrderNumber { get; set; } = "1";
        private bool _printCustomerThisOrder = true;
        private bool _printKitchenThisOrder = true;

        // Delivery properties
        private string _deliveryCustomerName { get; set; } = string.Empty;
        private string _deliveryPhone { get; set; } = string.Empty;
        private string _deliveryAddress { get; set; } = string.Empty;
        private string _selectedDeliveryDriver { get; set; } = string.Empty;
        private Customer? _selectedDeliveryCustomer { get; set; }
        private bool _isAutoFilling = false;
        private decimal _deliveryFee { get; set; } = 0;
        private bool _hasActiveShift { get; set; } 

        public ObservableCollection<Discount> AvailableDiscounts { get; } = new ObservableCollection<Discount>();

        private Discount? _selectedDiscount;
        public Discount? SelectedDiscount
        {
            get => _selectedDiscount;
            set
            {
                if (SetProperty(ref _selectedDiscount, value))
                {
                    if (value != null && !string.IsNullOrWhiteSpace(value.Code))
                    {
                        DiscountCode = value.Code;
                    }
                }
            }
        }

        public ObservableCollection<Customer> DeliveryCustomers { get; } = new ObservableCollection<Customer>();

        public Customer? SelectedDeliveryCustomer
        {
            get => _selectedDeliveryCustomer;
            set
            {
                if (_selectedDeliveryCustomer != value)
                {
                    _selectedDeliveryCustomer = value;
                    OnPropertyChanged(nameof(SelectedDeliveryCustomer));

                    if (value != null)
                    {
                        _isAutoFilling = true;
                        DeliveryCustomerName = value.Name;
                        DeliveryPhone = value.PhoneNumber ?? string.Empty;
                        DeliveryAddress = value.Address ?? string.Empty;
                        _isAutoFilling = false;
                    }
                }
            }
        }

        public string ShiftButtonText => HasActiveShift ? "🕐 المناوبة نشطة" : "🕐 المناوبة";

        public string ShiftButtonColor => HasActiveShift ? "#01922a" : "#6c757d";

        public ObservableCollection<string> DeliveryDrivers => _deliveryDrivers;

        public bool AllCategoriesSelected
        {
            get => _allCategoriesSelected;
            set
            {
                _allCategoriesSelected = value;
                OnPropertyChanged();
            }
        }

        public string OrderType
        {
            get => _orderType;
            set
            {
                if (_orderType != value)
                {
                    _orderType = value;
                    OnPropertyChanged(nameof(OrderType));
                    OnPropertyChanged(nameof(IsDeliveryOrder));
                    OnPropertyChanged(nameof(TotalAmount));
                }
            }
        }

        public bool IsDeliveryOrder => OrderType == "delivery";

        public string DeliveryCustomerName
        {
            get => _deliveryCustomerName;
            set
            {
                if (_deliveryCustomerName != value)
                {
                    _deliveryCustomerName = value;
                    OnPropertyChanged(nameof(DeliveryCustomerName));

                    if (!_isAutoFilling && DeliveryCustomers != null && !string.IsNullOrWhiteSpace(value))
                    {
                        var matched = DeliveryCustomers.FirstOrDefault(c =>
                            string.Equals(c.Name?.Trim(), value.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (matched != null && _selectedDeliveryCustomer != matched)
                        {
                            _isAutoFilling = true;
                            _selectedDeliveryCustomer = matched;
                            OnPropertyChanged(nameof(SelectedDeliveryCustomer));
                            DeliveryPhone = matched.PhoneNumber ?? string.Empty;
                            DeliveryAddress = matched.Address ?? string.Empty;
                            _isAutoFilling = false;
                        }
                    }
                }
            }
        }

        public string DeliveryPhone
        {
            get => _deliveryPhone;
            set
            {
                if (_deliveryPhone != value)
                {
                    _deliveryPhone = value;
                    OnPropertyChanged(nameof(DeliveryPhone));

                    if (!_isAutoFilling && DeliveryCustomers != null && !string.IsNullOrWhiteSpace(value))
                    {
                        var matched = DeliveryCustomers.FirstOrDefault(c =>
                            string.Equals(c.PhoneNumber?.Trim(), value.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (matched != null && _selectedDeliveryCustomer != matched)
                        {
                            _isAutoFilling = true;
                            _selectedDeliveryCustomer = matched;
                            OnPropertyChanged(nameof(SelectedDeliveryCustomer));
                            DeliveryCustomerName = matched.Name;
                            DeliveryAddress = matched.Address ?? string.Empty;
                            _isAutoFilling = false;
                        }
                    }
                }
            }
        }

        public string DeliveryAddress
        {
            get => _deliveryAddress;
            set
            {
                if (_deliveryAddress != value)
                {
                    _deliveryAddress = value;
                    OnPropertyChanged(nameof(DeliveryAddress));
                }
            }
        }

        public string SelectedDeliveryDriver
        {
            get => _selectedDeliveryDriver;
            set
            {
                if (_selectedDeliveryDriver != value)
                {
                    _selectedDeliveryDriver = value;
                    OnPropertyChanged(nameof(SelectedDeliveryDriver));
                }
            }
        }

        public decimal DeliveryFee
        {
            get => _deliveryFee;
            set
            {
                if (_deliveryFee != value)
                {
                    _deliveryFee = value;
                    OnPropertyChanged(nameof(DeliveryFee));
                    OnPropertyChanged(nameof(DeliveryFeeText));
                    OnPropertyChanged(nameof(TotalAmount));
                }
            }
        }

        public string DeliveryFeeText
        {
            get => _deliveryFee.ToString("N0");
            set
            {
                if (decimal.TryParse(value, out var fee))
                {
                    DeliveryFee = fee;
                }
                else if (string.IsNullOrWhiteSpace(value))
                {
                    DeliveryFee = 0;
                }
            }
        }

        public string PaymentMethod
        {
            get => _paymentMethod;
            set
            {
                if (_paymentMethod != value)
                {
                    _paymentMethod = value;
                    OnPropertyChanged(nameof(PaymentMethod));
                }
            }
        }

        public string DiscountCode
        {
            get => _discountCode;
            set
            {
                if (_discountCode != value)
                {
                    _discountCode = value;
                    OnPropertyChanged(nameof(DiscountCode));
                    if (string.IsNullOrWhiteSpace(_discountCode) && _hasDiscount)
                    {
                        DiscountAmount = 0m;
                        HasDiscount = false;
                    }
                }
            }
        }

        public decimal DiscountAmount
        {
            get => _discountAmount;
            set
            {
                if (_discountAmount != value)
                {
                    _discountAmount = value;
                    OnPropertyChanged(nameof(DiscountAmount));
                    OnPropertyChanged(nameof(TotalAmount));
                }
            }
        }

        public bool HasDiscount
        {
            get => _hasDiscount;
            set
            {
                if (_hasDiscount != value)
                {
                    _hasDiscount = value;
                    OnPropertyChanged(nameof(HasDiscount));
                }
            }
        }

        public string NextOrderNumber
        {
            get => _nextOrderNumber;
            set
            {
                if (_nextOrderNumber != value)
                {
                    _nextOrderNumber = value;
                    OnPropertyChanged(nameof(NextOrderNumber));
                }
            }
        }

        public bool PrintCustomerThisOrder
        {
            get => _printCustomerThisOrder;
            set
            {
                if (_printCustomerThisOrder != value)
                {
                    _printCustomerThisOrder = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool PrintKitchenThisOrder
        {
            get => _printKitchenThisOrder;
            set
            {
                if (_printKitchenThisOrder != value)
                {
                    _printKitchenThisOrder = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<OrderItem> OrderItems { get; }

        public decimal SubTotal
        {
            get
            {
                decimal total = 0;
                foreach (var item in OrderItems)
                {
                    total += item.TotalPrice;
                }
                if (total <= 0)
                {
                    total = 0;
                }
                return total;
            }
        }

        public decimal CountProduct => OrderItems.Sum(z => z.Quantity);

        public decimal TaxAmount
        {
            get
            {
                try
                {
                    var settings = POSSystem.WPF.Services.SettingsService.LoadSettings();
                    if (settings != null && settings.TaxRate > 0 && !settings.TaxIncludedInPrice)
                    {
                        var subTotal = OrderItems.Sum(item => item.TotalPrice);
                        var afterDiscount = Math.Max(0, subTotal - DiscountAmount);
                        return Math.Round(afterDiscount * (settings.TaxRate / 100m), 2);
                    }
                }
                catch { }
                return 0m;
            }
        }

        public decimal TotalAmount
        {
            get
            {
                var subTotal = OrderItems.Sum(item => item.TotalPrice);
                var total = Math.Max(0, subTotal - DiscountAmount);
                total += TaxAmount;

                if (IsDeliveryOrder)
                {
                    total += DeliveryFee;
                }

                return Math.Max(0, total);
            }
        }

        public bool HasActiveShift
        {
            get => _hasActiveShift;
            set
            {
                if (_hasActiveShift != value)
                {
                    _hasActiveShift = value;
                    OnPropertyChanged(nameof(HasActiveShift));
                    OnPropertyChanged(nameof(ShiftButtonText));
                    OnPropertyChanged(nameof(ShiftButtonColor));
                }
            }
        }

        private decimal _paidAmount;
        public decimal PaidAmount
        {
            get => _paidAmount;
            set
            {
                if (_paidAmount != value)
                {
                    _paidAmount = value;
                    OnPropertyChanged(nameof(PaidAmount));
                    CalculateChange();
                }
            }
        }

        private decimal _changeDue;
        public decimal ChangeDue
        {
            get => _changeDue;
            set
            {
                if (_changeDue != value)
                {
                    _changeDue = value;
                    OnPropertyChanged(nameof(ChangeDue));
                }
            }
        }

        private void CalculateChange()
        {
            if (PaidAmount > 0)
            {
                ChangeDue = Math.Max(0, PaidAmount - TotalAmount);
            }
            else
            {
                ChangeDue = 0;
            }
        }
    }
}
