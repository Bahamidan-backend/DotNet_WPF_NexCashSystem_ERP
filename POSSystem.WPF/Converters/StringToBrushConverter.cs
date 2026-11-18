using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace POSSystem.WPF.Converters
{
    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorString && !string.IsNullOrEmpty(colorString))
            {
                try
                {
                    return (Brush)new BrushConverter().ConvertFromString(colorString);
                }
                catch
                {
                    return Brushes.Transparent;
                }
            }
            // If value is null, return a default readable color (e.g. gray) instead of Transparent
            return (Brush)new BrushConverter().ConvertFromString("#f8f9fa");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
