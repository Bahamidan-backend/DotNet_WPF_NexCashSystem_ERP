using System.Windows.Controls;
using POSSystem.WPF.Models;
using POSSystem.WPF.ViewModels.Purchases;
using POSSystem.WPF.ViewModels.Returns;

namespace POSSystem.WPF.Views.AdminPages
{
    public partial class ReturnsManagementView : UserControl, POSSystem.WPF.Services.ISearchable
    {
        public ReturnsManagementView()
        {
            InitializeComponent();
        }

        public ReturnsManagementView(User currentUser, string? invoiceNumber = null) : this()
        {
            var returnViewModel = new ReturnOrderViewModel(currentUser);
            CustomerReturnsView.DataContext = returnViewModel;
            SupplierReturnsView.DataContext = new PurchaseReturnViewModel(currentUser);

            if (!string.IsNullOrWhiteSpace(invoiceNumber))
                returnViewModel.InitializeWithInvoice(invoiceNumber);

            this.Unloaded += ReturnsManagementView_Unloaded;
        }

        public void ApplySearch(string query)
        {
            if (CustomerReturnsView.DataContext is POSSystem.WPF.Services.ISearchable s1)
                s1.ApplySearch(query);
            if (SupplierReturnsView.DataContext is POSSystem.WPF.Services.ISearchable s2)
                s2.ApplySearch(query);
        }

        private void ReturnsManagementView_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (CustomerReturnsView.DataContext is System.IDisposable customerDisposable)
                customerDisposable.Dispose();
                
            if (SupplierReturnsView.DataContext is System.IDisposable supplierDisposable)
                supplierDisposable.Dispose();
        }
    }
}
