using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Business.Services.interfase
{
    public interface IPurchaseReturnService
    {
        Task<PurchaseReturn> GetByIdAsync(int id);
        Task<IEnumerable<PurchaseReturn>> GetAllAsync();
        Task<PurchaseReturn> ProcessReturnAsync(PurchaseReturn purchaseReturn);
        Task<string> GenerateReturnNumberAsync();
        Task<IEnumerable<PurchaseReturn>> SearchAsync(string searchTerm, DateTime? fromDate, DateTime? toDate);
    }
}
