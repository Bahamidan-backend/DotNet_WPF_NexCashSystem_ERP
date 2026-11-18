
namespace POSSystem.WPF.Data.Repositories.Interfase
{
    public interface ISaleRepository : IRepository<Sale>
    {
        Task<IEnumerable<Sale>> GetSalesWithItemsAsync();
        Task<IEnumerable<Sale>> GetSalesByUserAsync(int userId);
        Task<IEnumerable<Sale>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Sale>> GetSalesByStatusAsync(SaleStatus status);
        Task<Sale?> GetSaleWithItemsAsync(int saleId);
        Task<string> GenerateUniqueInvoiceNumberAsync();
        Task<bool> InvoiceNumberExistsAsync(string invoiceNumber);
        Task<IEnumerable<Sale>> GetTodaySalesAsync();
        Task<decimal> GetTotalSalesByDateAsync(DateTime date);
        Task<decimal> GetTotalSalesByMonthAsync(int year, int month);
    }
}
