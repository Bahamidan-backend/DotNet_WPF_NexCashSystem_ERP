
using System.Windows.Controls;
using POSSystem.WPF.ViewModels.Employees;
using POSSystem.WPF.ViewModels;

namespace POSSystem.WPF.Views.AdminPages
{
    public partial class EmployeeManagementView : UserControl
    {
        public EmployeeManagementView()
        {
            InitializeComponent();
            DataContext = new EmployeeManagementViewModel();
        }
    }
}

