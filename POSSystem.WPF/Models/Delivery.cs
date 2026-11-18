using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.Models
{
    public class Delivery
    {
        public int DeliveryId { get; set; }
        public string? DeliveryCustomerName { get; set; }
        public string? DeliveryPhone { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? DeliveryDriverName { get; set; }
        public decimal DeliveryFee { get; set; } = 0;

      
    }
}
