using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.Models.Helper
{
    public class CashBoxReport
    {
        public int TransactionId { get; set; }
        public int ShiftId { get; set; }
        public string ShiftNumber { get; set; } = string.Empty;
        public string CashierName { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string TransactionTime { get; set; } = string.Empty;
        public bool IsPersonal { get; set; }
        public string ExpenseTypeText { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
