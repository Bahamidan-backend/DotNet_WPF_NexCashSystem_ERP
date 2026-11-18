using POSSystem.WPF.Models;

namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface IPurchaseRepository : IRepository<Purchase>
    {
        Task<IEnumerable<Purchase>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Purchase>> GetBySupplierAsync(string supplierName);
        Task<decimal> GetTotalByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Purchase?> GetWithItemsAsync(int id);
        //Task<IEnumerable<Purchase>> GetAllPurchaseAsync();

    }
}

