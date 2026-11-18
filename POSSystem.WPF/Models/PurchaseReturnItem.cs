using System;

namespace POSSystem.WPF.Models
{
    public class PurchaseReturnItem
    {
        public int Id { get; set; }
        
        public int PurchaseReturnId { get; set; }
        public virtual PurchaseReturn PurchaseReturn { get; set; } = null!;
        
        public int OriginalPurchaseItemId { get; set; }
        public virtual PurchaseItem OriginalPurchaseItem { get; set; } = null!;
        
        public int Quantity { get; set; }
        
        public decimal UnitPrice { get; set; }
        
        public decimal TotalAmount { get; set; }
        
        public string? ReasonCode { get; set; }
    }
}
