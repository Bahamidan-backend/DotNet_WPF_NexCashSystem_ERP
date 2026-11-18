using System;
using System.Globalization;
using System.Windows.Data;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Converters
{
    public class CategoryToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ProductCategory category)
            {
                return category switch
                {
                    ProductCategory.Food => "🍽️",
                    ProductCategory.Drink => "🥤",
                    ProductCategory.Dessert => "🍰",
                    ProductCategory.Appetizer => "🥗",
                    _ => "🍽️"
                };
            }
            return "🍽️";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
