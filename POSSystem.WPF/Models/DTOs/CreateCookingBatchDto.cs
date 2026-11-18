using System;
using static POSSystem.WPF.Models.Recipe;

namespace POSSystem.WPF.Models.DTOs
{
    public class CreateCookingBatchDto
    {
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public decimal BatchMultiplier { get; set; } = 1.0m;
        public decimal MainIngredientQuantity { get; set; }
        public PortionUnit PortionUnit { get; set; }
        public decimal ActualPortions { get; set; }
        public int? CookedByUserId { get; set; }
        public int? ShiftId { get; set; }
        public string? Notes { get; set; }
    }
}

