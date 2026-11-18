
using System.Windows;
using System.Windows.Controls;
using POSSystem.WPF.ViewModels.Settings;

namespace POSSystem.WPF.Views.Components
{
    public partial class SettingsView : UserControl
    {
        public string SystemVersion { get; set; } = "1.0.0";
        public string LastUpdate { get; set; } = "2024-01-01";
        public string DatabasePath { get; set; } = "pos.db";
        public string BackupPath { get; set; } = "backup/";
        public string ExportPath { get; set; } = "export/";

        public SettingsView( )
        {
            
            InitializeComponent();
            DataContext = this;
        }

        public event RoutedEventHandler? BackToProductsClick;
        public event RoutedEventHandler? SaveSettingsClick;
        public event RoutedEventHandler? RestartSystemClick;
        public event RoutedEventHandler? ClearDataClick;
        public event RoutedEventHandler? ExportDataClick;

        private void BackToProducts_Click(object sender, RoutedEventArgs e)
        {
            BackToProductsClick?.Invoke(sender, e);
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm)
            {
                vm.CurrentPassword = CurrentPasswordBox.Password;
                vm.NewPassword = NewPasswordBox.Password;
            }

            SaveSettingsClick?.Invoke(sender, e);
            
            // Clear passwords from view
            CurrentPasswordBox.Password = "";
            NewPasswordBox.Password = "";
        }

        private void RestartSystem_Click(object sender, RoutedEventArgs e)
        {
            RestartSystemClick?.Invoke(sender, e);
        }

        private void ClearData_Click(object sender, RoutedEventArgs e)
        {
            ClearDataClick?.Invoke(sender, e);
        }

        private void ExportData_Click(object sender, RoutedEventArgs e)
        {
            ExportDataClick?.Invoke(sender, e);
        }
    }
}
