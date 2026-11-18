using POSSystem.WPF.StaticFunction;
using POSSystem.WPF.Models; // Assuming Order model is here
using System.Windows;
using System.Windows.Controls;
using System;

namespace POSSystem.WPF.Controls
{
    public partial class OrderDetailsControl : UserControl
    {
        public Order Order { get; private set; }
        public event EventHandler CloseRequested;

        public OrderDetailsControl()
        {
            InitializeComponent();
        }

        public void Initialize(Order order)
        {
            Order = order;
            DataContext = order;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
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
                        
                        CloseRequested?.Invoke(this, EventArgs.Empty);
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
