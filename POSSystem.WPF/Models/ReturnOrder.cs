using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSSystem.WPF.Models
{
    public class ReturnOrder
    {
        public int Id { get; set; }
        
        public string ReturnNumber { get; set; } = string.Empty;
        
        public int OriginalOrderId { get; set; }
        public Order OriginalOrder { get; set; } = null!;
        
        public int CashierId { get; set; }
        public User Cashier { get; set; } = null!;
        
        public decimal TotalAmount { get; set; }
        
        public DateTime ReturnDate { get; set; } = DateTime.Now;
        
        public string Reason { get; set; } = string.Empty;
        
        public PaymentMethod ReturnMethod { get; set; } = PaymentMethod.Cash;
        
        public List<ReturnOrderItem> ReturnOrderItems { get; set; } = new List<ReturnOrderItem>();

        [NotMapped]
        public string? RefundWallet
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Reason) || !Reason.Contains("[استرداد محفظة:")) return null;
                int start = Reason.IndexOf("[استرداد محفظة:", StringComparison.Ordinal) + "[استرداد محفظة:".Length;
                int end = Reason.IndexOf('|', start);
                if (end == -1) end = Reason.IndexOf(']', start);
                if (end == -1) end = Reason.Length;
                return Reason.Substring(start, end - start).Trim();
            }
        }

        [NotMapped]
        public string? RefundReferenceNumber
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Reason) || !Reason.Contains("مرجع:")) return null;
                int start = Reason.IndexOf("مرجع:", StringComparison.Ordinal) + "مرجع:".Length;
                int end = Reason.IndexOf(']', start);
                if (end == -1) end = Reason.Length;
                return Reason.Substring(start, end - start).Trim();
            }
        }

        [NotMapped]
        public string ReturnMethodText
        {
            get
            {
                if (ReturnMethod == PaymentMethod.MobilePayment)
                {
                    var wallet = RefundWallet;
                    return !string.IsNullOrWhiteSpace(wallet) ? $"إيداع ({wallet})" : "إيداع محفظة إلكترونية";
                }
                return ReturnMethod switch
                {
                    PaymentMethod.Cash => "نقدي (كاش)",
                    PaymentMethod.DebitCard => "شبكة (مدى)",
                    PaymentMethod.CreditCard => "بطاقة ائتمان",
                    PaymentMethod.OnCredit => "آجل (دين)",
                    PaymentMethod.Remake => "إعادة تجهيز",
                    _ => "غير محدد"
                };
            }
        }
    }
}
