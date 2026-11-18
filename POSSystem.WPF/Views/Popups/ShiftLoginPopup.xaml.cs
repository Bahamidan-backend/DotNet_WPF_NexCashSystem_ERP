using System.Windows;
using System.Windows.Input;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;
using Microsoft.Extensions.DependencyInjection;

namespace POSSystem.WPF.Views.Popups
{
    public partial class ShiftLoginPopup : Window
    {
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;
        public User? AuthenticatedUser { get; private set; }
        public bool IsAuthenticated { get; private set; }

        private readonly IServiceScope _scope;

        public ShiftLoginPopup()
        {
            InitializeComponent();
            _scope = App.ServiceProvider.CreateScope();
            var context = _scope.ServiceProvider.GetRequiredService<POSDbContext>();
            _unitOfWork = new UnitOfWork(context);
            _userService = new UserService(_unitOfWork);
            Closed += (_, _) => 
            {
                _unitOfWork.Dispose();
                _scope.Dispose();
            };
            
            // Focus on email textbox
            Loaded += (s, e) => EmailTextBox.Focus();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformLogin();
        }

        private async void EmailTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PasswordBox.Focus();
            }
        }

        private async void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await PerformLogin();
            }
        }

        private async System.Threading.Tasks.Task PerformLogin()
        {
            try
            {
                string usernameOrEmail = EmailTextBox.Text.Trim();
                string password = PasswordBox.Password;

                // Hide error message
                ErrorMessageText.Visibility = Visibility.Collapsed;

                if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
                {
                    ShowError("يرجى إدخال البريد الإلكتروني أو اسم المستخدم وكلمة المرور");
                    return;
                }

                // Try to find user by email first, then by name
                User? user = await _userService.GetUserByEmailAsync(usernameOrEmail);
                
                // If not found by email, try to find by name
                if (user == null)
                {
                    var allUsers = await _userService.GetActiveUsersAsync();
                    user = allUsers.FirstOrDefault(u => u.Name.Equals(usernameOrEmail, StringComparison.OrdinalIgnoreCase) && u.IsActive);
                }

                if (user == null)
                {
                    ShowError("المستخدم غير موجود");
                    return;
                }

                // Validate password
                if (!PasswordHasher.VerifyPassword(password, user.Password))
                {
                    ShowError("بيانات الدخول غير صحيحة");
                    return;
                }

                if (PasswordHasher.NeedsRehash(user.Password))
                {
                    user.Password = PasswordHasher.HashPassword(password);
                    await _userService.UpdateUserAsync(user);
                }

                // Check if user is a cashier (User role)
                if (user.Role != UserRole.User)
                {
                    ShowError("يجب أن يكون المستخدم كاشير لبدء المناوبة");
                    return;
                }

                AuthenticatedUser = user;
                IsAuthenticated = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تسجيل الدخول: {ex.Message}");
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

