using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;

namespace POSSystem.WPF.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // If parameter is provided, use it (format: "trueColor|falseColor")
                if (parameter is string paramString && paramString.Contains("|"))
                {
                    var colors = paramString.Split('|');
                    if (colors.Length >= 2)
                    {
                        try
                        {
                            // Handle hex colors (#rrggbb or #aarrggbb)
                            if (colors[0].Trim().StartsWith("#") && colors[1].Trim().StartsWith("#"))
                            {
                                var trueColor = (Color)ColorConverter.ConvertFromString(colors[0].Trim());
                                var falseColor = (Color)ColorConverter.ConvertFromString(colors[1].Trim());
                                return new SolidColorBrush(boolValue ? trueColor : falseColor);
                            }
                        }
                        catch
                        {
                            // Fall back to default if color parsing fails
                        }
                    }
                }
                
                // Default behavior
                return boolValue ? new SolidColorBrush(Color.FromRgb(0, 53, 140)) : new SolidColorBrush(Color.FromRgb(231, 76, 60));
            }
            return new SolidColorBrush(Color.FromRgb(149, 165, 166));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
