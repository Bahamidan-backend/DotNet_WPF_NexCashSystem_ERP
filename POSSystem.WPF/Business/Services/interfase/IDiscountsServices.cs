

namespace POSSystem.WPF.Business.Services.interfase
{
    interface IDiscountsServices
    {

        Task<IEnumerable<Discount>> GetAllDiscounts();

        Task<int> AddDiscounts(Discount discount);

        Task<int> UpdateDiscounts(Discount discount);
        Task<int> DeleteDiscounts(  Discount discount);

    }
}
