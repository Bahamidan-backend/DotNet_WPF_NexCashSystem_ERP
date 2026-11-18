using POSSystem.WPF.Models;
using POSSystem.WPF.Shered.Proberty;
using System.Windows.Input;

namespace POSSystem.WPF.ViewModels.Settings
{
    public partial class AdvancedSettingsViewModel
    {
        private readonly IUserService _userService;
        private User? _currentUser;
        public AdvancedSettingsProberty advancedSettingsProberty { get; }

        private ICommand? _saveSettingsCommand;
        private ICommand? _changePasswordCommand;
        private ICommand? _changeImageCommand;
        private ICommand? _removeImageCommand;
        private ICommand? _cancelCommand;
    }
}
