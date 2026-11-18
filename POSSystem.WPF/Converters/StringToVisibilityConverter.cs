using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace POSSystem.WPF.Converters
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string paramValue && paramValue.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
            {
                bool isEmpty = value == null || (value is string s && string.IsNullOrWhiteSpace(s));
                return isEmpty ? Visibility.Visible : Visibility.Collapsed;
            }

            if (value is string stringValue && parameter is string targetParam)
            {
                // Compare the value with the parameter
                return stringValue == targetParam ? Visibility.Visible : Visibility.Collapsed;
            }

            // If no parameter provided, show if string is not empty
            if (value is string str && !string.IsNullOrWhiteSpace(str))
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
