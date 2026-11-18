using System;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Helpers
{
    /// <summary>
    /// مساعد تحويل وحدات القياس بين الوصفات والمستودع (Unit of Measure - UoM Conversion)
    /// يضمن دقة حسابات المخزون والتكاليف (1 كجم = 1000 جرام، 1 لتر = 1000 مل، 1 ذبيحة = 8 أثمان، 1 كيس افتراضي = 40 كجم، إلخ)
    /// </summary>
    public static class UnitConversionHelper
    {
        public static decimal ConvertQuantity(decimal quantity, Recipe.PortionUnit fromUnit, Recipe.PortionUnit? toUnit)
        {
            if (!toUnit.HasValue || fromUnit == toUnit.Value || quantity == 0)
                return quantity;

            var to = toUnit.Value;

            // 1. تحويلات الوزن والكتلة (Gram <-> Kg <-> Bag)
            if (fromUnit == Recipe.PortionUnit.Gram && to == Recipe.PortionUnit.Kg)
                return quantity / 1000m;

            if (fromUnit == Recipe.PortionUnit.Kg && to == Recipe.PortionUnit.Gram)
                return quantity * 1000m;

            if (fromUnit == Recipe.PortionUnit.Kg && to == Recipe.PortionUnit.Bag)
                return quantity / 40.0m;

            if (fromUnit == Recipe.PortionUnit.Bag && to == Recipe.PortionUnit.Kg)
                return quantity * 40.0m;

            if (fromUnit == Recipe.PortionUnit.Gram && to == Recipe.PortionUnit.Bag)
                return quantity / 40000.0m;

            if (fromUnit == Recipe.PortionUnit.Bag && to == Recipe.PortionUnit.Gram)
                return quantity * 40000.0m;

            // 2. تحويلات الحجم والسوائل (Milliliter <-> Liter <-> Gallon)
            if (fromUnit == Recipe.PortionUnit.Milliliter && to == Recipe.PortionUnit.Liter)
                return quantity / 1000m;

            if (fromUnit == Recipe.PortionUnit.Liter && to == Recipe.PortionUnit.Milliliter)
                return quantity * 1000m;

            // 1 جالون = 4 لترات قياسية في المطابخ الشعبية
            if (fromUnit == Recipe.PortionUnit.Liter && to == Recipe.PortionUnit.Gallon)
                return quantity / 4.0m;

            if (fromUnit == Recipe.PortionUnit.Gallon && to == Recipe.PortionUnit.Liter)
                return quantity * 4.0m;

            if (fromUnit == Recipe.PortionUnit.Milliliter && to == Recipe.PortionUnit.Gallon)
                return quantity / 4000m;

            if (fromUnit == Recipe.PortionUnit.Gallon && to == Recipe.PortionUnit.Milliliter)
                return quantity * 4000m;

            // 3. تحويلات الذبائح وأجزائها (Portion / Eighth <-> Carcass)
            if ((fromUnit == Recipe.PortionUnit.Eighth || fromUnit == Recipe.PortionUnit.Portion) && to == Recipe.PortionUnit.Carcass)
                return quantity / 8.0m;

            if (fromUnit == Recipe.PortionUnit.Carcass && (to == Recipe.PortionUnit.Eighth || to == Recipe.PortionUnit.Portion))
                return quantity * 8.0m;

            if ((fromUnit == Recipe.PortionUnit.Portion && to == Recipe.PortionUnit.Eighth) ||
                (fromUnit == Recipe.PortionUnit.Eighth && to == Recipe.PortionUnit.Portion))
                return quantity;

            return quantity;
        }

        public static decimal GetConversionRate(Recipe.PortionUnit fromUnit, Recipe.PortionUnit? toUnit)
        {
            return ConvertQuantity(1.0m, fromUnit, toUnit);
        }
    }
}
