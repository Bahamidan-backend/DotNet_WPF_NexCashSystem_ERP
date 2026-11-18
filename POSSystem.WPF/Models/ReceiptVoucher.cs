
namespace POSSystem.WPF.Models
{
    public class ReceiptVoucher : BaseModel
    {
        public string VoucherNumber { get; set; } = string.Empty;
        
        public decimal Amount { get; set; }
        
        public VoucherType Type { get; set; }
        
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public int? TargetShiftId { get; set; }
        
        public string CustomerName { get; set; } = string.Empty;
        
        public DateTime VoucherDate { get; set; } = DateTime.Now;
        
        public string? Notes { get; set; }
        
        public int? UserId { get; set; }
        public User? User { get; set; }
        
        public int? CustomerDebtId { get; set; }
        public CustomerDebt? CustomerDebt { get; set; }
        
        public int? EmployeeAdvanceId { get; set; }
        public EmployeeAdvance? EmployeeAdvance { get; set; }
        
        // Display properties
        public string TypeText => Type switch
        {
            VoucherType.Receipt => "سند قبض",
            VoucherType.Payment => "سند صرف",
            _ => "غير محدد"
        };
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string PaymentMethodText
        {
            get
            {
                if (PaymentMethod != PaymentMethod.Cash && !string.IsNullOrWhiteSpace(Notes))
                {
                    string[] prefixes = { "[محفظة:", "[جهة الصرف:", "[طريقة الصرف:", "[حساب:" };
                    foreach (var p in prefixes)
                    {
                        var idx = Notes.IndexOf(p, System.StringComparison.Ordinal);
                        if (idx >= 0)
                        {
                            var end = Notes.IndexOf(']', idx);
                            if (end > idx)
                            {
                                var wallet = Notes.Substring(idx + p.Length, end - idx - p.Length).Trim();
                                if (!string.IsNullOrWhiteSpace(wallet))
                                    return wallet;
                            }
                        }
                    }
                    foreach (var w in POSSystem.WPF.Helpers.FinancialChannelHelper.UnifiedWallets)
                    {
                        if (Notes.Contains(w))
                            return w;
                    }
                }

                return PaymentMethod switch
                {
                    PaymentMethod.Cash => "نقدي (من الصندوق)",
                    PaymentMethod.BankTransfer => "تحويل بنكي",
                    PaymentMethod.MobilePayment => "محفظة إلكترونية",
                    PaymentMethod.DebitCard or PaymentMethod.CreditCard => "شبكة / بطاقة",
                    _ => "نقدي (من الصندوق)"
                };
            }
        }
    }
    
    public enum VoucherType
    {
        Receipt,    // سند قبض
        Payment     // سند صرف
    }
}

