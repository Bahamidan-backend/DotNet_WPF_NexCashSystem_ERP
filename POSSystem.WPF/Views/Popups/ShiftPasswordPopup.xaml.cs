using System.Windows;
using System.Windows.Input;
using POSSystem.WPF.Services;

namespace POSSystem.WPF.Views.Popups
{
    public partial class ShiftPasswordPopup : Window
    {
        private readonly string _requiredPassword;

        public ShiftPasswordPopup()
        {
            InitializeComponent();
            
            // Focus on password box
            Loaded += (s, e) => PasswordBox.Focus();

            // Load the password from settings
            var settings = SettingsService.LoadSettings();
            _requiredPassword = settings.ShiftPassword;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            VerifyPassword();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                VerifyPassword();
            }
        }

        private void VerifyPassword()
        {
            ErrorMessageText.Visibility = Visibility.Collapsed;
            string enteredPassword = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(enteredPassword))
            {
                ShowError("يرجى إدخال كلمة السر");
                return;
            }

            if (enteredPassword == _requiredPassword)
            {
                // Password is correct
                DialogResult = true;
                Close();
            }
            else
            {
                ShowError("كلمة السر غير صحيحة");
                PasswordBox.Clear();
                PasswordBox.Focus();
            }
        }

        private void ShowError(string message)
        {
            ErrorMessageText.Text = message;
            ErrorMessageText.Visibility = Visibility.Visible;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
