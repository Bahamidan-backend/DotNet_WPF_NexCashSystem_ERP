using System;

namespace POSSystem.WPF.Models
{
    public class EmployeeLedgerEntry
    {
        public DateTime Date { get; set; } = DateTime.Now;
        public string TransactionType { get; set; } = string.Empty; // نوع الحركة (استحقاق راتب شهري، صرف سلفة، سداد سلفة، صرف وتسليم راتب)
        public string ReferenceNumber { get; set; } = string.Empty; // رقم السند / رقم السلفة / شهر الراتب
        public decimal Debit { get; set; }    // مدين (مسحوبات / على الموظف: صرف سلفة، تسليم وصرف راتب)
        public decimal Credit { get; set; }   // دائن (مستحقات / للموظف: استحقاق راتب شهري، سداد سلفة)
        public decimal Balance { get; set; }  // الرصيد التراكمي (+ له مستحق / - عليه سلف متبقية)
        public string? Notes { get; set; }
        public string? PayoutMethod { get; set; } // طريقة الصرف (نقداً، محفظة جيب، ون كاش...)

        public string DisplayText => $"{TransactionType} ({ReferenceNumber})";
    }
}

