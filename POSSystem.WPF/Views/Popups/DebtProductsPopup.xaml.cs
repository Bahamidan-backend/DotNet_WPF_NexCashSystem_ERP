using System.Windows;
using POSSystem.WPF.Models;
using System.Linq;

namespace POSSystem.WPF.Views.Popups
{
    public partial class DebtProductsPopup : Window
    {
        public DebtProductsPopup(CustomerDebt debt)
        {
            InitializeComponent();
            
            if (debt?.Order != null)
            {
                SubtitleTextBlock.Text = $"منتجات الطلب رقم: {debt.Order.OrderNumber}";
                ProductsDataGrid.ItemsSource = debt.Order.OrderItems;
                TotalTextBlock.Text = $"{debt.Order.Total:N0} ريال";
            }
            else
            {
                SubtitleTextBlock.Text = "لا توجد تفاصيل منتجات متاحة لهذا الدين";
                TotalTextBlock.Text = $"{debt?.Amount ?? 0:N0} ريال";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
