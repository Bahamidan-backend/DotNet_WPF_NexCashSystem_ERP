using System;

namespace POSSystem.WPF.Models
{
    public class SupplierPayment : BaseModel
    {
        public int SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }

        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        
        public string? Notes { get; set; }

        /// <summary>
        /// User/Manager who authorized the payment.
        /// </summary>
        public int? UserId { get; set; }
        public virtual User? User { get; set; }
    }
}
