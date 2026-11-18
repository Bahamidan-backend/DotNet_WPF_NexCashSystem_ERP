using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSSystem.WPF.Models
{
    public class Discount : BaseModel
    {
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Column(TypeName = "decimal(5,2)")]
        public decimal Percentage { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(30);

        [NotMapped]
        public string Status
        {
            get
            {
                if (!IsActive) return "Inactive";
                if (DateTime.Now < StartDate) return "Pending";
                if (DateTime.Now > EndDate) return "Expired";
                return "Active";
            }
        }

        [NotMapped]
        public string StatusDisplay => Status switch
        {
            "Active" => "نشط",
            "Pending" => "قادم",
            "Expired" => "منتهي",
            _ => "معطل"
        };

        [NotMapped]
        public string DisplayLabel
        {
            get
            {
                string title = !string.IsNullOrWhiteSpace(Name) ? Name : Code;
                decimal pct = Percentage > 0 ? Percentage : DiscountPercentage;
                decimal amt = Amount > 0 ? Amount : DiscountAmount;
                if (pct > 0)
                    return $"{title} ({Code}) — خصم {pct:0.##}%";
                if (amt > 0)
                    return $"{title} ({Code}) — خصم {amt:N0} ريال";
                return $"{title} ({Code})";
            }
        }
    }
}
