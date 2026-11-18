using POSSystem.WPF.Helpers;
using System.Collections.ObjectModel;
using System.Windows.Input;
using POSSystem.WPF.Models;
using POSSystem.WPF.Business.Services.interfase;

namespace POSSystem.WPF.ViewModels.Purchases
{
    public partial class PurchaseReturnViewModel
    {
        private readonly IPurchaseReturnService _purchaseReturnService;
        private readonly IPurchaseService _purchaseService;
        private readonly POSSystem.WPF.Data.UnitOfWork.IUnitOfWork _unitOfWork;

        private string _searchInvoiceNumber = string.Empty;
        private Purchase? _currentPurchase;
        private Purchase? _selectedPurchase;
        private Supplier? _selectedSupplierFilter;
        private string _returnReason = string.Empty;
        private string _returnNotes = string.Empty;
        private string _selectedRefundChannel = "حاسب كريمي";
        private PaymentMethod _returnMethod = PaymentMethod.OnCredit;
        private PurchaseReturnSettlementMode _selectedSettlementMode = PurchaseReturnSettlementMode.DebitNote;
        private User _currentUser;
        
        public ObservableCollection<Purchase> AllPurchases { get; } = new ObservableCollection<Purchase>();
        public ObservableCollection<Purchase> FilteredPurchases { get; } = new ObservableCollection<Purchase>();
        public ObservableCollection<Supplier> AllSuppliers { get; } = new ObservableCollection<Supplier>();

        public ObservableCollection<string> AvailableRefundChannels { get; } = new ObservableCollection<string>
        {
            "حاسب كريمي",
            "محفظة جيب",
            "محفظة فلوسك",
            "محفظة ون كاش",
            "محفظة جوالي",
            "محفظة موبايل موني",
            "محفظة يمن والت",
            "الصندوق الرئيسي"
        };

        public string SelectedRefundChannel
        {
            get => _selectedRefundChannel;
            set => SetProperty(ref _selectedRefundChannel, value);
        }

        private string _supplierName = string.Empty;
        private string _purchaseDate = string.Empty;
        private string _originalPaymentMethod = string.Empty;

        public ObservableCollection<PurchaseReturnItemViewModel> ReturnItems { get; } = new ObservableCollection<PurchaseReturnItemViewModel>();
        
        public ObservableCollection<string> CommonReasons { get; } = new ObservableCollection<string>
        {
            "انتهاء صلاحية", "تالف", "خطأ في الطلب", "جودة سيئة", "أخرى"
        };
        
        public ObservableCollection<PaymentMethod> PaymentMethods { get; } = new ObservableCollection<PaymentMethod>
        {
            PaymentMethod.OnCredit, PaymentMethod.Cash
        };

        public Supplier? SelectedSupplierFilter
        {
            get => _selectedSupplierFilter;
            set
            {
                if (SetProperty(ref _selectedSupplierFilter, value))
                {
                    ApplySupplierFilter();
                }
            }
        }

        public PurchaseReturnSettlementMode SelectedSettlementMode
        {
            get => _selectedSettlementMode;
            set
            {
                if (SetProperty(ref _selectedSettlementMode, value))
                {
                    OnPropertyChanged(nameof(IsDebitNoteSettlement));
                    OnPropertyChanged(nameof(IsCashReceiptSettlement));
                    ReturnMethod = (_selectedSettlementMode == PurchaseReturnSettlementMode.CashReceipt) 
                        ? PaymentMethod.Cash 
                        : PaymentMethod.OnCredit;
                }
            }
        }

        public bool IsDebitNoteSettlement
        {
            get => _selectedSettlementMode == PurchaseReturnSettlementMode.DebitNote;
            set
            {
                if (value) SelectedSettlementMode = PurchaseReturnSettlementMode.DebitNote;
            }
        }

        public bool IsCashReceiptSettlement
        {
            get => _selectedSettlementMode == PurchaseReturnSettlementMode.CashReceipt;
            set
            {
                if (value) SelectedSettlementMode = PurchaseReturnSettlementMode.CashReceipt;
            }
        }

        public Purchase? SelectedPurchase
        {
            get => _selectedPurchase;
            set 
            { 
                _selectedPurchase = value; 
                OnPropertyChanged();
                if (_selectedPurchase != null)
                {
                    SearchInvoiceNumber = _selectedPurchase.PurchaseNumber;
                    SearchPurchaseAsync().SafeFireAndForget();
                }
            }
        }

        public string SearchInvoiceNumber
        {
            get => _searchInvoiceNumber;
            set { _searchInvoiceNumber = value; OnPropertyChanged(); }
        }

        public Purchase? CurrentPurchase
        {
            get => _currentPurchase;
            set 
            { 
                _currentPurchase = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(HasPurchase));
                ((RelayCommand)AddAllToReturnCommand).RaiseCanExecuteChanged();
            }
        }

        public bool HasPurchase => CurrentPurchase != null;
        public decimal TotalReturnAmount => ReturnItems.Sum(x => x.TotalAmount);

        public string ReturnReason
        {
            get => _returnReason;
            set { _returnReason = value; OnPropertyChanged(); ((RelayCommand)ProcessReturnCommand).RaiseCanExecuteChanged(); }
        }
        
        public string ReturnNotes
        {
            get => _returnNotes;
            set { _returnNotes = value; OnPropertyChanged(); ((RelayCommand)ProcessReturnCommand).RaiseCanExecuteChanged(); }
        }

        public PaymentMethod ReturnMethod
        {
            get => _returnMethod;
            set { _returnMethod = value; OnPropertyChanged(); }
        }

        public string SupplierName { get => _supplierName; set { _supplierName = value; OnPropertyChanged(); } }
        public string PurchaseDate { get => _purchaseDate; set { _purchaseDate = value; OnPropertyChanged(); } }
        public string OriginalPaymentMethod { get => _originalPaymentMethod; set { _originalPaymentMethod = value; OnPropertyChanged(); } }

        public ICommand SearchPurchaseCommand { get; }
        public ICommand ProcessReturnCommand { get; }
        public ICommand AddAllToReturnCommand { get; }
        public ICommand ClearSupplierFilterCommand { get; }
        public ICommand SelectRefundChannelQuickCommand { get; }
    }
}
