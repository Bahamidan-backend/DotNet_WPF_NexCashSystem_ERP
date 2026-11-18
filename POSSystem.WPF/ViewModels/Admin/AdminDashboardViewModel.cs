using POSSystem.WPF.Models;
using POSSystem.WPF.Services;
using POSSystem.WPF.Shered.Commands;
using POSSystem.WPF.Shered.Proberty;

namespace POSSystem.WPF.ViewModels.Admin
{
    public partial class AdminDashboardViewModel : BaseViewModel
    {
        public AdminDashboardViewModel(User user)
        {
            _currentUser = user;
            adminDashboardCommand = new AdminDashboardCommand();
            
            adminDashboardCommand.ToggleSidebarCommand = new RelayCommand(ToggleSidebar);

            Settings = SettingsService.LoadSettings();
            SettingsService.OnSettingsChanged += ReloadSettings;
        }

        private void ReloadSettings()
        {
            Settings = SettingsService.LoadSettings();
            OnPropertyChanged(nameof(WelcomeMessage));
        }
    }
}
