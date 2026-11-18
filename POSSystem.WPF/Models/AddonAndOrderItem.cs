

namespace POSSystem.WPF.Models
{
    public class AddonAndOrderItem
    {
        [Key]
        public int id { get; set; }
        [ForeignKey("Addon")]
        public int AddonId { get; set; }
        public Addon Addon { get; set; }
        [ForeignKey("orderitem")]
        public int OrderitemID { get; set; }
        public OrderItem orderitem { get; set; }




    }
}
