using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace POSSystem.WPF.Converters
{
    public class StockStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                switch (status)
                {
                    case "Out of Stock":
                    case "نفد المخزون":
                        return new SolidColorBrush(Color.FromRgb(231, 76, 60)); // Red
                    case "Low Stock":
                    case "مخزون منخفض":
                        return new SolidColorBrush(Color.FromRgb(243, 156, 18)); // Orange
                    case "In Stock":
                    case "متوفر":
                        return new SolidColorBrush(Color.FromRgb(39, 174, 96)); // Green
                    default:
                        return new SolidColorBrush(Color.FromRgb(149, 165, 166)); // Gray
                }
            }
            return new SolidColorBrush(Color.FromRgb(149, 165, 166));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
