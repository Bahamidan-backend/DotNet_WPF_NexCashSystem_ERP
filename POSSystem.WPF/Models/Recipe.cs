using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSSystem.WPF.Models
{
    public class Recipe
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public PortionUnit portionUnit { get; set; }
        public decimal Quantity { get; set; }
        
        public bool IsMainIngredient { get; set; } = false;
        
        public ComponentType? AnimalComponentRequirement { get; set; }
        
        [ForeignKey("Product")]
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int? WarehouseFromProductId { get; set; }
        [ForeignKey("WarehouseFromProductId")]
        public virtual WarehouseFromProduct? WarehouseFromProduct { get; set; }

        [NotMapped]
        public string PortionUnitDisplay => PortionUnitHelper.GetDisplayName(portionUnit);

        public enum PortionUnit
        {
            // Standard ASCII Identifiers (Safe from encoding corruption)
            Bag = 1,
            Bundle = 2,
            Carton = 3,
            Box = 4,
            Piece = 5,
            Kg = 6,
            Carcass = 7,
            Gallon = 8,
            Eighth = 9,
            Gram = 10,
            Liter = 11,
            Milliliter = 12,
            Portion = 13,

            // Arabic aliases for backward compatibility across existing source files
            كيس = 1,
            شدة = 2,
            كرتون = 3,
            صندوق = 4,
            حبة = 5,
            كجم = 6,
            ذبيحة = 7,
            جالون = 8,
            ثمن = 9,
            جرام = 10,
            لتر = 11,
            مل = 12,
            نفر = 13
        }
    }

    public static class PortionUnitHelper
    {
        public static readonly Dictionary<Recipe.PortionUnit, string> DisplayNames = new()
        {
            { Recipe.PortionUnit.Bag, "كيس" },
            { Recipe.PortionUnit.Bundle, "شدة" },
            { Recipe.PortionUnit.Carton, "كرتون" },
            { Recipe.PortionUnit.Box, "صندوق" },
            { Recipe.PortionUnit.Piece, "حبة" },
            { Recipe.PortionUnit.Portion, "نفر" },
            { Recipe.PortionUnit.Kg, "كجم" },
            { Recipe.PortionUnit.Carcass, "ذبيحة" },
            { Recipe.PortionUnit.Gallon, "جالون" },
            { Recipe.PortionUnit.Eighth, "ثمن" },
            { Recipe.PortionUnit.Gram, "جرام" },
            { Recipe.PortionUnit.Liter, "لتر" },
            { Recipe.PortionUnit.Milliliter, "مل" }
        };

        public static string GetDisplayName(Recipe.PortionUnit unit)
        {
            return DisplayNames.TryGetValue(unit, out var name) ? name : unit.ToString();
        }

        public static Recipe.PortionUnit FromDisplayName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return Recipe.PortionUnit.Piece;

            foreach (var kvp in DisplayNames)
            {
                if (kvp.Value.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.ToString().Equals(name.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Key;
                }
            }
            return Recipe.PortionUnit.Piece;
        }
    }
}
