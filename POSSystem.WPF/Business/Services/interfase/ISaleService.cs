
namespace POSSystem.WPF.Business.Services.interfase
{
    public interface ISaleService
    {
        Task<IEnumerable<Sale>> GetAllSalesAsync();
        Task<IEnumerable<Sale>> GetSalesWithItemsAsync();
        Task<Sale?> GetSaleByIdAsync(int id);
        Task<Sale?> GetSaleWithItemsAsync(int id);
        Task<IEnumerable<Sale>> GetSalesByUserAsync(int userId);
        Task<IEnumerable<Sale>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Sale>> GetSalesByStatusAsync(SaleStatus status);
        Task<IEnumerable<Sale>> GetTodaySalesAsync();
        Task<Sale> CreateSaleAsync(Sale sale);
        Task<Sale> UpdateSaleAsync(Sale sale);
        Task DeleteSaleAsync(int saleId);
        Task<string> GenerateUniqueInvoiceNumberAsync();
        Task<bool> ValidateSaleAsync(Sale sale);
        Task<decimal> GetTotalSalesByDateAsync(DateTime date);
        Task<decimal> GetTotalSalesByMonthAsync(int year, int month);
        Task<bool> ProcessSaleAsync(Sale sale);
    }
}
