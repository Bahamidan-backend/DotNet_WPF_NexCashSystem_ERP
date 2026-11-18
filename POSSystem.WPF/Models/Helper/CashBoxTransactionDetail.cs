using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.Models.Helper
{
    public class CashBoxTransactionDetail
    {
        public int TransactionId { get; set; }
        public DateTime Date { get; set; }
        public string Time { get; set; } = string.Empty;
        public string CashierName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsPersonal { get; set; }
        public string ExpenseType { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
