using System.Threading.Tasks;
using POSSystem.WPF.Models;
using POSSystem.WPF.Business.Services;

namespace POSSystem.WPF.ViewModels.Discounts
{
    public partial class DiscountsViewModel
    {
        public void SelectAllStatus()
        {
            discountsProberty.AllStatusSelected = true;
            discountsProberty.ActiveSelected = false;
            discountsProberty.ExpiredSelected = false;
            discountsProberty.PendingSelected = false;
            discountsProberty.InactiveSelected = false;
            discountsProberty.FilterDiscounts();
        }

        public void ClearAllFilters()
        {
            discountsProberty.SearchText = "";
            SelectAllStatus();
        }

        public async void DeleteDiscount(Discount discount)
        {
            await _DiscountsServices.DeleteDiscounts(discount);
            LoadSampleDiscounts();
            discountsProberty.FilterDiscounts();
        }

        public async Task AddDiscount(Discount discount)
        {
            await _DiscountsServices.AddDiscounts(discount);
            LoadSampleDiscounts();
            discountsProberty.FilterDiscounts();
        }

        public async void UpdateDiscount(Discount discount)
        {
            await _DiscountsServices.UpdateDiscounts(discount);
            LoadSampleDiscounts();
            discountsProberty.FilterDiscounts();
        }

        public async Task ApplyBulkDiscount()
        {
            var productService = new ProductService(_UnitOfWork);
            await productService.ApplyBulkDiscountAsync(discountsProberty.BulkDiscountPercentage);
            discountsProberty.BulkDiscountPercentage = 0;
            LoadSampleDiscounts();
            POSSystem.WPF.Business.ServiceLocator.NotifyInventoryChanged();
        }

        public async Task RevertBulkDiscount()
        {
            var productService = new ProductService(_UnitOfWork);
            await productService.RevertBulkDiscountAsync();
            LoadSampleDiscounts();
            POSSystem.WPF.Business.ServiceLocator.NotifyInventoryChanged();
        }
    }
}
