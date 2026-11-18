using System.Windows.Controls;
using POSSystem.WPF.ViewModels.Suppliers;

namespace POSSystem.WPF.Views.AdminPages
{
    public partial class SupplierManagementView : UserControl
    {
        public SupplierManagementView()
        {
            InitializeComponent();
            DataContext = new SupplierManagementViewModel();
        }
    }
}
