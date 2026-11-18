using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Views.Popups
{
    public partial class OrderDetailsPopup : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public Product Product { get; private set; }
        public ObservableCollection<SizeSelectionItem> AvailableSizes { get; private set; }
        
        private string _orderNote = string.Empty;
        public string OrderNote
        {
            get => _orderNote;
            set { _orderNote = value; OnPropertyChanged(nameof(OrderNote)); }
        }

        private decimal _totalPrice;
        public decimal TotalPrice
        {
            get => _totalPrice;
            set { _totalPrice = value; OnPropertyChanged(nameof(TotalPrice)); }
        }

        public ProductSize SelectedSize => AvailableSizes.FirstOrDefault(s => s.IsSelected)?.ProductSize;

        public OrderDetailsPopup(Product product)
        {
            InitializeComponent();
            Product = product;
            
            AvailableSizes = new ObservableCollection<SizeSelectionItem>();
            bool hasFull = false, hasHalf = false, hasQuarter = false;

            if (product.ProductSizes != null)
            {
                foreach (var size in product.ProductSizes)
                {
                    if (size.SizeName == "نفر" || size.SizeName == "حبة") hasFull = true;
                    if (size.SizeName == "نص نفر" || size.SizeName == "نص حبة") hasHalf = true;
                    if (size.SizeName == "ربع نفر" || size.SizeName == "ربع حبة") hasQuarter = true;
                    
                    // Fix price based on size name dynamically if needed
                    CalculateDynamicPrice(size, product.Price);

                    var item = new SizeSelectionItem { ProductSize = size };
                    item.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(SizeSelectionItem.IsSelected) && item.IsSelected)
                        {
                            UpdateTotal();
                        }
                    };
                    AvailableSizes.Add(item);
                }
            }

            // Automatically add missing standard sizes based on existence of others
            // If it has Half but no Quarter, inject Quarter.
            // Or just always ensure standard portions if it's a portion-based product.
            if ((hasFull || hasHalf) && !hasQuarter)
            {
                bool isHabaType = product.ProductSizes != null && product.ProductSizes.Any(s => s.SizeName == "نص حبة");
                var qSize = new ProductSize { SizeName = hasHalf && isHabaType ? "ربع حبة" : "ربع نفر", Price = product.Price / 4 };
                var qItem = new SizeSelectionItem { ProductSize = qSize };
                qItem.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(SizeSelectionItem.IsSelected) && qItem.IsSelected) UpdateTotal(); };
                AvailableSizes.Add(qItem);
            }

            // Select the first size by default
            if (AvailableSizes.Any())
            {
                AvailableSizes.First().IsSelected = true;
                TotalPrice = AvailableSizes.First().ProductSize.Price;
            }
            else
            {
                TotalPrice = product.Price;
            }

            DataContext = this;
        }

        private void UpdateTotal()
        {
            var selected = SelectedSize;
            if (selected != null)
            {
                CalculateDynamicPrice(selected, Product.Price);
                TotalPrice = selected.Price;
            }
        }

        private void CalculateDynamicPrice(ProductSize size, decimal basePrice)
        {
            if (size.SizeName == "نفر" || size.SizeName == "حبة")
            {
                size.Price = basePrice;
            }
            else if (size.SizeName == "نص نفر" || size.SizeName == "نص حبة")
            {
                size.Price = basePrice / 2;
            }
            else if (size.SizeName == "ربع نفر" || size.SizeName == "ربع حبة")
            {
                size.Price = basePrice / 4;
            }
            else if (size.SizeName == "نفر ونص" || size.SizeName == "حبة ونص")
            {
                size.Price = basePrice * 1.5m;
            }
            else if (size.SizeName == "نفرين" || size.SizeName == "حبتين")
            {
                size.Price = basePrice * 2;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void NoteTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (NotePlaceholder != null)
            {
                NotePlaceholder.Visibility = string.IsNullOrWhiteSpace(NoteTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    public class SizeSelectionItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        public ProductSize ProductSize { get; set; }
        public string SizeName => ProductSize?.SizeName;
        
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }
    }
}
