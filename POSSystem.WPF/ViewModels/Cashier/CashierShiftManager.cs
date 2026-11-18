using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using POSSystem.WPF.Models;
using POSSystem.WPF.StaticFunction;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Shered.Proberty;

namespace POSSystem.WPF.ViewModels.Cashier
{
    public partial class CashierViewModel
    {
        public async Task CheckActiveShiftAsync()
        {
            try { _currentShift = await _shiftService.GetActiveShiftByUserIdAsync(_currentUser.Id); HasActiveShift = _currentShift != null; }
            catch { _currentShift = null; HasActiveShift = false; }
        }

        public void ManageShift()
        {
            bool hasPendingOrders = cashierProberty.OrderItems.Count > 0;
            var shiftPopup = new Views.Popups.ShiftManagementPopup(_currentUser.Id, hasPendingOrders);
            var owner = Application.Current.MainWindow;
            if (owner != null && owner != shiftPopup && owner.IsLoaded && owner.IsVisible) shiftPopup.Owner = owner;
            shiftPopup.ShowDialog();
            CheckActiveShiftAsync().SafeFireAndForget();
        }

        public async void Logout()
        {
            try
            {
                _currentShift = await _shiftService.GetActiveShiftByUserIdAsync(_currentUser.Id);
                if (_currentShift != null && _currentShift.IsActive)
                {
                    if (MessageBox.Show("يوجد مناوبة نشطة. يجب إنهاء المناوبة قبل تسجيل الخروج.\n\nهل تريد إنهاء المناوبة وتسجيل الخروج؟", "تنبيه", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        bool hasPendingOrders = cashierProberty.OrderItems.Count > 0;
                        var shiftPopup = new Views.Popups.ShiftManagementPopup(_currentUser.Id, hasPendingOrders);
                        var owner = Application.Current.MainWindow;
                        if (owner != null && owner != shiftPopup && owner.IsLoaded && owner.IsVisible) shiftPopup.Owner = owner;
                        shiftPopup.ShowDialog();
                        _currentShift = await _shiftService.GetActiveShiftByUserIdAsync(_currentUser.Id);
                        if (_currentShift != null && _currentShift.IsActive) { MessageBox.Show("لم يتم إنهاء المناوبة. لا يمكن تسجيل الخروج.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information); return; }
                    }
                    else return;
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error checking shift status: {ex.Message}"); }
            _navigationService.NavigateToLogin();
        }

    }
}
