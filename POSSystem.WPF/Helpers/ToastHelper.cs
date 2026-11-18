using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace POSSystem.WPF.Helpers
{
    public static class ToastHelper
    {
        public static void ShowWarning(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = Brushes.Transparent,
                    Topmost = true,
                    ShowInTaskbar = false,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ResizeMode = ResizeMode.NoResize
                };

                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Orange Warning
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(20, 10, 20, 10),
                    Margin = new Thickness(10)
                };

                // Add drop shadow
                border.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 320,
                    ShadowDepth = 2,
                    Opacity = 0.3,
                    BlurRadius = 10
                };

                var textBlock = new TextBlock
                {
                    Text = message,
                    Foreground = Brushes.White,
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    FlowDirection = FlowDirection.RightToLeft,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 400
                };

                border.Child = textBlock;
                window.Content = border;

                // Position bottom right
                window.Loaded += (s, e) =>
                {
                    var desktopWorkingArea = SystemParameters.WorkArea;
                    window.Left = desktopWorkingArea.Right - window.ActualWidth - 20;
                    window.Top = desktopWorkingArea.Bottom - window.ActualHeight - 20;

                    // Fade out animation
                    var animation = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 0.0,
                        Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                        BeginTime = TimeSpan.FromSeconds(5) // Show for 5 seconds
                    };

                    animation.Completed += (sender, args) => window.Close();
                    window.BeginAnimation(UIElement.OpacityProperty, animation);
                };

                window.Show();
            });
        }
        
        public static void ShowSuccess(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = Brushes.Transparent,
                    Topmost = true,
                    ShowInTaskbar = false,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ResizeMode = ResizeMode.NoResize
                };

                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Green Success
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(20, 10, 20, 10),
                    Margin = new Thickness(10)
                };

                border.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 320,
                    ShadowDepth = 2,
                    Opacity = 0.3,
                    BlurRadius = 10
                };

                var textBlock = new TextBlock
                {
                    Text = message,
                    Foreground = Brushes.White,
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    FlowDirection = FlowDirection.RightToLeft,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 400
                };

                border.Child = textBlock;
                window.Content = border;

                window.Loaded += (s, e) =>
                {
                    var desktopWorkingArea = SystemParameters.WorkArea;
                    window.Left = desktopWorkingArea.Right - window.ActualWidth - 20;
                    window.Top = desktopWorkingArea.Bottom - window.ActualHeight - 20;

                    var animation = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 0.0,
                        Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                        BeginTime = TimeSpan.FromSeconds(3)
                    };

                    animation.Completed += (sender, args) => window.Close();
                    window.BeginAnimation(UIElement.OpacityProperty, animation);
                };

                window.Show();
            });
        }
    }
}
