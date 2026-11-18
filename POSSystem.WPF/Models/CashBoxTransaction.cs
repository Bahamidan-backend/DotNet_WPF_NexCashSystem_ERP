
namespace POSSystem.WPF.Models
{
    public class CashBoxTransaction : BaseModel
    {
        public CashBoxTransaction()
        {
            Name = "حركة نقدية";
        }

        public int ShiftId { get; set; }
        public Shift Shift { get; set; } = null!;
        
        public int? UserId { get; set; }
        public User? User { get; set; }
        
        public CashBoxTransactionType Type { get; set; }
        
        public decimal Amount { get; set; }
        
        public new string Description { get; set; } = string.Empty;
        
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        
        public string? Notes { get; set; }
        
        // For advance/salary deduction tracking
        public int? EmployeeAdvanceId { get; set; }
        public EmployeeAdvance? EmployeeAdvance { get; set; }
        
        // تمييز المصروفات الشخصية عن مصروفات المطعم
        public bool IsPersonal { get; set; } = false; // true = شخصي (ينقص من الراتب), false = للمطعم (ينقص من فلوس المطعم)
        
        public string TypeText => Type switch
        {
            CashBoxTransactionType.Income => "دخل",
            CashBoxTransactionType.Expense => "مصروف",
            CashBoxTransactionType.Advance => "سلفة",
            CashBoxTransactionType.Debt => "دين",
            CashBoxTransactionType.BankDeposit => "توريد بنكي",
            _ => "غير محدد"
        };
        
        public string ExpenseTypeText => Type switch
        {
            CashBoxTransactionType.Expense => (IsPersonal ? "شخصي (ينقص من الراتب)" : "للمطعم"),
            CashBoxTransactionType.BankDeposit => "إيداع بنكي",
            CashBoxTransactionType.Advance => "سلفة موظف",
            CashBoxTransactionType.Debt => "دين",
            CashBoxTransactionType.Income => "-",
            _ => "-"
        };
    }
    
    public enum CashBoxTransactionType
    {
        Income,     // دخل (من المبيعات)
        Expense,    // مصروف
        Advance,    // سلفة موظف
        Debt,       // دين من صندوق
        BankDeposit // توريد بنكي / إيداع في البنك
    }
}
