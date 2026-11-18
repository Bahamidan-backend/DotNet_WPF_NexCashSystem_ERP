using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSSystem.WPF.Models
{
    public enum FinancialAccountType
    {
        MainCashBox = 0,     // الصندوق الرئيسي
        BankAccount = 1,     // حساب بنكي
        ElectronicWallet = 2 // محفظة إلكترونية
    }

    public enum FinancialTransactionType
    {
        Deposit = 0,         // إيداع
        Withdrawal = 1,      // سحب
        TransferOut = 2,     // تحويل صادر
        TransferIn = 3,      // تحويل وارد
        OwnerBankDeposit = 4,// إيداع صاحب المطعم من الصندوق إلى البنك
        SaleCollection = 5,  // تحصيل مبيعات
        DebtCollection = 6,  // تحصيل دين عميل
        ExpensePayment = 7,  // صرف مصروف
        PurchasePayment = 8, // دفع فاتورة مشتريات
        SalesReturn = 9,     // استرداد مرتجع مبيعات
        Reversal = 10        // قيد عكسي / تصحيح
    }

    [Table("FinancialAccounts")]
    public class FinancialAccount : BaseModel
    {
        [Required]
        [MaxLength(150)]
        public string AccountName { get; set; } = string.Empty;

        public FinancialAccountType AccountType { get; set; } = FinancialAccountType.BankAccount;

        [MaxLength(150)]
        public string BankOrWalletName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? AccountNumber { get; set; }

        [MaxLength(50)]
        public string Currency { get; set; } = "ريال يمني";

        public decimal CurrentBalance { get; set; }

        public decimal TotalInflow { get; set; }

        public decimal TotalOutflow { get; set; }

        public DateTime? LastTransactionDate { get; set; }

        public bool AllowOverdraft { get; set; } = false;

        public virtual ICollection<FinancialAccountTransaction> Transactions { get; set; } = new List<FinancialAccountTransaction>();

        [NotMapped]
        public string AccountTypeText => AccountType switch
        {
            FinancialAccountType.MainCashBox => "الصندوق الرئيسي",
            FinancialAccountType.BankAccount => "حساب بنكي",
            FinancialAccountType.ElectronicWallet => "محفظة إلكترونية",
            _ => "حساب مالي"
        };

        [NotMapped]
        public string StatusText => IsActive ? "نشط" : "معطل";

        [NotMapped]
        public string StatusBadgeBackground => IsActive ? "#dcfce7" : "#fee2e2";

        [NotMapped]
        public string StatusBadgeForeground => IsActive ? "#166534" : "#991b1b";

        [NotMapped]
        public string LastTransactionDisplay => LastTransactionDate.HasValue
            ? LastTransactionDate.Value.ToString("yyyy/MM/dd hh:mm tt")
            : "لا توجد حركات";
    }

    [Table("FinancialAccountTransactions")]
    public class FinancialAccountTransaction : BaseModel
    {
        public int FinancialAccountId { get; set; }

        [ForeignKey(nameof(FinancialAccountId))]
        public virtual FinancialAccount? FinancialAccount { get; set; }

        public int? RelatedAccountId { get; set; }

        [ForeignKey(nameof(RelatedAccountId))]
        public virtual FinancialAccount? RelatedAccount { get; set; }

        public FinancialTransactionType TransactionType { get; set; }

        public decimal AmountIn { get; set; }

        public decimal AmountOut { get; set; }

        public decimal BalanceAfter { get; set; }

        [MaxLength(120)]
        public string? ReferenceNumber { get; set; }

        [MaxLength(100)]
        public string? SourceEntityType { get; set; } // Order, Expense, Purchase, CustomerDebt, Transfer, Manual

        public int? SourceEntityId { get; set; }

        public int? PerformedByUserId { get; set; }

        [MaxLength(150)]
        public string PerformedByUserName { get; set; } = "النظام";

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [MaxLength(500)]
        public string? Notes { get; set; }

        [NotMapped]
        public string TransactionTypeText => TransactionType switch
        {
            FinancialTransactionType.Deposit => "إيداع",
            FinancialTransactionType.Withdrawal => "سحب",
            FinancialTransactionType.TransferOut => "تحويل صادر",
            FinancialTransactionType.TransferIn => "تحويل وارد",
            FinancialTransactionType.OwnerBankDeposit => "إيداع من الصندوق للبنك (تحويل داخلي)",
            FinancialTransactionType.SaleCollection => "تحصيل مبيعات",
            FinancialTransactionType.DebtCollection => "تحصيل دين عميل",
            FinancialTransactionType.ExpensePayment => "دفع مصروف",
            FinancialTransactionType.PurchasePayment => "دفع مشتريات",
            FinancialTransactionType.SalesReturn => "مرتجع مبيعات",
            FinancialTransactionType.Reversal => "حركة عكسية",
            _ => "حركة مالية"
        };
    }
}
