namespace POSSystem.WPF.Models
{
    public class Order
    {
        public int Id { get; set; }
        
        public string OrderNumber { get; set; } = string.Empty;
        
        public string OrderType { get; set; } = "dinein";
        
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        
        public decimal SubTotal { get; set; }
        
        public decimal ServiceCharge { get; set; }
        
        public decimal Discount { get; set; }
                
        public decimal Total { get; set; }
        
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        public DateTime? CompletedAt { get; set; }
        
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        
        public List<ReturnOrder> ReturnOrders { get; set; } = new List<ReturnOrder>();
        
        // Additional properties for UI display
        public string StatusText => Status switch
        {
            OrderStatus.Completed => "تم التوصيل",
            OrderStatus.Preparing => "قيد التجهيز",
            OrderStatus.Ready => "جاهز",
            OrderStatus.Served => "خرج مع السائق",
            OrderStatus.Cancelled => "ملغي",
            OrderStatus.Pending => "مدفوع",
            OrderStatus.Returned => "مرتجع",
            OrderStatus.PartiallyReturned => "مرتجع جزئي",
            _ => "غير محدد"
        };
        
        public string PaymentMethodText
        {
            get
            {
                if (PaymentMethod == PaymentMethod.MobilePayment || Notes?.Contains("[إيداع:") == true)
                {
                    var deposit = Helpers.OrderDepositHelper.ParseDepositInfo(Notes);
                    if (deposit != null)
                    {
                        var parts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(deposit.BankOrWallet)) parts.Add(deposit.BankOrWallet);
                        if (!string.IsNullOrWhiteSpace(deposit.Currency)) parts.Add(deposit.Currency);
                        return parts.Count > 0 ? $"إيداع ({string.Join(" - ", parts)})" : "إيداع";
                    }

                    if (!string.IsNullOrWhiteSpace(Notes))
                    {
                        var knownWallets = new[]
                        {
                            "حاسب كريمي", "محفظة جيب", "محفظة فلوسك", "محفظة ون كاش",
                            "محفظة جوالي", "محفظة موبايل موني", "محفظة يمن والت",
                            "الكريمي", "ون كاش", "جوالي", "جيب", "فلوسك",
                            "تحويل بنكي", "حوالة صرافة", "بنكك"
                        };
                        foreach (var w in knownWallets)
                        {
                            if (Notes.Contains(w, StringComparison.OrdinalIgnoreCase))
                                return $"إيداع ({w})";
                        }
                    }

                    return "إيداع محفظة";
                }

                if (PaymentMethod == PaymentMethod.Split)
                {
                    var split = Helpers.OrderSplitHelper.ParseSplit(Notes);
                    if (split != null)
                    {
                        var segs = new List<string>();
                        if (split.CashAmount > 0) segs.Add($"نقدي: {split.CashAmount:N0}");
                        if (split.CardAmount > 0)
                        {
                            string bankInfo = !string.IsNullOrWhiteSpace(split.BankOrWallet) ? $" ({split.BankOrWallet})" : "";
                            segs.Add($"إيداع{bankInfo}: {split.CardAmount:N0}");
                        }
                        if (split.DebtAmount > 0)
                        {
                            string custInfo = !string.IsNullOrWhiteSpace(split.CustomerName) ? $" ({split.CustomerName})" : "";
                            segs.Add($"آجل{custInfo}: {split.DebtAmount:N0}");
                        }
                        return segs.Count > 0 ? $"دفع مجزأ ({string.Join(" + ", segs)})" : "دفع مجزأ";
                    }
                    return "دفع مجزأ";
                }

                return PaymentMethod switch
                {
                    PaymentMethod.Cash => "نقدي",
                    PaymentMethod.CreditCard => "بطاقة ائتمان",
                    PaymentMethod.DebitCard => "بطاقة خصم",
                    PaymentMethod.MobilePayment => "دفع إلكتروني",
                    PaymentMethod.OnCredit => "دين",
                    _ => "غير محدد"
                };
            }
        }
        
        public string OrderTypeText => OrderType switch
        {
            "dinein" => "تناول في المطعم",
            "takeaway" => "طلب خارجي",
            "delivery" => "توصيل",
            _ => "غير محدد"
        };

        public int? DeliveryID { get; set; } 
        [ForeignKey("DeliveryID")]
        public Delivery? Delivery { get; set; } 
        
        public int ItemsCount => OrderItems?.Sum(item => item.Quantity) ?? 0;
        
        public int? ShiftId { get; set; }
        [ForeignKey("ShiftId")]
        public Shift? Shift { get; set; }

        public bool CanCancel => Status != OrderStatus.Cancelled && Status != OrderStatus.Returned && Status != OrderStatus.PartiallyReturned;
        
        public decimal TaxRate { get; set; } = 0m;
        public decimal TaxAmount { get; set; } = 0m;

        [NotMapped]
        public decimal Tax => TaxAmount > 0 ? TaxAmount : (TaxRate > 0 ? SubTotal * TaxRate : 0m);

        [NotMapped]
        public string CustomerDisplayName { get; set; } = "عميل مباشر";

        [NotMapped]
        public bool IsHighRisk { get; set; }

        [NotMapped]
        public bool HasDiscountWarning { get; set; }

        [NotMapped]
        public string RiskLabel { get; set; } = string.Empty;

        [NotMapped]
        public Helpers.OrderDepositInfo? DepositInfo => Helpers.OrderDepositHelper.ParseDepositInfo(Notes);

        [NotMapped]
        public bool IsDepositPayment => PaymentMethod == PaymentMethod.MobilePayment || DepositInfo != null;

        [NotMapped]
        public string DepositBankOrWallet
        {
            get
            {
                var info = DepositInfo;
                if (!string.IsNullOrWhiteSpace(info?.BankOrWallet)) return info.BankOrWallet;
                var split = Helpers.OrderSplitHelper.ParseSplit(Notes);
                if (!string.IsNullOrWhiteSpace(split?.BankOrWallet)) return split.BankOrWallet;
                if (!string.IsNullOrWhiteSpace(Notes))
                {
                    var knownWallets = new[]
                    {
                        "حاسب كريمي", "محفظة جيب", "محفظة فلوسك", "محفظة ون كاش",
                        "محفظة جوالي", "محفظة موبايل موني", "محفظة يمن والت",
                        "الكريمي", "ون كاش", "جوالي", "جيب", "فلوسك",
                        "تحويل بنكي", "حوالة صرافة", "بنكك"
                    };
                    foreach (var w in knownWallets)
                    {
                        if (Notes.Contains(w, StringComparison.OrdinalIgnoreCase))
                            return w;
                    }
                }
                return "إيداع عام";
            }
        }

        [NotMapped]
        public string DepositCurrency
        {
            get
            {
                var info = DepositInfo;
                return !string.IsNullOrWhiteSpace(info?.Currency) ? info.Currency : "ريال يمني";
            }
        }

        [NotMapped]
        public decimal DepositAmount
        {
            get
            {
                var info = DepositInfo;
                return (info != null && info.Amount > 0) ? info.Amount : Total;
            }
        }

        [NotMapped]
        public string DepositReferenceNumber
        {
            get
            {
                var info = DepositInfo;
                return !string.IsNullOrWhiteSpace(info?.ReferenceNumber) ? info.ReferenceNumber : "لا يوجد";
            }
        }

        [NotMapped]
        public string PaymentMethodDisplay
        {
            get
            {
                if (PaymentMethod == PaymentMethod.MobilePayment || Notes?.Contains("[إيداع:") == true)
                {
                    var deposit = DepositInfo;
                    if (deposit != null)
                    {
                        var parts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(deposit.BankOrWallet)) parts.Add(deposit.BankOrWallet);
                        if (!string.IsNullOrWhiteSpace(deposit.Currency)) parts.Add(deposit.Currency);

                        return parts.Count > 0
                            ? $"إيداع ({string.Join(" - ", parts)})"
                            : "إيداع محفظة/بنك";
                    }

                    // Fallback: check if Notes contains any known wallet name
                    if (!string.IsNullOrWhiteSpace(Notes))
                    {
                        var knownWallets = new[]
                        {
                            "حاسب كريمي", "محفظة جيب", "محفظة فلوسك", "محفظة ون كاش",
                            "محفظة جوالي", "محفظة موبايل موني", "محفظة يمن والت",
                            "الكريمي", "ون كاش", "جوالي", "جيب", "فلوسك",
                            "تحويل بنكي", "حوالة صرافة", "بنكك"
                        };
                        foreach (var w in knownWallets)
                        {
                            if (Notes.Contains(w, StringComparison.OrdinalIgnoreCase))
                                return $"إيداع ({w})";
                        }
                    }

                    return "إيداع محفظة إلكترونية";
                }

                return PaymentMethod switch
                {
                    PaymentMethod.Cash => "نقدي (كاش)",
                    PaymentMethod.DebitCard => "شبكة (مدى)",
                    PaymentMethod.CreditCard => "بطاقة ائتمان",
                    PaymentMethod.MobilePayment => "دفع إلكتروني",
                    PaymentMethod.OnCredit => "آجل (دين)",
                    PaymentMethod.BankTransfer => "تحويل بنكي",
                    _ => PaymentMethodText
                };
            }
        }
    }
    
    public class OrderItem : INotifyPropertyChanged
    {
        private int _quantity;
        private decimal _totalPrice;

        public int Id { get; set; }
        
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
        
        public int? ProductId { get; set; }
        public Product? Product { get; set; }
        
        public int? AddonId { get; set; }
        
        [ForeignKey("AddonId")]
        public Addon? Addon { get; set; }
        
        // Additional properties for UI display
        public string Name => Addon?.Name ?? Product?.Name ?? "";
        public decimal Price => UnitPrice;

        private string _size = string.Empty;
        public string Size
        {
            get => _size ?? string.Empty;
            set
            {
                if (_size != value)
                {
                    _size = value ?? string.Empty;
                    OnPropertyChanged(nameof(Size));
                }
            }
        }
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
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        private decimal _unitPrice;
        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (value < 0) value = 0;
                if (_unitPrice != value)
                {
                    _unitPrice = value;
                    OnPropertyChanged(nameof(UnitPrice));
                    OnPropertyChanged(nameof(Price));
                    UpdateTotalPrice();
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }
        
        /// <summary>
        /// تكلفة الوحدة التاريخية عند إنشاء الطلب (لحساب COGS بدقة حتى لو تغيرت أسعار المكونات لاحقاً)
        /// </summary>
        public decimal UnitCost { get; set; }

        /// <summary>
        /// إجمالي تكلفة البند عند إنشاء الطلب
        /// </summary>
        public decimal TotalCost { get; set; }
        
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
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }

        private void UpdateTotalPrice()
        {
            decimal modifiersPerUnit = addonAndOrderItems != null && addonAndOrderItems.Count > 0
                ? addonAndOrderItems.Where(a => a.Addon != null).Sum(a => a.Addon!.Price)
                : 0m;
            TotalPrice = Quantity * (UnitPrice + modifiersPerUnit);
        }
        public ICollection<AddonAndOrderItem> addonAndOrderItems { get; set; } = new List<AddonAndOrderItem>();

        public List<ReturnOrderItem> ReturnOrderItems { get; set; } = new List<ReturnOrderItem>();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public List<Addon> Addons { get; set; } = new List<Addon>();

        public OrderItem()
        {
            _quantity = 1;
            _totalPrice = 0;
        }

        public void InitializeTotalPrice()
        {
            UpdateTotalPrice();
        }
    }
    
    public enum OrderStatus
    {
        Pending,        // مدفوع
        Preparing,      // قيد التجهيز
        Ready,          // جاهز
        Served,         // قيد التوصيل / خرج مع السائق
        Completed,      // تم التوصيل
        Cancelled,      // ملغي
        Returned,       // مرتجع بالكامل
        PartiallyReturned // مرتجع جزئي
    }
    
    public enum PaymentMethod
    {
        Cash,           // نقدي
        CreditCard,     // بطاقة ائتمان
        DebitCard,      // بطاقة خصم
        MobilePayment,  // دفع إلكتروني
        OnCredit,       // آجل
        Remake,         // إعادة تجهيز (بدل تعويض) - للمرتجعات فقط
        BankTransfer,   // تحويل بنكي
        Split           // دفع مجزأ (نقدي + شبكة)
    }
}
