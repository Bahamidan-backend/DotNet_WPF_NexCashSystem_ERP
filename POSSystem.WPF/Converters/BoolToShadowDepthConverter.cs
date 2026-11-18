using System;
using System.Globalization;
using System.Windows.Data;

namespace POSSystem.WPF.Converters
{
    public class BoolToShadowDepthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string paramString && paramString.Contains("|"))
            {
                var values = paramString.Split('|');
                if (values.Length >= 2)
                {
                    try
                    {
                        var trueValue = double.Parse(values[0].Trim());
                        var falseValue = double.Parse(values[1].Trim());
                        return boolValue ? trueValue : falseValue;
                    }
                    catch
                    {
                        // Fall back to default if parsing fails
                    }
                }
            }
            return 2.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

