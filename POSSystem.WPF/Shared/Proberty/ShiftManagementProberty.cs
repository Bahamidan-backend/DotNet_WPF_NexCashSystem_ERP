using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Shered.Proberty
{
    public class ShiftManagementProberty : BaseModel
    {
        private ShiftType _selectedShiftType = ShiftType.Morning;
        private decimal _startingCash;
        private decimal _endingCash;
        private string _shiftNotes = "";
        public decimal _withdrawAmount;
        public string _withdrawDescription = "";
        private int _withdrawalType = 0; // 0 = Personal, 1 = Restaurant Expense
        private ObservableCollection<User> _employees = new();
        private User? _selectedEmployee;

        private string _depositBankName = "حاسب كريمي";
        private string _depositReferenceNumber = "";

        public ObservableCollection<User> Employees
        {
            get => _employees;
            set => SetProperty(ref _employees, value);
        }

        public User? SelectedEmployee
        {
            get => _selectedEmployee;
            set => SetProperty(ref _selectedEmployee, value);
        }

        public ShiftType SelectedShiftType
        {
            get => _selectedShiftType;
            set
            {
                SetProperty(ref _selectedShiftType, value);
                OnPropertyChanged(nameof(IsMorningSelected));
                OnPropertyChanged(nameof(IsEveningSelected));
            }
        }

        public bool IsMorningSelected => SelectedShiftType == ShiftType.Morning;
        public bool IsEveningSelected => SelectedShiftType == ShiftType.Evening;

        public decimal StartingCash
        {
            get => _startingCash;
            set => SetProperty(ref _startingCash, value);
        }

        public decimal EndingCash
        {
            get => _endingCash;
            set => SetProperty(ref _endingCash, value);
        }

        public string ShiftNotes
        {
            get => _shiftNotes;
            set => SetProperty(ref _shiftNotes, value);
        }

        public decimal WithdrawAmount
        {
            get => _withdrawAmount;
            set => SetProperty(ref _withdrawAmount, value);
        }

        public string WithdrawDescription
        {
            get => _withdrawDescription;
            set => SetProperty(ref _withdrawDescription, value);
        }

        public bool IsPersonalWithdrawal
        {
            get => _withdrawalType == 0;
            set
            {
                if (value)
                {
                    _withdrawalType = 0;
                    NotifyWithdrawalTypes();
                }
            }
        }

        public bool IsRestaurantWithdrawal
        {
            get => _withdrawalType == 1;
            set
            {
                if (value)
                {
                    _withdrawalType = 1;
                    NotifyWithdrawalTypes();
                }
            }
        }

        public bool IsBankDepositWithdrawal => false;

        private void NotifyWithdrawalTypes()
        {
            OnPropertyChanged(nameof(IsPersonalWithdrawal));
            OnPropertyChanged(nameof(IsRestaurantWithdrawal));
            OnPropertyChanged(nameof(IsBankDepositWithdrawal));
            OnPropertyChanged(nameof(WithdrawalActionButtonText));
        }

        public string WithdrawalActionButtonText
        {
            get
            {
                if (IsRestaurantWithdrawal) return "🏢 تأكيد سحب مصروف للمطعم";
                return "👤 تأكيد سحب دين شخصي";
            }
        }

        public string DepositBankName
        {
            get => _depositBankName;
            set => SetProperty(ref _depositBankName, value);
        }

        public string DepositReferenceNumber
        {
            get => _depositReferenceNumber;
            set => SetProperty(ref _depositReferenceNumber, value);
        }

        public ObservableCollection<string> DepositBanksList { get; } = new(POSSystem.WPF.Helpers.FinancialChannelHelper.GetActiveWalletsList());

        public void RefreshDepositBanksList()
        {
            var current = EndShiftBankName;
            DepositBanksList.Clear();
            foreach (var w in POSSystem.WPF.Helpers.FinancialChannelHelper.GetActiveWalletsList())
            {
                DepositBanksList.Add(w);
            }
            if (!string.IsNullOrWhiteSpace(current) && DepositBanksList.Contains(current))
                EndShiftBankName = current;
            else if (DepositBanksList.Count > 0)
                EndShiftBankName = DepositBanksList[0];
        }

        // --- خصائص التوريد والإيداع البنكي عند إنهاء المناوبة ---
        private bool _isDepositOnEndShift = true;
        public bool IsDepositOnEndShift
        {
            get => _isDepositOnEndShift;
            set => SetProperty(ref _isDepositOnEndShift, value);
        }

        private string _endShiftBankName = "حاسب كريمي";
        public string EndShiftBankName
        {
            get => _endShiftBankName;
            set => SetProperty(ref _endShiftBankName, value);
        }

        private string _endShiftDepositReference = "";
        public string EndShiftDepositReference
        {
            get => _endShiftDepositReference;
            set => SetProperty(ref _endShiftDepositReference, value);
        }

        private string _endShiftDepositorName = "";
        public string EndShiftDepositorName
        {
            get => _endShiftDepositorName;
            set => SetProperty(ref _endShiftDepositorName, value);
        }

        private decimal _endShiftDepositAmount;
        public decimal EndShiftDepositAmount
        {
            get => _endShiftDepositAmount;
            set => SetProperty(ref _endShiftDepositAmount, value);
        }
    }
}
