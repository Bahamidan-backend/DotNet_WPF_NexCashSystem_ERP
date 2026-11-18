using POSSystem.WPF.Models;
using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Business.Services.interfase;

namespace POSSystem.WPF.ViewModels.Cashier
{
    public partial class ShiftManagementViewModel
    {
        private readonly IShiftService _shiftService;
        private readonly ICashBoxTransactionService _cashBoxTransactionService;
        private readonly IUserService _userService;
        private readonly IReportService _reportServiceForShift;
        private readonly IEmployeeService _employeeService;
        private readonly IBankDepositService? _bankDepositService;
        private readonly int _userId;

        private Shift? _currentShift;
        
        public ShiftManagementProberty shiftManagementProberty { get; }
        public ShiftManagementCommands Commands { get; }

        public Shift? CurrentShift
        {
            get => _currentShift;
            set
            {
                SetProperty(ref _currentShift, value);
                OnPropertyChanged(nameof(ShowStartShiftSection));
                OnPropertyChanged(nameof(ShowActiveShiftSection));
                OnPropertyChanged(nameof(ActionButtonText));
                OnPropertyChanged(nameof(ShiftStatusText));
                LoadCashBoxBalance();
            }
        }

        public bool ShowStartShiftSection => CurrentShift == null;
        public bool ShowActiveShiftSection => CurrentShift != null && CurrentShift.IsActive;

        public string ActionButtonText
        {
            get
            {
                if (CurrentShift == null) return "بدء المناوبة";
                if (shiftManagementProberty != null && shiftManagementProberty.IsDepositOnEndShift)
                    return "🏦 إنهاء المناوبة وتوريد الكاش للبنك";
                return "إنهاء المناوبة";
            }
        }
        
        public string ShiftStatusText => CurrentShift == null ? "لا توجد مناوبة نشطة" : $"مناوبة نشطة - {CurrentShift.TypeText}";

        public decimal CurrentCashInBox { get; private set; }
    }
}
