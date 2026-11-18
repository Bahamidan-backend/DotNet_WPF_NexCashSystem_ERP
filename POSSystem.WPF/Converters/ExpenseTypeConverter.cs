using System.Globalization;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Converters
{
    public class ExpenseTypeConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ExpenseType type)
            {
                return type switch
                {
                    ExpenseType.Monthly => "شهري",
                    ExpenseType.Weekly => "أسبوعي",
                    ExpenseType.Daily => "يومي",
                    ExpenseType.OneTime => "مرة واحدة",
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
                    "شهري" => ExpenseType.Monthly,
                    "أسبوعي" => ExpenseType.Weekly,
                    "يومي" => ExpenseType.Daily,
                    "مرة واحدة" => ExpenseType.OneTime,
                    _ => ExpenseType.Monthly
                };
            }
            return ExpenseType.Monthly;
        }
    }
}



