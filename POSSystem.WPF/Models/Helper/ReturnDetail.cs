using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.Models.Helper
{
    public class ReturnDetail
    {
        public int SaleId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal ReturnAmount { get; set; }
        public DateTime ReturnDate { get; set; }
        public string? Reason { get; set; }
    }
}
