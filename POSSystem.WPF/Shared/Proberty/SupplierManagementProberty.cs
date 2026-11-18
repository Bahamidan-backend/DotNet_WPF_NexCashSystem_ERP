using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using POSSystem.WPF.Models;
using POSSystem.WPF.Business.Services.interfase;

namespace POSSystem.WPF.Shered.Proberty
{
    public class SupplierManagementProberty : BaseModel
    {
        private readonly ISupplierService _supplierService;

        public SupplierManagementProberty(ISupplierService supplierService)
        {
            _supplierService = supplierService;
            _suppliers = new ObservableCollection<Supplier>();
            _selectedSupplierPurchases = new ObservableCollection<Purchase>();
            _selectedSupplierPayments = new ObservableCollection<SupplierPayment>();
            _selectedSupplierLedger = new ObservableCollection<SupplierLedgerEntry>();
        }

        public ObservableCollection<Supplier> _suppliers;
        public ObservableCollection<Purchase> _selectedSupplierPurchases;
        public ObservableCollection<SupplierPayment> _selectedSupplierPayments;
        public ObservableCollection<SupplierLedgerEntry> _selectedSupplierLedger;

        private Supplier? _selectedSupplier;
        private string _searchText = "";
        
        // Summary Cards
        private decimal _totalSuppliersDebt;
        private int _totalSuppliersCount;
        private int _overdueSuppliersCount;

        public ObservableCollection<Supplier> Suppliers => _suppliers;
        public ObservableCollection<Purchase> SelectedSupplierPurchases => _selectedSupplierPurchases;
        public ObservableCollection<SupplierPayment> SelectedSupplierPayments => _selectedSupplierPayments;
        public ObservableCollection<SupplierLedgerEntry> SelectedSupplierLedger => _selectedSupplierLedger;

        public Supplier? SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                _selectedSupplier = value;
                OnPropertyChanged();
            }
        }


        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }

        public decimal TotalSuppliersDebt
        {
            get => _totalSuppliersDebt;
            set { _totalSuppliersDebt = value; OnPropertyChanged(); }
        }

        public int TotalSuppliersCount
        {
            get => _totalSuppliersCount;
            set { _totalSuppliersCount = value; OnPropertyChanged(); }
        }

        public int OverdueSuppliersCount
        {
            get => _overdueSuppliersCount;
            set 
            { 
                _overdueSuppliersCount = value; 
                OnPropertyChanged();
            }
        }

        public bool HasNoSuppliers => Suppliers == null || Suppliers.Count == 0;
        public bool HasSuppliers => !HasNoSuppliers;

        public void NotifySuppliersChanged()
        {
            OnPropertyChanged(nameof(Suppliers));
            OnPropertyChanged(nameof(HasNoSuppliers));
            OnPropertyChanged(nameof(HasSuppliers));
        }
    }
}
