using POSSystem.WPF.Models;

namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface IKitchenRepository : IRepository<Kitchen>
    {
        Task<IEnumerable<Kitchen>> GetActiveKitchensAsync();
        Task<Kitchen?> GetKitchenByNameAsync(string name);
    }
}
