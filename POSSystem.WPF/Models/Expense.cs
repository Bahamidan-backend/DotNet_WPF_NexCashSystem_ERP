namespace POSSystem.WPF.Models
{
    public class Expense : BaseModel
    {
        public decimal Amount { get; set; }
        
        public ExpenseType Type { get; set; }
        
        public string Category { get; set; } = string.Empty; // كهرباء، إيجار، موظفين، صيانة، إلخ
        
        public DateTime ExpenseDate { get; set; } = DateTime.Now;
        
        public string? InvoiceNumber { get; set; }
        
        public string? SupplierName { get; set; }
        
        public string? Notes { get; set; }
        
        public int? UserId { get; set; }
        public User? User { get; set; }

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsWalletExpense => PaymentMethod != PaymentMethod.Cash || (!string.IsNullOrWhiteSpace(Notes) && Notes.Contains("[محفظة:"));
        
        // Display properties
        public string TypeText => Type switch
        {
            ExpenseType.Monthly => "شهري",
            ExpenseType.Weekly => "أسبوعي",
            ExpenseType.Daily => "يومي",
            ExpenseType.OneTime => "مرة واحدة",
            _ => "غير محدد"
        };

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
                    PaymentMethod.CreditCard => "محفظة إلكترونية",
                    PaymentMethod.DebitCard => "شبكة / بطاقة",
                    _ => "نقدي (من الصندوق)"
                };
            }
        }
    }
    
    public enum ExpenseType
    {
        Monthly,    // شهري
        Weekly,     // أسبوعي
        Daily,      // يومي
        OneTime     // مرة واحدة
    }
}
