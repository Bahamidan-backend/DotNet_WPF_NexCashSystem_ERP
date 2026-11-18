using POSSystem.WPF.ViewModels.Settings;
using POSSystem.WPF.ViewModels;

namespace POSSystem.WPF.Views.Popups
{
    public partial class AdvancedSettingsPopup : Window
    {
        private AdvancedSettingsViewModel? _viewModel;

        public AdvancedSettingsPopup()
        {
            try
            {
                InitializeComponent();
                _viewModel = new AdvancedSettingsViewModel();
                DataContext = _viewModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تهيئة نافذة الإعدادات المتقدمة: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(_viewModel!.advancedSettingsProberty.FullName))
                {
                    MessageBox.Show("يرجى إدخال الاسم الكامل", "خطأ في التحقق", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    FullNameTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(_viewModel!.advancedSettingsProberty.Email))
                {
                    MessageBox.Show("يرجى إدخال البريد الإلكتروني", "خطأ في التحقق", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    EmailTextBox.Focus();
                    return;
                }

                // Validate email format
                if (!IsValidEmail(_viewModel!.advancedSettingsProberty.Email))
                {
                    MessageBox.Show("يرجى إدخال بريد إلكتروني صحيح", "خطأ في التحقق", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    EmailTextBox.Focus();
                    return;
                }

                // Save personal information
                _ = _viewModel!.SavePersonalInformation();

                MessageBox.Show("تم حفظ المعلومات الشخصية بنجاح!", "تم الحفظ", 
                    MessageBoxButton.OK, MessageBoxImage.Information);

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء حفظ المعلومات: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate password fields
                if (string.IsNullOrWhiteSpace(CurrentPasswordBox.Password))
                {
                    MessageBox.Show("يرجى إدخال كلمة المرور الحالية", "خطأ في التحقق", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    CurrentPasswordBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewPasswordBox.Password))
                {
                    MessageBox.Show("يرجى إدخال كلمة المرور الجديدة", "خطأ في التحقق", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NewPasswordBox.Focus();
                    return;
                }

                if (NewPasswordBox.Password.Length < 6)
                {
                    MessageBox.Show("كلمة المرور الجديدة يجب أن تكون 6 أحرف على الأقل", "خطأ في التحقق", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NewPasswordBox.Focus();
                    return;
                }

                if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
                {
                    MessageBox.Show("كلمة المرور الجديدة وتأكيدها غير متطابقتين", "خطأ في التحقق", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    ConfirmPasswordBox.Focus();
                    return;
                }

                // Change password
                bool success = await _viewModel!.ChangePassword(CurrentPasswordBox.Password, NewPasswordBox.Password);

                if (success)
                {
                    MessageBox.Show("تم تغيير كلمة المرور بنجاح!", "تم التغيير", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Clear password fields
                    CurrentPasswordBox.Password = "";
                    NewPasswordBox.Password = "";
                    ConfirmPasswordBox.Password = "";
                }
                else
                {
                    MessageBox.Show("كلمة المرور الحالية غير صحيحة", "خطأ في التحقق", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    CurrentPasswordBox.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تغيير كلمة المرور: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = "اختيار صورة النظام",
                    Filter = "ملفات الصور|*.jpg;*.jpeg;*.png;*.gif|ملفات JPG|*.jpg;*.jpeg|ملفات PNG|*.png|ملفات GIF|*.gif|جميع الملفات|*.*",
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    
                    // Check file size (2MB limit)
                    FileInfo fileInfo = new FileInfo(filePath);
                    if (fileInfo.Length > 2 * 1024 * 1024) // 2MB
                    {
                        MessageBox.Show("حجم الملف كبير جداً. الحد الأقصى المسموح هو 2 ميجابايت", "خطأ", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Load and display the image
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(filePath);
                    bitmap.DecodePixelWidth = 100; // Resize for preview
                    bitmap.EndInit();

                    CurrentImagePreview.Source = bitmap;
                    _viewModel!.advancedSettingsProberty.SelectedImagePath = filePath;

                    MessageBox.Show("تم اختيار الصورة بنجاح! اضغط 'حفظ التغييرات' لحفظها.", "تم الاختيار", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء اختيار الصورة: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("هل أنت متأكد من إزالة صورة النظام؟", 
                    "تأكيد الإزالة", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    CurrentImagePreview.Source = null;
                    _viewModel!.advancedSettingsProberty.SelectedImagePath = null;
                    _viewModel.RemoveSystemImage();

                    MessageBox.Show("تم إزالة صورة النظام", "تم الإزالة", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء إزالة الصورة: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("هل أنت متأكد من إلغاء التغييرات؟", 
                "تأكيد الإلغاء", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
