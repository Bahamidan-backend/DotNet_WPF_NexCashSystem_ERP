using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.Models.Helper
{
    public class ProductSaleInfo
    {

        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public decimal TotalQuantity { get; set; }

    }
}
