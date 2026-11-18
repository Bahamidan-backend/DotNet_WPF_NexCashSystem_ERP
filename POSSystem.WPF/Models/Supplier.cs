using System;
using System.Collections.Generic;

namespace POSSystem.WPF.Models
{
    public class Supplier : BaseModel
    {
        public new string Name { get; set; } = string.Empty;
        public string? VatNumber { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        
        public decimal TotalBalance { get; set; } = 0;

        public new DateTime CreatedAt { get; set; } = DateTime.Now;
        public new DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
        public virtual ICollection<SupplierPayment> SupplierPayments { get; set; } = new List<SupplierPayment>();
    }
}
