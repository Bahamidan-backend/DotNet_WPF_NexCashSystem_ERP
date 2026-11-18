using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.Services;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.ViewModels.Settings
{
    public partial class SettingsViewModel
    {
        private SettingsProberty _settingsProberty;
        public SettingsProberty settingsProberty 
        { 
            get => _settingsProberty;
            set { _settingsProberty = value; OnPropertyChanged(); }
        }

        private LicenseService _licenseService = new LicenseService();
        private string _licenseKey;
        public string LicenseKey
        {
            get => _licenseKey;
            set { _licenseKey = value; OnPropertyChanged(); }
        }

        public string MachineId => _licenseService.GetMachineId();

        private User _currentUser;
        public string UserName
        {
            get => _currentUser?.Name;
            set { if (_currentUser != null && _currentUser.Name != value) { _currentUser.Name = value; OnPropertyChanged(); } }
        }

        public string UserEmail
        {
            get => _currentUser?.Email;
            set { if (_currentUser != null && _currentUser.Email != value) { _currentUser.Email = value; OnPropertyChanged(); } }
        }

        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }

        // ===== خصائص التحديث الذاتي =====

        private readonly UpdateService _updateService = new UpdateService();
        private UpdateInfo? _latestUpdateInfo;

        private string _currentVersion = "...";
        public string CurrentVersion
        {
            get => _currentVersion;
            set { _currentVersion = value; OnPropertyChanged(); }
        }

        private string _updateStatus = "";
        public string UpdateStatus
        {
            get => _updateStatus;
            set { _updateStatus = value; OnPropertyChanged(); }
        }

        private int _updateProgress;
        public int UpdateProgress
        {
            get => _updateProgress;
            set { _updateProgress = value; OnPropertyChanged(); }
        }

        private bool _isUpdateAvailable;
        public bool IsUpdateAvailable
        {
            get => _isUpdateAvailable;
            set { _isUpdateAvailable = value; OnPropertyChanged(); }
        }

        private bool _isCheckingUpdate;
        public bool IsCheckingUpdate
        {
            get => _isCheckingUpdate;
            set { _isCheckingUpdate = value; OnPropertyChanged(); }
        }

        private bool _isDownloadingUpdate;
        public bool IsDownloadingUpdate
        {
            get => _isDownloadingUpdate;
            set { _isDownloadingUpdate = value; OnPropertyChanged(); }
        }

        private string _updateChangelog = "";
        public string UpdateChangelog
        {
            get => _updateChangelog;
            set { _updateChangelog = value; OnPropertyChanged(); }
        }
    }
}

