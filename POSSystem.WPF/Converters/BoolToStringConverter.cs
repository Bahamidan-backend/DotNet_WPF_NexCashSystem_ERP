using System;
using System.Globalization;
using System.Windows.Data;

namespace POSSystem.WPF.Converters
{
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string paramString)
            {
                // Format: "trueValue|falseValue"
                var values = paramString.Split('|');
                if (values.Length >= 2)
                {
                    return boolValue ? values[0].Trim() : values[1].Trim();
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

