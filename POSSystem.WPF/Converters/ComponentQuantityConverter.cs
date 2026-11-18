using System.Globalization;
using System.Windows.Data;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Converters
{
    public class ComponentQuantityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AnimalBatch batch && parameter is string component)
            {
                string unit = batch.IsWeightMode ? "كجم" : "حبة";
                
                if (component == "Back")
                    return $"{batch.BackWeight} {unit}";
                else if (component == "Liver")
                    return $"{batch.LiverWeight} {unit}";
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
