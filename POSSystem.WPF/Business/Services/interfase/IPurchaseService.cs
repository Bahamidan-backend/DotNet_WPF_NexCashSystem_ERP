using POSSystem.WPF.Models;

namespace POSSystem.WPF.Business.Services.interfase
{
    public interface IPurchaseService
    {
        //Task<IEnumerable<Purchase>> GetAllPurchasesAsync();
        Task<IEnumerable<PurchaseItem>> GetAllPurchaseAsync1();

        Task<IEnumerable<Purchase>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Purchase>> GetBySupplierAsync(string supplierName);
        Task<Purchase?> GetPurchaseByIdAsync(int id);
        Task<Purchase?> GetPurchaseWithItemsAsync(int id);
        Task<Purchase> CreatePurchaseAsync(Purchase purchase);
        Task<Purchase> UpdatePurchaseAsync(Purchase purchase);
        Task DeletePurchaseAsync(int id);
        Task<decimal> GetTotalByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<string> GeneratePurchaseNumberAsync();
        Task<PurchaseItem> CreatePurchaseitemAsync(PurchaseItem purchaseitem);

    }
}

