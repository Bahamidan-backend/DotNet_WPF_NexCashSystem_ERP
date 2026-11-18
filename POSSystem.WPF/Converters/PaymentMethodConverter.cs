using System;
using System.Globalization;
using System.Windows.Data;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Converters
{
    public class PaymentMethodConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PaymentMethod paymentMethod)
            {
                return paymentMethod switch
                {
                    PaymentMethod.Cash => "نقدي",
                    PaymentMethod.CreditCard => "بطاقة ائتمان",
                    PaymentMethod.DebitCard => "بطاقة خصم",
                    PaymentMethod.MobilePayment => "دفع إلكتروني",
                    _ => "غير محدد"
                };
            }
            return "غير محدد";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string paymentText)
            {
                return paymentText switch
                {
                    "نقدي" => PaymentMethod.Cash,
                    "بطاقة ائتمان" => PaymentMethod.CreditCard,
                    "بطاقة خصم" => PaymentMethod.DebitCard,
                    "دفع إلكتروني" => PaymentMethod.MobilePayment,
                    _ => PaymentMethod.Cash
                };
            }
            return PaymentMethod.Cash;
        }
    }
}
