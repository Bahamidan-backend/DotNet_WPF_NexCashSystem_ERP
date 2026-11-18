using Microsoft.Extensions.DependencyInjection;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Models;
using POSSystem.WPF.ViewModels.Cashier;
using POSSystem.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace POSSystem.WPF.Views.Popups
{
    public partial class DismantleProductPopup : UserControl
    {
        private List<Product> _allProducts = new List<Product>();
        private CashierViewModel _parentViewModel;

        public event EventHandler CloseRequested;

        public DismantleProductPopup(CashierViewModel parentViewModel)
        {
            InitializeComponent();
            _parentViewModel = parentViewModel;
            LoadProductsAsync();
        }

        private async void LoadProductsAsync()
        {
            try
            {
                var products = await POSSystem.WPF.App.ServiceProvider.GetRequiredService<IProductService>().GetAllProductsAsync();
                _allProducts = products.Where(p => !p.IsAddon && p.IsActive).ToList();
                
                SourceProductComboBox.ItemsSource = _allProducts;
                Target1ComboBox.ItemsSource = _allProducts;
                Target2ComboBox.ItemsSource = _allProducts;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في جلب المنتجات: {ex.Message}");
            }
        }

        private void SourceProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SourceProductComboBox.SelectedItem is Product selectedProduct)
            {
                SourceProductStockText.Text = $"الرصيد المتاح: {selectedProduct.TotalValue}";
            }
            else
            {
                SourceProductStockText.Text = "الرصيد المتاح: 0";
            }
        }

        private async void DismantleButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(SourceProductComboBox.SelectedItem is Product sourceProduct))
            {
                MessageBox.Show("يرجى اختيار المنتج المصدري", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(QuantityTextBox.Text, out decimal quantityToDismantle) || quantityToDismantle <= 0)
            {
                MessageBox.Show("يرجى إدخال كمية صحيحة للتفكيك", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var target1Product = Target1ComboBox.SelectedItem as Product;
            var target2Product = Target2ComboBox.SelectedItem as Product;

            if (target1Product == null && target2Product == null)
            {
                MessageBox.Show("يرجى اختيار هدف واحد على الأقل للتفكيك", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal totalSourceNafar = quantityToDismantle * sourceProduct.PersonNumberFood;

            if (sourceProduct.TotalValue < totalSourceNafar)
            {
                MessageBox.Show($"لا يوجد رصيد كافٍ في المصدر. المطلوب: {totalSourceNafar}، والمتوفر: {sourceProduct.TotalValue}", "رصيد غير كافٍ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Process the swap
                sourceProduct.TotalValue -= totalSourceNafar;
                sourceProduct.StockQuantity = sourceProduct.PersonNumberFood > 0 ? sourceProduct.TotalValue / sourceProduct.PersonNumberFood : 0;
                POSSystem.WPF.App.ServiceProvider.GetRequiredService<IUnitOfWork>().Products.Update(sourceProduct);

                if (target1Product != null && target1Product.Id != sourceProduct.Id)
                {
                    decimal target1Add = quantityToDismantle * target1Product.PersonNumberFood;
                    target1Product.TotalValue += target1Add;
                    target1Product.StockQuantity = target1Product.PersonNumberFood > 0 ? target1Product.TotalValue / target1Product.PersonNumberFood : 0;
                    POSSystem.WPF.App.ServiceProvider.GetRequiredService<IUnitOfWork>().Products.Update(target1Product);
                }

                if (target2Product != null && target2Product.Id != sourceProduct.Id && (target1Product == null || target2Product.Id != target1Product.Id))
                {
                    decimal target2Add = quantityToDismantle * target2Product.PersonNumberFood;
                    target2Product.TotalValue += target2Add;
                    target2Product.StockQuantity = target2Product.PersonNumberFood > 0 ? target2Product.TotalValue / target2Product.PersonNumberFood : 0;
                    POSSystem.WPF.App.ServiceProvider.GetRequiredService<IUnitOfWork>().Products.Update(target2Product);
                }

                // Push to DB
                await POSSystem.WPF.App.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();

                MessageBox.Show("تمت عملية التفكيك بنجاح!", "تم التفكيك", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Refresh Cashier screen
                if (_parentViewModel != null)
                {
                    _ = _parentViewModel.LoadProductsAsync();
                }

                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء التفكيك: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
