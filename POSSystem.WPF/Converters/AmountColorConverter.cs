using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace POSSystem.WPF.Converters
{
    public class AmountColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal amount)
            {
                if (amount < 0)
                    return Brushes.Red; // Negative amount (underpayment)
                if (amount > 0)
                    return Brushes.Green; // Positive amount (change due)
            }
            return Brushes.Black; // Zero or invalid
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
