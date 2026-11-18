

using POSSystem.WPF.Models;
using POSSystem.WPF.StaticFunction;
using POSSystem.WPF.ViewModels.Addons;
using POSSystem.WPF.ViewModels;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace POSSystem.WPF.Views
{
    public partial class OrderDetailsPopup : Window
    {
        public OrderItem OrderItem { get; set; }
        public string SpecialNotes { get; set; } = "";

        public AddonOrderDtilsViewModel _viewModel;
        public List<Addon> Addons { get; set; }

        public OrderDetailsPopup(OrderItem orderItem)
        {
            InitializeComponent();
            OrderItem = orderItem;
           
            _viewModel =new AddonOrderDtilsViewModel(OrderItem);
            DataContext = _viewModel;
        }

        

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }



        public ObservableCollection<OrderItem> OrderItems;

    private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {

            var vm = DataContext as AddonOrderDtilsViewModel;
            if (vm == null) return;

            // خذ نسخة من كل الإضافات المعروضة في الـ ViewModel (حتى تُبقى القائمة كاملة عند إعادة الفتح)
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

            // Assign the full list (with IsActive flags) to the OrderItem so reopening the popup
            // will show all addons and preserve selection state.
            OrderItem.Addons = allAddonsForView;

            // Create a lightweight OrderItem DTO for local processing / persistence if needed
            var Orderitems = new OrderItem
            {
                Addons     = OrderItem.Addons,
                Size       = OrderItem.Size,
                Notes      = SpecialNotes,
                TotalPrice = OrderItem.TotalPrice,
                ProductId  = OrderItem.ProductId,
                UnitPrice  = OrderItem.UnitPrice,
                Quantity   = OrderItem.Quantity,
                UpdatedAt  = OrderItem.UpdatedAt,
                CreatedAt  = OrderItem.CreatedAt,
                OrderId    = OrderItem.OrderId,
            };

            // Only iterate over the selected addons for stock decrement and join creation
            var selectedAddons = vm.SelectedAddons.ToList();
            foreach (var Addos in selectedAddons)
            {
                OrderItem.addonAndOrderItems.Add(new AddonAndOrderItem
                {
                    OrderitemID = OrderItem.Id,
                    AddonId = Addos.Id
                });

                // Decrease the addon stock by 1 for this confirmed order
                Addos.Quantity--;

                // Persist the quantity change to the database via the Addon service
                try
                {
                    await ServiceLocator.AddonService.UpdateAddonQuantityAsync(Addos.Id, Addos.Quantity);
                }
                catch (Exception ex)
                {
                    // Log and continue — we don't want a single addon update failure to crash the UI.
                    System.Diagnostics.Debug.WriteLine($"Failed to update addon quantity for Id={Addos.Id}: {ex.Message}");
                }

            }

            DialogResult = true;
            Close();
        }




       
        private void RecalculatePriceBySize(AddonOrderDtilsViewModel vm, string selectedTag)
        { 
            var selected = vm.Sizes.FirstOrDefault(s => s.Tag == selectedTag);
            if (selected != null)
            {
                // 1. Update OrderItem size name
                OrderItem.Size = selected.Name;

                // 2. Use the price from the selected size option (parsed from Tag)
                if (decimal.TryParse(selectedTag, out decimal finalPrice))
                {
                    OrderItem.UnitPrice = finalPrice;
                }
                else
                {
                    // Fallback to product price if tag parsing fails
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
                    // Ignore errors
                }
            }
        }



        // Size selection logic
        private void SizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string selectedTag)
            {
                var vm = DataContext as AddonOrderDtilsViewModel;
                if (vm == null) return;

                // 1. إلغاء تحديد كل الأحجام (للتأثير البصري)
                foreach (var size in vm.Sizes)
                {
                    size.IsActive = false;
                }

                RecalculatePriceBySize(vm,selectedTag);


            }
        }


    }
}
