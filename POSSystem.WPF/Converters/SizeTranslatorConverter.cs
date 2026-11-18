using System;
using System.Globalization;
using System.Windows.Data;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Converters
{
    public class SizeTranslatorConverter : IValueConverter
    {
        public static string TranslateSize(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "حبة / نفر";
            var conv = new SizeTranslatorConverter();
            return conv.Convert(raw, typeof(string), null!, System.Globalization.CultureInfo.CurrentCulture)?.ToString() ?? raw;
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            if (value is Recipe.PortionUnit portionUnit)
            {
                return PortionUnitHelper.GetDisplayName(portionUnit);
            }

            string size = value.ToString()?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(size)) return string.Empty;

            return size.ToLowerInvariant() switch
            {
                "bag" => "كيس",
                "bundle" => "شدة",
                "carton" => "كرتون",
                "box" => "صندوق",
                "piece" => "حبة",
                "p" => "حبة",
                "kg" => "كجم",
                "carcass" => "ذبيحة",
                "gallon" => "جالون",
                "portion" => "نفر",
                "نفر" => "نفر",
                "eighth" => "ثمن",
                "gram" => "جرام",
                "liter" => "لتر",
                "milliliter" => "مل",
                "single" => "نفر",
                "small" => "صغير",
                "medium" => "وسط",
                "large" => "كبير",
                _ => size
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Recipe.PortionUnit.Piece;
            return PortionUnitHelper.FromDisplayName(value.ToString());
        }
    }
}
