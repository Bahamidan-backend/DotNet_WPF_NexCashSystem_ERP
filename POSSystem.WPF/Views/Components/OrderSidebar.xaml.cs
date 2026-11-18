using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using POSSystem.WPF.Models;
using POSSystem.WPF.ViewModels;

namespace POSSystem.WPF.Views.Components
{
    public partial class OrderSidebar : UserControl
    {
        public event RoutedEventHandler? LeftoverDisposalClick;

        public OrderSidebar()
        {
            InitializeComponent();
        }

        private void OrderTypeButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset all order type buttons to normal style
            DineInButton.Style = FindResource("OrderTypeButtonStyle") as Style;
            TakeAwayButton.Style = FindResource("OrderTypeButtonStyle") as Style;
            DeliveryButton.Style = FindResource("OrderTypeButtonStyle") as Style;

            // Set clicked button to active style
            if (sender is Button clickedButton)
            {
                clickedButton.Style = FindResource("ActiveOrderTypeButtonStyle") as Style;
            }
        }

        private void LeftoverDisposalButton_Click(object sender, RoutedEventArgs e)
        {
            if (LeftoverDisposalClick != null)
            {
                LeftoverDisposalClick.Invoke(sender, e);
            }
            else if (Window.GetWindow(this) is POSSystem.WPF.Views.CashierWindow cashierWindow)
            {
                cashierWindow.OpenLeftoverDisposalPopup();
            }
        }

        private void BillTypeButton_Click(object sender, RoutedEventArgs e)
        {
            // Styles are now handled via DataTriggers in XAML bound to CashierProberty flags
        }

        private bool _isUpdatingCartQtyBox = false;

        private static string NormalizeArabicDigits(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var chars = input.Trim().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] >= '\u0660' && chars[i] <= '\u0669')
                    chars[i] = (char)('0' + (chars[i] - '\u0660'));
                else if (chars[i] >= '\u06F0' && chars[i] <= '\u06F9')
                    chars[i] = (char)('0' + (chars[i] - '\u06F0'));
            }
            return new string(chars);
        }

        private void CartQuantityTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.SelectAll();
            }
        }

        private void CartQuantityTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb && !tb.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                tb.Focus();
                tb.SelectAll();
            }
        }

        private void CartQuantityTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var norm = NormalizeArabicDigits(e.Text);
            if (string.IsNullOrEmpty(norm) || !norm.All(char.IsDigit))
            {
                e.Handled = true;
            }
        }

        private void CartQuantityTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox tb)
            {
                e.Handled = true;
                Keyboard.ClearFocus();
            }
        }

        private void CartQuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingCartQtyBox) return;
            if (sender is not TextBox tb || tb.Tag is not OrderItem item) return;
            if (!tb.IsKeyboardFocusWithin) return;
            if (DataContext is not POSSystem.WPF.ViewModels.Cashier.CashierViewModel vm) return;

            var norm = NormalizeArabicDigits(tb.Text);
            if (string.IsNullOrWhiteSpace(norm))
                return;

            if (!int.TryParse(norm, out int desiredQty) || desiredQty <= 0)
                return;

            _isUpdatingCartQtyBox = true;
            try
            {
                int applied = vm.SetDirectOrderItemQuantity(item, desiredQty, out bool wasCapped, out int maxAvailable);
                if (wasCapped)
                {
                    tb.Text = applied.ToString();
                    tb.CaretIndex = tb.Text.Length;
                    MessageBox.Show(
                        $"عذراً، الكمية المدخلة ({desiredQty}) أكبر من المتوفر في المخزون!\nأقصى كمية متاحة من '{item.Name}' هي ({maxAvailable}).",
                        "تنبيه - تجاوز الكمية المتاحة",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            finally
            {
                _isUpdatingCartQtyBox = false;
            }
        }

        private void CartQuantityTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingCartQtyBox) return;
            if (sender is not TextBox tb || tb.Tag is not OrderItem item) return;

            var norm = NormalizeArabicDigits(tb.Text);
            if (!int.TryParse(norm, out int desiredQty) || desiredQty <= 0)
            {
                _isUpdatingCartQtyBox = true;
                try
                {
                    tb.Text = Math.Max(1, item.Quantity).ToString();
                }
                finally
                {
                    _isUpdatingCartQtyBox = false;
                }
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
