using System;
using System.Globalization;
using System.Windows.Data;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Converters
{
    public class PortionUnitConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            Recipe.PortionUnit? unit = null;

            if (value is Recipe.PortionUnit u)
            {
                unit = u;
            }
            else if (int.TryParse(value.ToString(), out int intValue))
            {
                unit = (Recipe.PortionUnit)intValue;
            }
            else if (Enum.TryParse<Recipe.PortionUnit>(value.ToString(), true, out var parsedEnum))
            {
                unit = parsedEnum;
            }

            if (unit.HasValue)
            {
                return PortionUnitHelper.GetDisplayName(unit.Value);
            }

            string strValue = value.ToString()?.Trim() ?? string.Empty;
            return PortionUnitHelper.GetDisplayName(PortionUnitHelper.FromDisplayName(strValue));
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return Recipe.PortionUnit.Piece;
            return PortionUnitHelper.FromDisplayName(value.ToString());
        }
    }
}
