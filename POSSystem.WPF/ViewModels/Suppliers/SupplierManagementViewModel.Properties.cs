using System;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Shered.Proberty;

namespace POSSystem.WPF.ViewModels.Suppliers
{
    public partial class SupplierManagementViewModel
    {
        private readonly ISupplierService _supplierService;
        public SupplierManagementProberty Property { get; }
        public SupplierManagementCommands Commands { get; }
        public event Action? SuppliersChanged;
    }
}
