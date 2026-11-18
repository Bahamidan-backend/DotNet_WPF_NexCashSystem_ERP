using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.Models.Helper
{
    public class CashBoxTransactionReport
    {
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public DateTime FirstTransactionTime { get; set; }
        public DateTime LastTransactionTime { get; set; }
        public decimal TotalAmount { get; set; }
        public int TransactionCount { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
