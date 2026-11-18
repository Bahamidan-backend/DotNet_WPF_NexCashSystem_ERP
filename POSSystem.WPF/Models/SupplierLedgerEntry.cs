using System;

namespace POSSystem.WPF.Models
{
    public class SupplierLedgerEntry
    {
        public DateTime Date { get; set; } = DateTime.Now;
        public string TransactionType { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public decimal Debit { get; set; }    // مدين (دفعات مسددة / إشعارات مدينة تخصم من الرصيد)
        public decimal Credit { get; set; }   // دائن (مشتريات آجلة تزيد الرصيد)
        public decimal Balance { get; set; }  // الرصيد التراكمي بعد الحركة
        public string? Notes { get; set; }

        public string DisplayText => $"{TransactionType} ({ReferenceNumber})";
    }
}

