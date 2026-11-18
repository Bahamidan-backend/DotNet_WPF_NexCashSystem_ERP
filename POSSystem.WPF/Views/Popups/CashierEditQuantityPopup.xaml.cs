using System;
using System.Windows;
using System.Windows.Controls;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Views.Popups
{
    public partial class CashierEditQuantityPopup : UserControl
    {
        public event EventHandler<CashierEditQuantityEventArgs> SaveRequested;
        public event EventHandler CloseRequested;

        private Product _product;

        public CashierEditQuantityPopup(Product product)
        {
            InitializeComponent();
            _product = product;
            DataContext = _product;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(QuantityTextBox.Text, out decimal addedQuantity) && addedQuantity > 0)
            {
                SaveRequested?.Invoke(this, new CashierEditQuantityEventArgs
                {
                    Product = _product,
                    AddedQuantity = addedQuantity,
                    Note = NoteTextBox.Text
                });
            }
            else
            {
                MessageBox.Show("يرجى إدخال كمية صحيحة أكبر من صفر.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    public class CashierEditQuantityEventArgs : EventArgs
    {
        public Product Product { get; set; }
        public decimal AddedQuantity { get; set; }
        public string Note { get; set; }
    }
}
