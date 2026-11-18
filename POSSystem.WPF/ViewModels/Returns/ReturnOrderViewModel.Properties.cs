using POSSystem.WPF.Helpers;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using POSSystem.WPF.Models;
using POSSystem.WPF.Business.Services;

namespace POSSystem.WPF.ViewModels.Returns
{
    public partial class ReturnOrderViewModel
    {
        private readonly IReturnOrderService _returnOrderService;
        private readonly IOrderService _orderService;
        private readonly POSSystem.WPF.Data.UnitOfWork.IUnitOfWork _unitOfWork;
        private readonly IShiftService _shiftService;

        private string _searchInvoiceNumber = string.Empty;
        private Order? _currentOrder;
        private Order? _selectedOrder;
        private string _returnReason = string.Empty;
        private string _returnNotes = string.Empty;
        private PaymentMethod _returnMethod = PaymentMethod.Cash;
        private User _currentUser;

        public ObservableCollection<Order> AllOrders { get; } = new ObservableCollection<Order>();

        private string _orderCustomerName = string.Empty;
        private string _orderTime = string.Empty;
        private string _orderCashierName = string.Empty;
        private string _currentOrderStatus = string.Empty;
        private string _selectedReturnType = "لا يوجد";

        public bool IsAdmin => _currentUser.Role == UserRole.Admin;
        public bool CanReturnFullOrder => IsAdmin;
        public Visibility FullReturnButtonVisibility => CanReturnFullOrder ? Visibility.Visible : Visibility.Collapsed;
        public bool DisablePaymentMethodSelection => false;

        public ObservableCollection<ReturnOrderItemViewModel> ReturnItems { get; } = new ObservableCollection<ReturnOrderItemViewModel>();
        public ObservableCollection<string> CommonReasons { get; } = new ObservableCollection<string>
        {
            "خطأ مطبخ", "تغيير رأي العميل", "تأخير", "جودة سيئة", "أخرى"
        };
        
        public ObservableCollection<PaymentMethod> PaymentMethods { get; } = new ObservableCollection<PaymentMethod>
        {
            PaymentMethod.Cash, PaymentMethod.MobilePayment, PaymentMethod.CreditCard, PaymentMethod.OnCredit, PaymentMethod.Remake
        };

        public ObservableCollection<string> PaymentMethodOptions { get; } = new ObservableCollection<string>
        {
            "نقدي (كاش من الدرج)",
            "إيداع / محفظة إلكترونية",
            "شبكة / بطاقة بنكية",
            "آجل (خصم من الدين)",
            "إعادة تجهيز (بدون استرداد مالي)"
        };

        private string _selectedPaymentMethodOption = "نقدي (كاش من الدرج)";
        public string SelectedPaymentMethodOption
        {
            get => _selectedPaymentMethodOption;
            set
            {
                if (SetProperty(ref _selectedPaymentMethodOption, value))
                {
                    UpdateReturnMethodFromOption(value);
                }
            }
        }

        public ObservableCollection<string> AvailableWallets { get; } = new ObservableCollection<string>(FinancialChannelHelper.GetActiveWalletsList());

        private string _selectedRefundWallet = "حاسب كريمي";
        public string SelectedRefundWallet
        {
            get => _selectedRefundWallet;
            set => SetProperty(ref _selectedRefundWallet, value);
        }

        private string _refundReferenceNumber = string.Empty;
        public string RefundReferenceNumber
        {
            get => _refundReferenceNumber;
            set => SetProperty(ref _refundReferenceNumber, value);
        }

        private bool _isWalletRefundSelected = false;
        public bool IsWalletRefundSelected
        {
            get => _isWalletRefundSelected;
            set => SetProperty(ref _isWalletRefundSelected, value);
        }

        private bool _hasOriginalPaymentNotice = false;
        public bool HasOriginalPaymentNotice
        {
            get => _hasOriginalPaymentNotice;
            set => SetProperty(ref _hasOriginalPaymentNotice, value);
        }

        private string _originalPaymentNoticeText = string.Empty;
        public string OriginalPaymentNoticeText
        {
            get => _originalPaymentNoticeText;
            set => SetProperty(ref _originalPaymentNoticeText, value);
        }

        public void UpdateReturnMethodFromOption(string option)
        {
            switch (option)
            {
                case "نقدي (كاش من الدرج)":
                    ReturnMethod = PaymentMethod.Cash;
                    IsWalletRefundSelected = false;
                    break;
                case "إيداع / محفظة إلكترونية":
                    ReturnMethod = PaymentMethod.MobilePayment;
                    IsWalletRefundSelected = true;
                    break;
                case "شبكة / بطاقة بنكية":
                    ReturnMethod = PaymentMethod.DebitCard;
                    IsWalletRefundSelected = false;
                    break;
                case "آجل (خصم من الدين)":
                    ReturnMethod = PaymentMethod.OnCredit;
                    IsWalletRefundSelected = false;
                    break;
                case "إعادة تجهيز (بدون استرداد مالي)":
                    ReturnMethod = PaymentMethod.Remake;
                    IsWalletRefundSelected = false;
                    break;
                default:
                    ReturnMethod = PaymentMethod.Cash;
                    IsWalletRefundSelected = false;
                    break;
            }
        }

        public void SetReturnMethodOption(PaymentMethod method)
        {
            SelectedPaymentMethodOption = method switch
            {
                PaymentMethod.MobilePayment => "إيداع / محفظة إلكترونية",
                PaymentMethod.DebitCard or PaymentMethod.CreditCard => "شبكة / بطاقة بنكية",
                PaymentMethod.OnCredit => "آجل (خصم من الدين)",
                PaymentMethod.Remake => "إعادة تجهيز (بدون استرداد مالي)",
                _ => "نقدي (كاش من الدرج)"
            };
        }

        public Order? SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                _selectedOrder = value;
                OnPropertyChanged();
                if (_selectedOrder != null)
                {
                    SearchInvoiceNumber = _selectedOrder.OrderNumber;
                    SearchOrderAsync().SafeFireAndForget();
                }
            }
        }

        public string SearchInvoiceNumber
        {
            get => _searchInvoiceNumber;
            set { _searchInvoiceNumber = value; OnPropertyChanged(); }
        }

        public Order? CurrentOrder
        {
            get => _currentOrder;
            set 
            { 
                _currentOrder = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(HasOrder));
                ((RelayCommand)AddAllToReturnCommand).RaiseCanExecuteChanged();
            }
        }

        public bool HasOrder => CurrentOrder != null;
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

        public string OrderCustomerName { get => _orderCustomerName; set { _orderCustomerName = value; OnPropertyChanged(); } }
        public string OrderTime { get => _orderTime; set { _orderTime = value; OnPropertyChanged(); } }
        public string OrderCashierName { get => _orderCashierName; set { _orderCashierName = value; OnPropertyChanged(); } }
        public string CurrentOrderStatus { get => _currentOrderStatus; set { _currentOrderStatus = value; OnPropertyChanged(); } }
        public string SelectedReturnType { get => _selectedReturnType; set { _selectedReturnType = value; OnPropertyChanged(); } }

        public ICommand SearchOrderCommand { get; }
        public ICommand ProcessReturnCommand { get; }
        public ICommand AddAllToReturnCommand { get; }
        public ICommand SelectWalletQuickCommand { get; }
    }
}
