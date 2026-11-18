using POSSystem.WPF.Helpers;
using System.Windows.Input;
using POSSystem.WPF.Shered.Commands;

namespace POSSystem.WPF.ViewModels.Settings
{
    public partial class SettingsViewModel
    {
        public SettingsCommands Commands { get; }

        public class SettingsCommands
        {
            public ICommand SaveSettingsCommand { get; }
            public ICommand ResetSettingsCommand { get; }
            public ICommand BackupDataCommand { get; }
            public ICommand RestoreDataCommand { get; }
            public ICommand BrowseBackupLocationCommand { get; }
            public ICommand TestPrinterCommand { get; }
            public ICommand UpdateExchangeRateCommand { get; }
            public ICommand RestartSystemCommand { get; }
            public ICommand ClearDataCommand { get; }
            public ICommand ExportDataCommand { get; }
            public ICommand RenewLicenseCommand { get; }
            public ICommand SelectLogoCommand { get; }
            public ICommand CheckForUpdateCommand { get; }
            public ICommand DownloadUpdateCommand { get; }
            public ICommand OpenLogsCommand { get; }

            public SettingsCommands(SettingsViewModel vm)
            {
                SelectLogoCommand = new RelayCommand(() => vm.SelectLogo());
                SaveSettingsCommand = new RelayCommand(async () => 
                {
                    vm.SaveSettings();
                    await vm.UpdateProfileAsync();
                });
                ResetSettingsCommand = new RelayCommand(() => vm.ResetSettings());
                BackupDataCommand = new RelayCommand(() => vm.BackupData());
                RestoreDataCommand = new RelayCommand(() => vm.RestoreData());
                BrowseBackupLocationCommand = new RelayCommand(() => vm.BrowseBackupLocation());
                TestPrinterCommand = new RelayCommand(() => vm.TestPrinter());
                UpdateExchangeRateCommand = new RelayCommand(() => vm.UpdateExchangeRate());
                RestartSystemCommand = new RelayCommand(() => vm.RestartSystem());
                ClearDataCommand = new RelayCommand(() => vm.ClearData());
                ExportDataCommand = new RelayCommand(() => vm.ExportData());
                RenewLicenseCommand = new RelayCommand(() => vm.ActivateLicense());
                CheckForUpdateCommand = new RelayCommand(() => vm.CheckForUpdateAsync().SafeFireAndForget(), () => !vm.IsCheckingUpdate && !vm.IsDownloadingUpdate);
                DownloadUpdateCommand = new RelayCommand(() => vm.DownloadAndApplyUpdateAsync().SafeFireAndForget(), () => vm.IsUpdateAvailable && !vm.IsDownloadingUpdate);
                OpenLogsCommand = new RelayCommand(() => POSSystem.WPF.Services.Logging.AppLogger.OpenLogFolder());
            }
        }
    }
}

