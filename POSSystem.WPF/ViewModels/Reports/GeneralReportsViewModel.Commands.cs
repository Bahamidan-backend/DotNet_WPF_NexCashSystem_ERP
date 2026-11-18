using System.Windows.Input;

namespace POSSystem.WPF.ViewModels.Reports
{
    public partial class GeneralReportsViewModel
    {
        public ICommand GenerateReportCommand { get; }
        public ICommand ExportReportCommand { get; }
    }
}
