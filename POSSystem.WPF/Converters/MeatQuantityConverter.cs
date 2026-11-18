using System.Globalization;
using System.Windows.Data;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Converters
{
    public class MeatQuantityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AnimalBatch batch)
            {
                string unit = batch.IsWeightMode ? "كجم" : "نفر";
                return $"{batch.RemainingWeight} {unit}";
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
