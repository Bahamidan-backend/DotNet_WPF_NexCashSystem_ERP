using System.Threading.Tasks;
using POSSystem.WPF.Models;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.Data;

namespace POSSystem.WPF.ViewModels.Discounts
{
    public partial class DiscountsViewModel : BaseViewModel, POSSystem.WPF.Services.ISearchable
    {
        public DiscountsViewModel()
        {
            var sp = POSSystem.WPF.App.ServiceProvider;
            _UnitOfWork = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Data.UnitOfWork.IUnitOfWork>(sp);
            _DiscountsServices = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IDiscountsServices>(sp);
            discountsProberty = new DiscountsProberty();
            LoadSampleDiscounts();
            discountsProberty.FilterDiscounts();
        }

        public void ApplySearch(string query)
        {
            discountsProberty.SearchText = query ?? string.Empty;
        }

        private async void LoadSampleDiscounts()
        {
            discountsProberty._allDiscounts.Clear();

            var DataAll = await _DiscountsServices.GetAllDiscounts();
            foreach (var discount in DataAll)
            {
                discountsProberty._allDiscounts.Add(discount);
            }
            OnPropertyChanged(discountsProberty.FilterDiscounts().ToString());
        }

    }
}
