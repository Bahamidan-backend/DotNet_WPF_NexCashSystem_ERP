
namespace POSSystem.WPF.Models
{
    public class EmployeeSalary
    {
        public int Id { get; set; }
        
        public int EmployeeId { get; set; }
        public User? Employee { get; set; }
        
        public decimal BaseSalary { get; set; }
        
        public decimal AdvanceDeduction { get; set; }
        
        public decimal NetSalary { get; set; }
        
        public DateTime SalaryDate { get; set; } = DateTime.Now;
        
        public DateTime PaymentDate { get; set; }
        
        public bool IsPaid { get; set; } = false;
        
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public int? TargetShiftId { get; set; }
        
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        // Display properties
        public string StatusText => IsPaid ? "مدفوعة" : "غير مدفوعة";

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string PayoutMethodText
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
                                var w = Notes.Substring(idx + p.Length, end - idx - p.Length).Trim();
                                if (!string.IsNullOrWhiteSpace(w)) return w;
                            }
                        }
                    }
                    foreach (var w in POSSystem.WPF.Helpers.FinancialChannelHelper.UnifiedWallets)
                    {
                        if (Notes.Contains(w)) return w;
                    }
                }

                return PaymentMethod switch
                {
                    PaymentMethod.Cash => "نقداً من الصندوق",
                    PaymentMethod.BankTransfer => "تحويل بنكي",
                    PaymentMethod.MobilePayment => "محفظة إلكترونية",
                    PaymentMethod.DebitCard or PaymentMethod.CreditCard => "بطاقة بنكية / شبكة",
                    _ => "خزينة خارجية / أخرى"
                };
            }
        }
    }
}

