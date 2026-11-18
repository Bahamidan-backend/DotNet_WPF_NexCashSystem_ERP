

using POSSystem.WPF.StaticFunction;

namespace POSSystem.WPF.Views
{
    public partial class OrderDetailsWindow : Window
    {
        public Order Order { get; }

        public OrderDetailsWindow(Order order)
        {
            InitializeComponent();
            Order = order;
            DataContext = order;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ReprintButton_Click(object sender, RoutedEventArgs e)
        {

            _ = Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                GloublStatic.PrintReceipt(Order);
            }));
            MessageBox.Show($" تم إعادة طباعة فاتورة {Order.OrderNumber}", 
                "طباعة", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (Order != null && Order.Status != OrderStatus.Completed && Order.Status != OrderStatus.Cancelled)
            {
                var result = MessageBox.Show(
                    $"هل أنت متأكد من إلغاء الطلب {Order.OrderNumber}؟\nسيتم استرجاع الأصناف للمخزون وعكس حركة الصندوق/الديون تلقائياً.", 
                    "تأكيد الإلغاء", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var orderService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IOrderService>(POSSystem.WPF.App.ServiceProvider);
                        await orderService.CancelOrderAsync(Order.Id, "إلغاء من نافذة تفاصيل الطلب");
                        Order.Status = OrderStatus.Cancelled;
                        Business.ServiceLocator.NotifyInventoryChanged();
                        Business.ServiceLocator.NotifyCashBoxChanged();

                        MessageBox.Show("تم إلغاء الطلب، استرجاع المخزون، وعكس حركة الصندوق بنجاح", 
                            "تم الإلغاء", MessageBoxButton.OK, MessageBoxImage.Information);
                        Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"حدث خطأ أثناء إلغاء الطلب: {ex.Message}", 
                            "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
