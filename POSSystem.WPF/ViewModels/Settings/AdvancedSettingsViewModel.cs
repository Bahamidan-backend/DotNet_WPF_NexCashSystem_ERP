using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using POSSystem.WPF.Models;
using POSSystem.WPF.Shered.Proberty;

namespace POSSystem.WPF.ViewModels.Settings
{
    public partial class AdvancedSettingsViewModel : BaseViewModel
    {
        public AdvancedSettingsViewModel()
        {
            var sp = POSSystem.WPF.App.ServiceProvider;
            _userService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IUserService>(sp);
            advancedSettingsProberty = new AdvancedSettingsProberty();
            LoadUserData();
        }

        private void LoadUserData()
        {
            try
            {
                _currentUser = new User
                {
                    Id = 1,
                    Name = "مدير النظام",
                    Email = "admin@pos.com",
                    Role = UserRole.Admin,
                    IsActive = true
                };

                if (_currentUser != null)
                {
                    advancedSettingsProberty.FullName = _currentUser.Name ?? string.Empty;
                    advancedSettingsProberty.Email = _currentUser.Email ?? string.Empty;
                    advancedSettingsProberty.Phone = string.Empty;
                    advancedSettingsProberty.Role = _currentUser.Role.ToString();
                    advancedSettingsProberty.Department = string.Empty;
                }

                LoadSystemImage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading user data: {ex.Message}");
            }
        }

        private void LoadSystemImage()
        {
            try
            {
                string imagePath = GetSystemImagePath();
                if (File.Exists(imagePath))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath);
                    bitmap.DecodePixelWidth = 100;
                    bitmap.EndInit();
                    advancedSettingsProberty.CurrentSystemImage = bitmap;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading system image: {ex.Message}");
            }
        }

        public async Task SavePersonalInformation()
        {
            await _userService.SavePersonalInformation(_currentUser, advancedSettingsProberty.FullName, advancedSettingsProberty.Email);
        }

        public async Task<bool> ChangePassword(string currentPassword, string newPassword)
        {
            return await _userService.Changepassword(_currentUser, currentPassword, newPassword);
        }

        public void SaveSystemImage()
        {
            try
            {
                if (!string.IsNullOrEmpty(advancedSettingsProberty.SelectedImagePath) && File.Exists(advancedSettingsProberty.SelectedImagePath))
                {
                    string destinationPath = GetSystemImagePath();
                    string destinationDir = Path.GetDirectoryName(destinationPath);

                    if (!Directory.Exists(destinationDir))
                    {
                        Directory.CreateDirectory(destinationDir);
                    }

                    File.Copy(advancedSettingsProberty.SelectedImagePath, destinationPath, true);
                    LoadSystemImage();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"فشل في حفظ صورة النظام: {ex.Message}");
            }
        }

        public void RemoveSystemImage()
        {
            try
            {
                string imagePath = GetSystemImagePath();
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                    advancedSettingsProberty.CurrentSystemImage = null;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"فشل في إزالة صورة النظام: {ex.Message}");
            }
        }

        private string GetSystemImagePath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "POSSystem");
            return Path.Combine(appFolder, "system_logo.png");
        }
    }
}
