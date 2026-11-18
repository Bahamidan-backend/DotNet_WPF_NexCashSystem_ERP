using System.ComponentModel.DataAnnotations.Schema;

namespace POSSystem.WPF.Models
{
    public class Shift : BaseModel
    {
        public string ShiftNumber { get; set; } = string.Empty;
        
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        
        public ShiftType Type { get; set; }
        
        public decimal StartingCash { get; set; }
        
        public decimal EndingCash { get; set; }
        
        public DateTime StartTime { get; set; } = DateTime.Now;
        
        public DateTime? EndTime { get; set; }
        
        public string? Notes { get; set; }
        
        // Navigation properties
        public List<CashBoxTransaction> CashBoxTransactions { get; set; } = new();
        
        // Computed properties
        [NotMapped]
        public decimal TotalSales => 0; // Will be calculated from orders

        [NotMapped]
        public decimal TotalCashIn => CashBoxTransactions?.Where(t => t.Type == CashBoxTransactionType.Income).Sum(t => t.Amount) ?? 0;
        
        [NotMapped]
        public decimal TotalCashOut => CashBoxTransactions?.Where(t => 
            t.Type == CashBoxTransactionType.Expense || 
            t.Type == CashBoxTransactionType.Advance || 
            t.Type == CashBoxTransactionType.Debt || 
            t.Type == CashBoxTransactionType.BankDeposit).Sum(t => t.Amount) ?? 0;

        [NotMapped]
        public decimal TotalCashInBox => StartingCash + TotalCashIn - TotalCashOut;
        
        [NotMapped]
        public decimal TotalBankDeposits => CashBoxTransactions?.Where(t => t.Type == CashBoxTransactionType.BankDeposit).Sum(t => t.Amount) ?? 0;
        
        [NotMapped]
        public decimal ExpectedEndingCash => StartingCash + TotalCashIn - TotalCashOut;

        [NotMapped]
        public decimal CashDifference => EndingCash - ExpectedEndingCash;

        [NotMapped]
        public string DifferenceStatusText => CashDifference switch
        {
            < 0 => $"عجز ({Math.Abs(CashDifference):N0} ريال)",
            > 0 => $"زيادة ({CashDifference:N0} ريال)",
            _ => "متطابق تماماً"
        };

        public string TypeText => Type switch
        {
            ShiftType.Morning => "صباحية",
            ShiftType.Evening => "مسائية",
            _ => "غير محدد"
        };
        
        public string StatusText => IsActive ? "نشطة" : "منتهية";
    }
    
    public enum ShiftType
    {
        Morning,    // صباحية
        Evening     // مسائية
    }
}
