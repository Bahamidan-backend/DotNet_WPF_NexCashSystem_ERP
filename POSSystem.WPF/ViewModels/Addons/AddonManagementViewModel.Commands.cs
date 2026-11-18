using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;
using POSSystem.WPF.Views.Popups;
using System.Windows.Input;
using System;
using System.Windows;
using System.Linq;

namespace POSSystem.WPF.ViewModels.Addons
{
    public partial class AddonManagementViewModel
    {
        public ICommand AddAddonCommand => _addAddonCommand ??= new RelayCommand(() => AddAddonCommand_Execute());
        public ICommand EditAddonCommand => _editAddonCommand ??= new RelayCommand<object>((parameter) => EditAddonCommand_Execute(parameter));
        public ICommand DeleteAddonCommand => _deleteAddonCommand ??= new RelayCommand<object>((parameter) => DeleteAddonCommand_Execute(parameter));
        public ICommand RefreshCommand => _refreshCommand ??= new RelayCommand(() => RefreshCommand_Execute());

        private void AddAddonCommand_Execute()
        {
            try
            {
                var addAddonPopup = new AddAddonPopup(this);
                var owner = Application.Current.MainWindow;
                if (owner != null && owner != addAddonPopup && owner.IsVisible)
                {
                    addAddonPopup.Owner = owner;
                }
                
                addAddonPopup.ShowDialog();
                POSSystem.WPF.Helpers.TaskExtensions.RunSafe(async () => { await LoadAddonsAsync(); await LoadStatisticsAsync(); });
                ServiceLocator.NotifyAddonsChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح نافذة إضافة الإضافة: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditAddonCommand_Execute(object? parameter = null)
        {
            var addon = parameter as Addon ?? addonManagement.SelectedAddon;
            
            if (addon != null)
            {
                try
                {
                    var editAddonPopup = new EditAddonPopup(this, addon);
                    var owner = Application.Current.MainWindow;
                    if (owner != null && owner != editAddonPopup && owner.IsVisible)
                    {
                        editAddonPopup.Owner = owner;
                    }
                    editAddonPopup.ShowDialog();
                    
                    POSSystem.WPF.Helpers.TaskExtensions.RunSafe(async () => { await LoadAddonsAsync(); await LoadStatisticsAsync(); });
                    ServiceLocator.NotifyAddonsChanged();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في فتح نافذة تعديل الإضافة: {ex.Message}", "خطأ", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void DeleteAddonCommand_Execute(object? parameter = null)
        {
            var addon = parameter as Addon ?? addonManagement.SelectedAddon;
            if (addon == null) return;

            if (addon.Quantity > 0 && !addon.IsInHouse)
            {
                MessageBox.Show(
                    "لا يمكن حذف هذه الإضافة لوجود كمية في المخزون (وهي مشتراة من مورد).\nيجب عمل مرتجع مشتريات أولاً لتصفير الكمية قبل التمكن من حذفها.", 
                    "تنبيه - منع الحذف", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"هل أنت متأكد من حذف الإضافة '{addon.Name}'؟", 
                "تأكيد الحذف", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _addonService.DeleteAddonAsync(addon.Id);
                    
                    MessageBox.Show("تم حذف الإضافة بنجاح!", "تم الحذف", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    addonManagement.Addons.Remove(addon);
                    LoadStatisticsAsync().SafeFireAndForget();
                    ServiceLocator.NotifyAddonsChanged();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"حدث خطأ أثناء حذف الإضافة: {ex.Message}", "خطأ", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RefreshCommand_Execute()
        {
             InitializeAsync().SafeFireAndForget();
        }
    }
}
