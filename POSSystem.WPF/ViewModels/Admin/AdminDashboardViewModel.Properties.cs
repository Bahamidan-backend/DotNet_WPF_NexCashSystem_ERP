using POSSystem.WPF.Models;
using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.Shered.Commands;

namespace POSSystem.WPF.ViewModels.Admin
{
    public partial class AdminDashboardViewModel
    {
        private readonly User _currentUser;
        private double _sidebarWidth = 280;
        private string _searchText = "";
        private bool _isSidebarVisible = true;
        private SettingsProberty _settings;

        public SettingsProberty Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                OnPropertyChanged(nameof(Settings));
            }
        }

        public AdminDashboardCommand adminDashboardCommand { get; }

        public string WelcomeMessage => !string.IsNullOrWhiteSpace(_settings?.CompanyName) ? _settings.CompanyName : $"Welcome, {_currentUser.Name}!";
        public string UserRole => $"Role: {_currentUser.Role}";

        public double SidebarWidth
        {
            get => _sidebarWidth;
            set
            {
                if (_sidebarWidth != value)
                {
                    _sidebarWidth = value;
                    OnPropertyChanged(nameof(SidebarWidth));
                }
            }
        }

        public event System.Action<string>? SearchTextChanged;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    SearchTextChanged?.Invoke(value);
                }
            }
        }

        public bool IsSidebarVisible
        {
            get => _isSidebarVisible;
            set
            {
                if (_isSidebarVisible != value)
                {
                    _isSidebarVisible = value;
                    OnPropertyChanged(nameof(IsSidebarVisible));
                }
            }
        }
    }
}
