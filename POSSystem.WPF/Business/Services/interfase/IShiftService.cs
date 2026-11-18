using POSSystem.WPF.Models.Helper;

namespace POSSystem.WPF.Business.Services.interfase
{
    public interface IShiftService
    {
        Task<Shift?> GetActiveShiftByUserIdAsync(int userId);
        Task<Shift> StartShiftAsync(int userId, ShiftType type, decimal startingCash, string? notes = null);
        Task<Shift> EndShiftAsync(int shiftId, decimal endingCash);
        Task<bool> CanUserWorkAsync(int userId);

        event EventHandler ShiftStarted;
        event EventHandler ShiftEnded;
        Task<Shift?> GetCurrentShiftAsync(int userId);
        Task<Shift?> LoadCurrentOrLastShiftAsync(User _currentUser);
        Task<Shift?> GetLastClosedShiftAsync();
        Task LoadCashBoxTransactionsByShiftAsync(IEnumerable<CashBoxTransaction> transactions, ObservableCollection<CashBoxTransaction> _cashBoxTransactions, ObservableCollection<CashBoxTransactionDetail> _transactionDetails);

        Task<IEnumerable<Order>> GetShiftTransactionsAsync(Shift _currentShift, User _currentUser, IOrderService _orderService);

        //Task LoadReportAsync(Shift? _currentShift, User _currentUser, string ShiftNumber, string ShiftType, DateTime? _shiftStartTime, DateTime? _shiftEndTime, decimal StartingCash, ObservableCollection<CashBoxTransaction> _cashBoxTransactions, ObservableCollection<CashBoxTransactionDetail> _transactionDetails);

    }
}
