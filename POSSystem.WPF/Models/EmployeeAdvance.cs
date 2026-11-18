
namespace POSSystem.WPF.Models
{
    public class EmployeeAdvance
    {
        public int Id { get; set; }
        
        public int EmployeeId { get; set; }
        public User? Employee { get; set; }
        
        public decimal Amount { get; set; }
        
        public decimal PaidAmount { get; set; }
        
        public decimal RemainingAmount => Amount - PaidAmount;
        
        public DateTime AdvanceDate { get; set; } = DateTime.Now;
        
        public DateTime? RepaymentDate { get; set; }
        
        public AdvanceRepaymentMethod RepaymentMethod { get; set; }
        
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        
        public int? ReceiptVoucherId { get; set; }
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public int? TargetShiftId { get; set; }
        
        public bool IsRepaid { get; set; } = false;
        
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        // Display properties
        public string StatusText => IsRepaid ? "مسددة" : "غير مسددة";
        public string RepaymentMethodText => RepaymentMethod switch
        {
            AdvanceRepaymentMethod.MonthlyDeduction => "خصم من الراتب",
            AdvanceRepaymentMethod.SpecificDate => "تاريخ محدد",
            AdvanceRepaymentMethod.Installment => "سداد تقسيط",
            _ => "غير محدد"
        };
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string PayoutMethodText => PaymentMethod switch
        {
            PaymentMethod.Cash => "نقداً من الصندوق",
            PaymentMethod.BankTransfer => "تحويل بنكي",
            PaymentMethod.MobilePayment => "محفظة إلكترونية",
            PaymentMethod.DebitCard or PaymentMethod.CreditCard => "بطاقة بنكية / شبكة",
            _ => "خزينة خارجية / أخرى"
        };
    }
    
    public enum AdvanceRepaymentMethod
    {
        MonthlyDeduction,  // خصم من الراتب
        SpecificDate,      // تاريخ محدد
        Installment        // سداد تقسيط
    }
}

