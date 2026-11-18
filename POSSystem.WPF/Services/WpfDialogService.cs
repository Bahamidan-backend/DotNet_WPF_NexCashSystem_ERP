using System.Windows;
using POSSystem.WPF.Business.Services.interfase;

namespace POSSystem.WPF.Services
{
    public class WpfDialogService : IDialogService
    {
        public void ShowWarning(string message, string title = "تنبيه")
        {
            Application.Current.Dispatcher.Invoke(() => 
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning));
        }

        public void ShowError(string message, string title = "خطأ")
        {
            Application.Current.Dispatcher.Invoke(() => 
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error));
        }

        public void ShowMessage(string message, string title = "رسالة")
        {
            Application.Current.Dispatcher.Invoke(() => 
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        public bool ShowConfirmation(string message, string title = "تأكيد")
        {
            return Application.Current.Dispatcher.Invoke(() => 
            {
                var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
                return result == MessageBoxResult.Yes;
            });
        }
    }
}
