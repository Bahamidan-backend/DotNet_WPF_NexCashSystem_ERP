namespace POSSystem.WPF.Models.DTOs
{
    public class UpdateRecipeDto
    {
        public int ProductId { get; set; }
        public int WarehouseFromProductId { get; set; }
        public decimal NewQty { get; set; }
        public int ExpectedYieldRate { get; set; } // Used for first-time injection to set YieldRate
    }
}
