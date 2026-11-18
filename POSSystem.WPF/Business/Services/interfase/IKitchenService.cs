using POSSystem.WPF.Models;

namespace POSSystem.WPF.Business.Services.interfase
{
    public interface IKitchenService
    {
        Task<IEnumerable<Kitchen>> GetAllKitchensAsync();
        Task<Kitchen?> GetKitchenByIdAsync(int id);
        Task CreateKitchenAsync(Kitchen kitchen);
        Task UpdateKitchenAsync(Kitchen kitchen);
        Task DeleteKitchenAsync(Kitchen kitchen);
    }
}
