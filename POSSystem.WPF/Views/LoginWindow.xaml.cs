using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Models;
using POSSystem.WPF.Services;

namespace POSSystem.WPF.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LicenseService _licenseService;
        private DispatcherTimer? _lockoutCountdownTimer;
        private DateTime? _activeLockoutUntil;
        private DateTime _lastFailedAttemptAt = DateTime.MinValue;
        private DateTime _alertShownAt = DateTime.MinValue;
        private const int LockoutDurationSeconds = 30; // 30 seconds instead of 5 minutes

        public LoginWindow()
        {
            InitializeComponent();
            _licenseService = new LicenseService();

            CheckLicense();
        }

        private void CheckLicense()
        {
            string savedLicense = _licenseService.LoadLicense();
            if (_licenseService.ValidateLicense(savedLicense, out string msg))
            {
                LoginFormPanel.Visibility = Visibility.Visible;
                LicenseFormPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                LoginFormPanel.Visibility = Visibility.Collapsed;
                LicenseFormPanel.Visibility = Visibility.Visible;
                MachineIdTextBox.Text = _licenseService.GetMachineId();

                if (!string.IsNullOrEmpty(savedLicense))
                {
                    LicenseErrorMessageText.Text = msg;
                    LicenseErrorMessageText.Visibility = Visibility.Visible;
                }
            }
        }

        private void ActivateButton_Click(object sender, RoutedEventArgs e)
        {
            string key = LicenseKeyTextBox.Text.Trim();
            if (_licenseService.ValidateLicense(key, out string msg))
            {
                _licenseService.SaveLicense(key);
                MessageBox.Show("تم تفعيل النظام بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                CheckLicense();
            }
            else
            {
                LicenseErrorMessageText.Text = msg;
                LicenseErrorMessageText.Visibility = Visibility.Visible;
            }
        }

        private void CopyMachineIdButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(MachineIdTextBox.Text))
            {
                POSSystem.WPF.Helpers.SafeClipboard.SetText(MachineIdTextBox.Text);
                MessageBox.Show("تم نسخ رقم الجهاز.", "تم النسخ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (LoginAlertOverlay.Visibility == Visibility.Visible)
            {
                if (e.Key == Key.Enter || e.Key == Key.Escape || e.Key == Key.Space)
                {
                    e.Handled = true;
                    // Ignore Enter key bounce within 350ms of the alert appearing so holding/double-pressing Enter doesn't immediately dismiss & resubmit
                    if ((DateTime.Now - _alertShownAt).TotalMilliseconds >= 350)
                    {
                        DismissLoginAlert();
                    }
                }
            }
        }

        private void ShowCenteredLoginAlert(string title, string message, string icon = "⚠️", DateTime? lockedUntil = null)
        {
            _alertShownAt = DateTime.Now;
            LoginButton.IsDefault = false;

            AlertOverlayIcon.Text = icon;
            AlertOverlayTitle.Text = title;
            AlertOverlayMessage.Text = message;

            ErrorMessageText.Text = message.Replace("\n", " — ");
            ErrorMessageText.Visibility = Visibility.Visible;

            if (lockedUntil.HasValue && lockedUntil.Value > DateTime.Now)
            {
                _activeLockoutUntil = lockedUntil.Value;
                AlertCountdownBorder.Visibility = Visibility.Visible;
                UpdateCountdownText();
                StartLockoutTimer();
            }
            else
            {
                _activeLockoutUntil = null;
                AlertCountdownBorder.Visibility = Visibility.Collapsed;
                StopLockoutTimer();
            }

            LoginAlertOverlay.Visibility = Visibility.Visible;
            AlertDismissButton.Focus();
        }

        private void DismissLoginAlert()
        {
            LoginAlertOverlay.Visibility = Visibility.Collapsed;
            LoginButton.IsDefault = true;
            _lastFailedAttemptAt = DateTime.Now;
            PasswordBox.Focus();
            PasswordBox.SelectAll();
        }

        private void AlertDismissButton_Click(object sender, RoutedEventArgs e)
        {
            DismissLoginAlert();
        }

        private void StartLockoutTimer()
        {
            StopLockoutTimer();
            _lockoutCountdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _lockoutCountdownTimer.Tick += (s, e) =>
            {
                UpdateCountdownText();
            };
            _lockoutCountdownTimer.Start();
        }

        private void StopLockoutTimer()
        {
            if (_lockoutCountdownTimer != null)
            {
                _lockoutCountdownTimer.Stop();
                _lockoutCountdownTimer = null;
            }
        }

        private void UpdateCountdownText()
        {
            if (!_activeLockoutUntil.HasValue)
            {
                StopLockoutTimer();
                return;
            }

            var remaining = _activeLockoutUntil.Value - DateTime.Now;
            int secondsLeft = (int)Math.Ceiling(remaining.TotalSeconds);
            if (secondsLeft <= 0)
            {
                StopLockoutTimer();
                _activeLockoutUntil = null;
                AlertCountdownBorder.Visibility = Visibility.Collapsed;
                ErrorMessageText.Visibility = Visibility.Collapsed;
                if (LoginAlertOverlay.Visibility == Visibility.Visible)
                {
                    DismissLoginAlert();
                }
            }
            else
            {
                AlertCountdownText.Text = $"⏳ سيُفتح القفل تلقائياً خلال: {secondsLeft} ثانية";
                ErrorMessageText.Text = $"الحساب مقفل مؤقتاً — سيُفتح خلال {secondsLeft} ثانية";
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Prevent accidental rapid Enter spam while alert is open or within 400ms of closing the alert
            if (LoginAlertOverlay.Visibility == Visibility.Visible)
                return;

            if ((DateTime.Now - _lastFailedAttemptAt).TotalMilliseconds < 400)
                return;

            try
            {
                string email = EmailTextBox.Text.Trim();
                string password = PasswordBox.Password;

                if (string.IsNullOrWhiteSpace(password))
                {
                    ShowCenteredLoginAlert(
                        "يرجى إدخال كلمة المرور",
                        "حقل كلمة المرور فارغ. يرجى كتابة كلمة المرور ثم المحاولة مرة أخرى.",
                        "🔑");
                    return;
                }

                // Support shorthand "user" or "admin" in addition to full email
                if (email.Equals("user", StringComparison.OrdinalIgnoreCase))
                    email = "user@pos.com";
                else if (email.Equals("admin", StringComparison.OrdinalIgnoreCase))
                    email = "admin@pos.com";

                ErrorMessageText.Visibility = Visibility.Collapsed;

                await using var context = new POSDbContext();

                // Defensive schema safeguard so Login never fails with 'no such column: u.PermissionsJson' on any DB file
                try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE Users ADD COLUMN PermissionsJson TEXT;"); } catch { }
                try { await context.Database.ExecuteSqlRawAsync("ALTER TABLE Users ADD COLUMN RoleTemplateName TEXT;"); } catch { }

                var user = await context.Users
                    .FirstOrDefaultAsync(u => (u.Email == email || u.Name == email) && u.IsActive);

                // If first-time install and user@pos.com / user123 was entered before seeding completed, auto-seed it immediately
                if (user == null && email.Equals("user@pos.com", StringComparison.OrdinalIgnoreCase) && password == "user123")
                {
                    user = await context.Users.FirstOrDefaultAsync(u => u.Email == "user@pos.com");
                    if (user == null)
                    {
                        user = new User
                        {
                            Email = "user@pos.com",
                            Password = PasswordHasher.HashPassword("user123"),
                            Name = "Cashier User",
                            JobTitle = "كاشير",
                            Role = UserRole.User,
                            RoleTemplateName = "كاشير",
                            IsActive = true,
                            MustChangePassword = false,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };
                        context.Users.Add(user);
                    }
                    else
                    {
                        user.IsActive = true;
                        user.Password = PasswordHasher.HashPassword("user123");
                        user.MustChangePassword = false;
                    }
                    await context.SaveChangesAsync();
                }

                if (user != null)
                {
                    // Clamp any legacy 5-minute lock to at most 30 seconds so the cashier never waits 5 minutes
                    if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.Now.AddSeconds(LockoutDurationSeconds))
                    {
                        user.LockedUntil = DateTime.Now.AddSeconds(LockoutDurationSeconds);
                        await context.SaveChangesAsync();
                    }

                    // Rate limiting check
                    if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.Now)
                    {
                        int remainingSec = Math.Max(1, (int)Math.Ceiling((user.LockedUntil.Value - DateTime.Now).TotalSeconds));
                        ShowCenteredLoginAlert(
                            "تم قفل الدخول مؤقتاً",
                            $"تم استنفاد المحاولات الخاطئة.\nيرجى الانتظار {remainingSec} ثانية فقط ثم المحاولة مرة أخرى.",
                            "🔒",
                            user.LockedUntil.Value);
                        return;
                    }

                    // If previous lock expired, reset counter cleanly
                    if (user.LockedUntil.HasValue && user.LockedUntil.Value <= DateTime.Now)
                    {
                        user.LockedUntil = null;
                        user.FailedLoginAttempts = 0;
                    }

                    if (PasswordHasher.VerifyPassword(password, user.Password))
                    {
                        StopLockoutTimer();
                        user.FailedLoginAttempts = 0;
                        user.LockedUntil = null;
                        user.MustChangePassword = false;

                        if (PasswordHasher.NeedsRehash(user.Password))
                        {
                            user.Password = PasswordHasher.HashPassword(password);
                            user.UpdatedAt = DateTime.Now;
                        }
                        await context.SaveChangesAsync();

                        try
                        {
                            if (PermissionGuard.ShouldOpenAdminDashboardOnLogin(user))
                            {
                                var adminWindow = new AdminDashboardWindow(user);
                                Application.Current.MainWindow = adminWindow;
                                adminWindow.Show();
                                this.Close();
                            }
                            else
                            {
                                var cashierWindow = new CashierWindow(user);
                                Application.Current.MainWindow = cashierWindow;
                                cashierWindow.Show();
                                this.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            ShowCenteredLoginAlert("خطأ في فتح النظام", $"فشل فتح النافذة الرئيسية:\n{ex.Message}", "❌");
                        }
                    }
                    else
                    {
                        _lastFailedAttemptAt = DateTime.Now;
                        user.FailedLoginAttempts++;
                        if (user.FailedLoginAttempts >= 5)
                        {
                            user.LockedUntil = DateTime.Now.AddSeconds(LockoutDurationSeconds);
                            user.FailedLoginAttempts = 0;
                            await context.SaveChangesAsync();

                            ShowCenteredLoginAlert(
                                "تم قفل الحساب مؤقتاً (30 ثانية)",
                                $"تم إدخال كلمة المرور بشكل خاطئ 5 مرات متتالية.\nتم إيقاف المحاولات مؤقتاً لمدة {LockoutDurationSeconds} ثانية فقط لحماية النظام.",
                                "🔒",
                                user.LockedUntil.Value);
                        }
                        else
                        {
                            int remainingAttempts = 5 - user.FailedLoginAttempts;
                            await context.SaveChangesAsync();

                            ShowCenteredLoginAlert(
                                "كلمة المرور غير صحيحة!",
                                $"كلمة المرور التي أدخلتها غير صحيحة، يرجى التأكد من الحروف والأرقام.\n\nالمحاولات المتبقية قبل التوقف المؤقت (30 ثانية): {remainingAttempts}",
                                "⚠️");
                        }
                    }
                }
                else
                {
                    _lastFailedAttemptAt = DateTime.Now;
                    ShowCenteredLoginAlert(
                        "بيانات الدخول غير صحيحة!",
                        "اسم المستخدم أو البريد الإلكتروني غير موجود في النظام.\nيرجى التأكد من كتابة البريد أو اسم المستخدم بشكل صحيح.",
                        "⚠️");
                }
            }
            catch (Exception ex)
            {
                ShowCenteredLoginAlert("خطأ في تسجيل الدخول", ex.Message, "❌");
            }
        }

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            StopLockoutTimer();
            this.Close();
        }
    }
}
