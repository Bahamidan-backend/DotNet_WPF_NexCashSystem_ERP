
using System.Globalization;
using System.Windows.Data;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Converters
{
    public class AnimalTypeToArabicConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AnimalType type)
            {
                return type switch
                {
                    AnimalType.Sheep => "خروف",
                    AnimalType.Cow => "بقرة",
                    AnimalType.Camel => "جمل",
                    _ => type.ToString()
                };
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
