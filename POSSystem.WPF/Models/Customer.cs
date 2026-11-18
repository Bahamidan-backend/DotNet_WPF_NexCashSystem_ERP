using System;
using System.Collections.Generic;

namespace POSSystem.WPF.Models
{
    public class Customer : BaseModel
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public decimal CreditLimit { get; set; } = 0m;
        
        public List<CustomerDebt> Debts { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
    }
}
