using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.Models.Helper
{
    public class DeliveryReport
    {
        public int UserId { get; set; }
        public string DeliveryPersonName { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalOrderValue { get; set; }
        public int CompletedOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal TotalDeliveryFees { get; set; }
    }
}
