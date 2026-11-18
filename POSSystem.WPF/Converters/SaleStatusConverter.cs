using System;
using System.Globalization;
using System.Windows.Data;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Converters
{
    public class SaleStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SaleStatus status)
            {
                return status switch
                {
                    SaleStatus.Completed => "مكتمل",
                    SaleStatus.Pending => "معلق",
                    SaleStatus.Cancelled => "ملغي",
                    SaleStatus.Refunded => "مسترد",
                    _ => "غير محدد"
                };
            }
            return "غير محدد";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string statusText)
            {
                return statusText switch
                {
                    "مكتمل" => SaleStatus.Completed,
                    "معلق" => SaleStatus.Pending,
                    "ملغي" => SaleStatus.Cancelled,
                    "مسترد" => SaleStatus.Refunded,
                    _ => SaleStatus.Pending
                };
            }
            return SaleStatus.Pending;
        }
    }
}
