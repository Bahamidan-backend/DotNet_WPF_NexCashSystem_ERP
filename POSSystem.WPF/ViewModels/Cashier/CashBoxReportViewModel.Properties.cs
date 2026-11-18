using System.Collections.ObjectModel;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Models;
using POSSystem.WPF.Models.Helper;
using POSSystem.WPF.Shered.Proberty;

namespace POSSystem.WPF.ViewModels.Cashier
{
    public partial class CashBoxReportViewModel
    {
        private readonly User _currentUser;
        private readonly IReportService _reportService;
        private readonly POSSystem.WPF.Business.Services.IReturnOrderService _returnOrderService;
        private readonly POSSystem.WPF.Business.Services.interfase.IShiftService _shiftService;

        private Shift? _currentShift;
        private ObservableCollection<CashBoxTransaction> _cashBoxTransactions;
        private ObservableCollection<Order> _orders;
        private ObservableCollection<CashBoxTransactionDetail> _transactionDetails;

        public CashBoxReportProberty CashBoxReportProberty { get; }
        public Pagination<CashBoxTransactionDetail> TransactionsPager { get; }

        public ObservableCollection<CashBoxTransaction> CashBoxTransactions => _cashBoxTransactions;
        public ObservableCollection<Order> Orders => _orders;
        public ObservableCollection<CashBoxTransactionDetail> TransactionDetails => TransactionsPager.PagedItems;

        public bool HasTransactions => TransactionsPager.HasItems;
        public bool NoTransactions => TransactionsPager.NoItems;
    }
}
