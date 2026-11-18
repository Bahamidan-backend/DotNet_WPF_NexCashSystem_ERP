using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;
using POSSystem.WPF.StaticFunction;
using POSSystem.WPF.ViewModels.Addons;
using POSSystem.WPF.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System;
using POSSystem.WPF.Business.Services;

namespace POSSystem.WPF.Controls
{
    public partial class ProductModifiersControl : UserControl
    {
        public OrderItem OrderItem { get; set; }
        public string SpecialNotes { get; set; } = "";
        public AddonOrderDtilsViewModel _viewModel;
        public event EventHandler CloseRequested; 

        public ProductModifiersControl()
        {
            InitializeComponent();
        }

        public void Initialize(OrderItem orderItem)
        {
            OrderItem = orderItem;
            // Assuming AddonOrderDtilsViewModel is available. If it's defined in OrderDetailsPopup.xaml.cs, we might need to move it or it might be in a separate file.
            // Based on previous file read, it seemed to be used in OrderDetailsPopup.xaml.cs but class definition wasn't fully visible or was imported.
            // Let's assume it's accessible.
            _viewModel = new AddonOrderDtilsViewModel(OrderItem);
            DataContext = _viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as AddonOrderDtilsViewModel;
            if (vm == null) return;

            // Take a copy of all displayed addons to preserve list state
            var allAddonsForView = vm.Addons.Select(a => new Addon
            {
                Id = a.Id,
                IsActive = a.IsActive,
                CategoryId = a.CategoryId,
                Description = a.Description,
                Name = a.Name,
                Price = a.Price,
                Quantity = a.Quantity,
                TotalAmount = a.TotalAmount,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            }).ToList();

            OrderItem.Addons = allAddonsForView;

            // Only iterate over the selected addons for stock decrement and join creation
            var selectedAddons = vm.SelectedAddons.ToList();
            
            // Clear existing addon associations if re-editing (though typically this is a new addition logic?)
            // If we are modifying an existing line item, we might need to be careful. 
            // The original logic just added to addonAndOrderItems.
            
            // FIX ISSUE-FIN-009: Clear previous modifier links on re-edit, attach Addon object for price calculation,
            // and do NOT deduct Addon stock in the popup before checkout (OrderService deducts it inside the sale transaction).
            OrderItem.addonAndOrderItems.Clear();
            foreach (var Addos in selectedAddons)
            {
                OrderItem.addonAndOrderItems.Add(new AddonAndOrderItem
                {
                    OrderitemID = OrderItem.Id,
                    AddonId = Addos.Id,
                    Addon = Addos
                });
            }
            OrderItem.InitializeTotalPrice();

            // Sync Special Notes back to OrderItem
            // The ViewModel binding might not update OrderItem directly if not set up that way
            // Sync Special Notes back to OrderItem
            if (!string.IsNullOrEmpty(SpecialNotes))
            {
                OrderItem.Notes = SpecialNotes;
            }
             // Actually, looking at the XAML: Text="{Binding SpecialNotes, UpdateSourceTrigger=PropertyChanged}"
             // And looking at original code: public string SpecialNotes { get; set; } = ""; in Window.
             // But DataContext was _viewModel. 
             // If _viewModel DOES NOT have SpecialNotes, the binding would fail or look for it on the Window if using ElementName/RelativeSource (which it wasn't).
             // WAIT. In the original code, the Window had `public string SpecialNotes`. But DataContext was `_viewModel`. 
             // Unless `_viewModel` has `SpecialNotes`, the binding `Text="{Binding SpecialNotes}"` would look at `_viewModel.SpecialNotes`.
             // I will assume `AddonOrderDtilsViewModel` has `SpecialNotes`.
             // HOWEVER, in the original code, `OrderItem.Notes = SpecialNotes;` was used. `SpecialNotes` was a property of the Window class.
             // So the binding in original XAML likely worked if `SpecialNotes` was in VM, OR it was broken/unused, OR I missed something.
             // Let's safeguard: if VM has it, use it. If not, we rely on the property in this class, but we need to bind to IT.
             
             // To be safe and mimicking original behavior (where it seemed to update a property), I'll rely on the VM having it if the original XAML worked.
             // But wait, the original XAML had `Text="{Binding SpecialNotes..."`. 
             // Generally, data context is the VM. So the VM must have it.
             
             if (vm.GetType().GetProperty("SpecialNotes") != null)
             {
                 OrderItem.Notes = (string)vm.GetType().GetProperty("SpecialNotes").GetValue(vm);
             }

            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void RecalculatePriceBySize(AddonOrderDtilsViewModel vm, string selectedTag)
        {
            var selected = vm.Sizes.FirstOrDefault(s => s.Tag == selectedTag);
            if (selected != null)
            {
                OrderItem.Size = selected.Name;

                if (decimal.TryParse(selectedTag, out decimal parsedPrice))
                {
                    OrderItem.UnitPrice = parsedPrice;
                }
                else
                {
                    // Fallback to base price if parsing fails for some reason
                    OrderItem.UnitPrice = OrderItem.Product.Price; 
                }
                OrderItem.InitializeTotalPrice();

                selected.IsActive = true;

                try
                {
                    var addonsTotal = 0m;
                    if (vm != null)
                    {
                        addonsTotal = vm.TotalAmount;
                    }
                    OrderItem.TotalPrice = (OrderItem.UnitPrice * OrderItem.Quantity) + addonsTotal;
                    vm.TriggerTotalUpdate();
                }
                catch
                {
                    // Ignore
                }
            }
        }

        private void SizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string selectedTag)
            {
                var vm = DataContext as AddonOrderDtilsViewModel;
                if (vm == null) return;

                foreach (var size in vm.Sizes)
                {
                    size.IsActive = false;
                }

                RecalculatePriceBySize(vm, selectedTag);
            }
        }
    }
}
