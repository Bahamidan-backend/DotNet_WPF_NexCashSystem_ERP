
namespace POSSystem.WPF.Helpers
{
    /// <summary>
    /// Shared helper for portion size calculations used in OrderService and ProductService.
    /// Centralizes the size-to-multiplier mapping to avoid code duplication (DRY principle).
    /// </summary>
    public static class PortionSizeHelper
    {
        public static Dictionary<Models.Category.PortionSize, string> DisplayNames = new()
        {
            { Models.Category.PortionSize.Single, "نفر" },
            { Models.Category.PortionSize.Piece, "حبة" },
            { Models.Category.PortionSize.Large, "كبير" }
        };

        /// <summary>
        /// Calculates the inventory deduction quantity based on the portion size name.
        /// </summary>
        /// <param name="size">The size string from the OrderItem or ProductSize.</param>
        /// <param name="quantity">The order item quantity.</param>
        /// <returns>The total deduction amount from inventory.</returns>
        public static decimal CalculateDeduction(string? size, decimal quantity)
        {
            var normalizedSize = size?.Trim() ?? string.Empty;
            decimal multiplier = GetSizeMultiplier(normalizedSize);
            return quantity * multiplier;
        }

        /// <summary>
        /// Returns the inventory multiplier for a given size name.
        /// A multiplier of 1.0 means one full unit is deducted per item ordered.
        /// </summary>
        public static decimal GetSizeMultiplier(string sizeName)
        {
            return sizeName switch
            {
                // Full unit sizes
                "نفر"     => 1.0m,
                "حبة"     => 1.0m,
                "ذبيحة"   => 1.0m,

                // Half unit sizes
                "نص نفر"  => 0.5m,
                "نص حبة"  => 0.5m,
                "نص ذبيحة" => 0.5m,

                // One and a half unit sizes
                "نفر ونص" => 1.5m,
                "حبة ونص" => 1.5m,

                // Quarter unit sizes
                "ربع حبة"   => 0.25m,
                "ربع نفر"   => 0.25m,
                "ربع ذبيحة" => 0.25m,

                // Default: treat as a full unit
                _ => 1.0m
            };
        }
    }
}
