using System.ComponentModel.DataAnnotations;

namespace POSSystem.WPF.Models
{
    public class Kitchen : BaseModel
    {
        [Required]
        [MaxLength(100)]
        public string? PrinterName { get; set; }

        public bool IsPrinterEnabled { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
