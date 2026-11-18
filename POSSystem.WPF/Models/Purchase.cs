using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using POSSystem.WPF.Helpers;

namespace POSSystem.WPF.Models
{
    public class Purchase : BaseModel
    {
        public Purchase()
        {
            Name = "فاتورة مشتريات";
        }

        public string PurchaseNumber { get; set; } = string.Empty;
        
        public decimal TotalAmount { get; set; }
        
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
        
        public int? SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }
        public string SupplierName { get; set; } = string.Empty; // Keep for backward compatibility or as backup
        
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        public DateTime? DueDate { get; set; }
        
        public string? InvoiceNumber { get; set; }
        
        public string? Notes { get; set; }
        
        public int? UserId { get; set; }
        public User? User { get; set; }
        
        public int? WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }

        public List<PurchaseItem> PurchaseItems { get; set; } = new();
        public virtual List<PurchaseReturn> PurchaseReturns { get; set; } = new();

        [NotMapped]
        public OrderDepositInfo? DepositInfo => OrderDepositHelper.ParseDepositInfo(Notes);

        [NotMapped]
        public bool IsDepositPayment => PaymentMethod == PaymentMethod.MobilePayment || DepositInfo != null;

        [NotMapped]
        public bool IsCreditPayment => PaymentMethod == PaymentMethod.OnCredit;

        [NotMapped]
        public bool HasVoucher => !IsCreditPayment;

        [NotMapped]
        public string? VoucherNumber
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Notes) || !Notes.Contains("[سند صرف:")) return null;
                int start = Notes.IndexOf("[سند صرف:", StringComparison.Ordinal);
                int end = Notes.IndexOf(']', start);
                if (end == -1) end = Notes.Length;
                string tag = Notes.Substring(start, end - start);
                return tag.Replace("[سند صرف:", "").Trim();
            }
        }

        [NotMapped]
        public string PaymentMethodDisplay
        {
            get
            {
                if (Notes?.Contains("طريقة الدفع: سند صرف") == true)
                {
                    var dep = DepositInfo;
                    if (dep != null && !string.IsNullOrWhiteSpace(dep.BankOrWallet))
                    {
                        return $"سند صرف ({dep.BankOrWallet})";
                    }
                    return "سند صرف";
                }

                if (PaymentMethod == PaymentMethod.MobilePayment || Notes?.Contains("[إيداع:") == true)
                {
                    var dep = DepositInfo;
                    if (dep != null && !string.IsNullOrWhiteSpace(dep.BankOrWallet))
                    {
                        return $"إيداع ({dep.BankOrWallet})";
                    }
                    return "إيداع / محفظة";
                }

                return PaymentMethod switch
                {
                    PaymentMethod.Cash => "نقدي (كاش)",
                    PaymentMethod.OnCredit => "آجل (دين)",
                    PaymentMethod.DebitCard or PaymentMethod.CreditCard => "شبكة (بطاقة)",
                    PaymentMethod.BankTransfer => "تحويل بنكي",
                    _ => "نقدي"
                };
            }
        }
    }
}
