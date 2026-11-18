
namespace POSSystem.WPF.Models
{
    public class CustomerDebt
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;
        
        private string _tempCustomerName = string.Empty;
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string CustomerName 
        { 
            get => Customer?.Name ?? _tempCustomerName; 
            set => _tempCustomerName = value; 
        }

        private string? _tempPhoneNumber;
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string? PhoneNumber 
        { 
            get => Customer?.PhoneNumber ?? _tempPhoneNumber; 
            set => _tempPhoneNumber = value; 
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public decimal? CreditLimit { get; set; }

        public decimal Amount { get; set; } // المبلغ المدين
        
        public decimal PaidAmount { get; set; } // المبلغ المدفوع
        
        public decimal RemainingAmount => Amount - PaidAmount; // المبلغ المتبقي
        
        public DateTime DebtDate { get; set; } // تاريخ الدين
        
        public DateTime? PaymentDate { get; set; } // تاريخ السداد
        
        public bool IsPaid { get; set; } = false; // تم السداد
        
        public string? Notes { get; set; } // ملاحظات
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        public int? UserId { get; set; } // الشخص الذي استلم المبلغ
        public int? ShiftId { get; set; } // المناوبة التي تم فيها السداد
        
        public int? OrderId { get; set; } // الطلب المرتبط بالدين
        public Order? Order { get; set; }
        
        // Display properties
        public string StatusText => IsPaid ? "مسدد" : "مدين";
        
        public string DebtAgeText
        {
            get
            {
                if (IsPaid) return "مسدد";
                var days = (DateTime.Now - DebtDate).Days;
                if (days == 0) return "اليوم";
                if (days == 1) return "أمس";
                if (days < 7) return $"{days} أيام";
                if (days < 30) return $"{days / 7} أسابيع";
                if (days < 365) return $"{days / 30} أشهر";
                return $"{days / 365} سنوات";
            }
        }
        
        public bool IsOldDebt => !IsPaid && (DateTime.Now - DebtDate).Days > 30; // دين قديم أكثر من 30 يوم


    }
}

