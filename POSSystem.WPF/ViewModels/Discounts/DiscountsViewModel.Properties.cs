using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.Business.Services;

namespace POSSystem.WPF.ViewModels.Discounts
{
    public partial class DiscountsViewModel
    {
        private POSSystem.WPF.Data.UnitOfWork.IUnitOfWork _UnitOfWork;
        private POSSystem.WPF.Business.Services.interfase.IDiscountsServices _DiscountsServices;
        public DiscountsProberty discountsProberty { get; }
    }
}
