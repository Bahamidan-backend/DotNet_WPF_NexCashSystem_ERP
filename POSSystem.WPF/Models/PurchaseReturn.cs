using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSSystem.WPF.Models
{
    public enum PurchaseReturnSettlementMode
    {
        DebitNote,      // خصم من رصيد المورد (إشعار مدين)
        CashReceipt     // استرداد نقدي فوري (سند قبض للخزينة)
    }

    public class PurchaseReturn : BaseModel
    {
        public string ReturnNumber { get; set; } = string.Empty;
        
        public int OriginalPurchaseId { get; set; }
        public virtual Purchase OriginalPurchase { get; set; } = null!;
        
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;
        
        public decimal TotalAmount { get; set; }
        
        public DateTime ReturnDate { get; set; } = DateTime.Now;
        
        public string Reason { get; set; } = string.Empty;
        
        public PaymentMethod ReturnMethod { get; set; } = PaymentMethod.OnCredit;
        
        public virtual List<PurchaseReturnItem> PurchaseReturnItems { get; set; } = new List<PurchaseReturnItem>();

        [NotMapped]
        public string? RefundChannel { get; set; }

        [NotMapped]
        public PurchaseReturnSettlementMode SettlementMode
        {
            get => ReturnMethod == PaymentMethod.Cash ? PurchaseReturnSettlementMode.CashReceipt : PurchaseReturnSettlementMode.DebitNote;
            set => ReturnMethod = (value == PurchaseReturnSettlementMode.CashReceipt) ? PaymentMethod.Cash : PaymentMethod.OnCredit;
        }

        [NotMapped]
        public string SettlementModeText => SettlementMode == PurchaseReturnSettlementMode.CashReceipt
            ? "استرداد نقدي (سند قبض)"
            : "خصم من رصيد المورد (إشعار مدين)";
    }
}
