using POSSystem.WPF.Helpers;
using System.Collections.ObjectModel;
using POSSystem.WPF.Models;
using POSSystem.WPF.Services;
using POSSystem.WPF.Business.Services.interfase;

namespace POSSystem.WPF.ViewModels.Customers
{
    public partial class CustomerManagementViewModel
    {
        private readonly ICustomerDebtService _customerDebtService;
        private readonly IOrderService _orderService;
        private readonly IShiftService _shiftService;
        private POSSystem.WPF.Helpers.ObservableRangeCollection<CustomerDebt> _debts;
        private POSSystem.WPF.Helpers.ObservableRangeCollection<CustomerDebt> _unpaidDebts;
        private CustomerDebt? _selectedDebt;
        private string _searchText = string.Empty;
        private bool _showUnpaidOnly = true;

        public POSSystem.WPF.Helpers.ObservableRangeCollection<CustomerDebt> Debts => _debts;
        public POSSystem.WPF.Helpers.ObservableRangeCollection<CustomerDebt> UnpaidDebts => _unpaidDebts;

        public Pagination<CustomerDebt> DebtsPager { get; } = new();

        public CustomerDebt? SelectedDebt
        {
            get => _selectedDebt;
            set => SetProperty(ref _selectedDebt, value);
        }

        private readonly POSSystem.WPF.Helpers.Debouncer _searchDebouncer = new POSSystem.WPF.Helpers.Debouncer(500);

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                _searchDebouncer.RunAsync(() => SearchDebtsAsync()).SafeFireAndForget();
            }
        }

        public bool ShowUnpaidOnly
        {
            get => _showUnpaidOnly;
            set
            {
                SetProperty(ref _showUnpaidOnly, value);
                LoadDebtsAsync().SafeFireAndForget();
            }
        }

        private decimal _totalUnpaidDebts;
        private decimal _totalPaidDebts;
        private int _unpaidDebtsCount;
        private int _oldDebtsCount;

        public decimal TotalUnpaidDebts
        {
            get => _totalUnpaidDebts;
            private set => SetProperty(ref _totalUnpaidDebts, value);
        }

        public decimal TotalPaidDebts
        {
            get => _totalPaidDebts;
            private set => SetProperty(ref _totalPaidDebts, value);
        }

        public int UnpaidDebtsCount
        {
            get => _unpaidDebtsCount;
            private set => SetProperty(ref _unpaidDebtsCount, value);
        }

        public int OldDebtsCount
        {
            get => _oldDebtsCount;
            private set => SetProperty(ref _oldDebtsCount, value);
        }
    }
}
