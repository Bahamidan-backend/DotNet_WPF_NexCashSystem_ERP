using System;
using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.ViewModels.Suppliers;

namespace POSSystem.WPF.ViewModels.Purchases
{
    public partial class PurchaseManagementViewModel
    {
        private readonly IWarehouseService _warehouseService;
        private readonly IPurchaseService _purchaseService;
        private readonly IAddonService _addonService;
        private readonly ISupplierService _supplierService;
        private readonly IPurchaseReturnService _purchaseReturnService;
        private readonly IReceiptVoucherService _receiptVoucherService;

        public PurchaseManagementProberty PurchaseManagementProberty { get; }
        public SupplierManagementViewModel SupplierVM { get; }
        public event Action? OnRequestClose;
    }
}
