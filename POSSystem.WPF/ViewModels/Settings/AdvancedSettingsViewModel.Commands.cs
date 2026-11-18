using System;
using System.Windows.Input;
using Microsoft.Win32;
using System.Diagnostics;

namespace POSSystem.WPF.ViewModels.Settings
{
    public partial class AdvancedSettingsViewModel
    {
        public ICommand SaveSettingsCommand => _saveSettingsCommand ??= new RelayCommand(() => SaveSettingsCommand_Execute(null));
        public ICommand ChangePasswordCommand => _changePasswordCommand ??= new RelayCommand(() => ChangePasswordCommand_Execute(null));
        public ICommand ChangeImageCommand => _changeImageCommand ??= new RelayCommand(() => ChangeImageCommand_Execute(null));
        public ICommand RemoveImageCommand => _removeImageCommand ??= new RelayCommand(() => RemoveImageCommand_Execute(null));
        public ICommand CancelCommand => _cancelCommand ??= new RelayCommand(() => CancelCommand_Execute(null));

        private async void SaveSettingsCommand_Execute(object? parameter)
        {
            try
            {
                await SavePersonalInformation();
                SaveSystemImage();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private async void ChangePasswordCommand_Execute(object? parameter)
        {
            try
            {
                if (!string.IsNullOrEmpty(advancedSettingsProberty.CurrentPassword) && !string.IsNullOrEmpty(advancedSettingsProberty.NewPassword))
                {
                    bool success = await ChangePassword(advancedSettingsProberty.CurrentPassword, advancedSettingsProberty.NewPassword);
                    
                    if (success)
                    {
                        advancedSettingsProberty.CurrentPassword = string.Empty;
                        advancedSettingsProberty.NewPassword = string.Empty;
                        advancedSettingsProberty.ConfirmNewPassword = string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error changing password: {ex.Message}");
            }
        }

        private void ChangeImageCommand_Execute(object? parameter)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "اختر صورة النظام",
                    Filter = "ملفات الصور|*.jpg;*.jpeg;*.png;*.bmp;*.gif|جميع الملفات|*.*",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    advancedSettingsProberty.SelectedImagePath = openFileDialog.FileName;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error selecting image: {ex.Message}");
            }
        }

        private void RemoveImageCommand_Execute(object? parameter)
        {
            try
            {
                RemoveSystemImage();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error removing image: {ex.Message}");
            }
        }

        private void CancelCommand_Execute(object? parameter)
        {
            if (parameter is System.Windows.Window window)
            {
                window.Close();
            }
        }
    }
}
