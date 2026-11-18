

namespace POSSystem.WPF.Models
{
    public class Sale
    {
        public int Id { get; set; }
        
        public string InvoiceNumber { get; set; } = string.Empty;
        
        public decimal TotalAmount { get; set; }
        
        public decimal TaxAmount { get; set; }
        
        public decimal DiscountAmount { get; set; }
        
        public decimal FinalAmount { get; set; }
        
        public PaymentMethod PaymentMethod { get; set; }
        
        public SaleStatus Status { get; set; }
        
        public int? UserId { get; set; }
        public User? User { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public string? Notes { get; set; }
        
        public List<SaleItem> SaleItems { get; set; } = new();
    }
    
    public class SaleItem
    {
        public int Id { get; set; }
        
        public int SaleId { get; set; }
        public Sale? Sale { get; set; }
        
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        
        public string? Size { get; set; }
        public int Quantity { get; set; }
        
        public decimal UnitPrice { get; set; }
        
        public decimal TotalPrice { get; set; }
        
        public decimal DiscountAmount { get; set; }

        
        public string? Notes { get; set; }
    }
    
    public enum SaleStatus
    {
        Pending,
        Completed,
        Cancelled,
        Refunded
    }
}
