using System;
using System.IO;
using System.Threading.Tasks;

namespace POSSystem.WPF.Helpers
{
    public static class TaskExtensions
    {
        public static void RunSafe(Func<Task> action, bool showToast = true, Action<Exception>? onException = null) => action().SafeFireAndForget(showToast, onException);

        public static void SafeFireAndForget(this Task task, bool showToast = true, Action<Exception>? onException = null)
        {
            task.ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    var exception = t.Exception.GetBaseException();
                    
                    // Call the custom action if provided
                    onException?.Invoke(exception);

                    // Log to a local file
                    try
                    {
                        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ErrorLog.txt");
                        string errorMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error: {exception.Message}\nStackTrace: {exception.StackTrace}\n--------------------------------------------------\n";
                        File.AppendAllText(logPath, errorMessage);
                    }
                    catch { /* Ignore logging errors to prevent crash loop */ }

                    // Show Toast Warning to the user
                    if (showToast)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            ToastHelper.ShowWarning($"حدث خطأ في الخلفية: {exception.Message}");
                        });
                    }
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
