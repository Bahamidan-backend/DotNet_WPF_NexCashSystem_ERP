using POSSystem.WPF.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace POSSystem.WPF.Business.Services.interfase
{
    public interface ISupplierService
    {
        Task<IEnumerable<Supplier>> GetAllSuppliersAsync();
        Task<Supplier?> GetSupplierByIdAsync(int id);
        Task<Supplier> CreateSupplierAsync(Supplier supplier);
        Task UpdateSupplierAsync(Supplier supplier);
        Task DeleteSupplierAsync(int id);

        Task RecordPaymentAsync(SupplierPayment payment, int? shiftId);
        Task<IEnumerable<SupplierPayment>> GetSupplierPaymentsAsync(int supplierId);
        Task<IEnumerable<Purchase>> GetSupplierPurchasesAsync(int supplierId);
        Task<IEnumerable<PurchaseReturn>> GetSupplierReturnsAsync(int supplierId);
        Task<IEnumerable<SupplierLedgerEntry>> GetSupplierLedgerAsync(int supplierId);
        
        /// <summary>
        /// Gets all suppliers with outstanding debt.
        /// </summary>
        Task<IEnumerable<Supplier>> GetSuppliersWithDebtAsync();
    }
}
