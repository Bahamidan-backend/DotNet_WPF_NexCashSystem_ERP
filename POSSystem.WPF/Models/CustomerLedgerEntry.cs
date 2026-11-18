using System;

namespace POSSystem.WPF.Models
{
    public class CustomerLedgerEntry
    {
        public DateTime Date { get; set; } = DateTime.Now;
        public string TransactionType { get; set; } = string.Empty; // نوع الحركة (فاتورة بيع آجل، سداد دين، مرتجع مبيعات)
        public string ReferenceNumber { get; set; } = string.Empty; // رقم المرجع (رقم الفاتورة، رقم السند، رقم المرتجع)
        public decimal Debit { get; set; }    // مدين (فواتير بيع آجلة تزيد من المديونية)
        public decimal Credit { get; set; }   // دائن (سدادات ومرتجعات تخفض من المديونية)
        public decimal Balance { get; set; }  // الرصيد التراكمي المتدرج بعد هذه الحركة
        public string? Notes { get; set; }
        public string? CashierName { get; set; }

        public string DisplayText => $"{TransactionType} ({ReferenceNumber})";
    }
}

