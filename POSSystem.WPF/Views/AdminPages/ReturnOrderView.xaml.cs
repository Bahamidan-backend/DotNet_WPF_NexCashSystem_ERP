using System.Windows.Controls;
using POSSystem.WPF.ViewModels.Returns;

namespace POSSystem.WPF.Views.AdminPages
{
    public partial class ReturnOrderView : UserControl
    {
        public ReturnOrderView()
        {
            InitializeComponent();
        }

        public ReturnOrderView(Models.User user) : this()
        {
            DataContext = new ReturnOrderViewModel(user);
        }
    }
}
