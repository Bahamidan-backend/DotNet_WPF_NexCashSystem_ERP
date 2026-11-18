using System.Globalization;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Converters
{
    public class VoucherTypeConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is VoucherType type)
            {
                return type switch
                {
                    VoucherType.Receipt => "سند قبض",
                    VoucherType.Payment => "سند صرف",
                    _ => "غير محدد"
                };
            }
            return "غير محدد";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return str switch
                {
                    "سند قبض" => VoucherType.Receipt,
                    "سند صرف" => VoucherType.Payment,
                    _ => VoucherType.Receipt
                };
            }
            return VoucherType.Receipt;
        }
    }
}



