using System;
using System.Globalization;
using System.Windows.Data;

namespace POSSystem.WPF.Converters
{
    public class GreaterThanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;

            double val = 0;
            double param = 0;

            try
            {
                val = System.Convert.ToDouble(value);
                param = System.Convert.ToDouble(parameter);
            }
            catch
            {
                return false;
            }

            return val > param;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
