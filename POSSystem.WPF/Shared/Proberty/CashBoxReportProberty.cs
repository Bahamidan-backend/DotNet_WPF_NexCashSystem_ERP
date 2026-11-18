using POSSystem.WPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.Shered.Proberty
{
    public class CashBoxReportProberty:BaseModel
    {



       public string _shiftNumber = "-";
       public string _shiftType = "-";
       public DateTime? _shiftStartTime;
       public DateTime? _shiftEndTime;
       public decimal _startingCash;
       public decimal _currentCashInBox;




        public int _totalInvoices;
        public decimal _totalSales;
        public decimal _cashSales;
        public decimal _cardSales;
        public decimal _creditSales;
        public decimal _averageInvoice;

        // Discounts & Coupons
        private int _discountsUsed;
        private decimal _totalDiscountAmount;

        // Addons
        private int _addonsSold;
        private decimal _addonsRevenue;

        // Customer Debts
        private int _customerDebtsCount;
        private decimal _totalCustomerDebts;

        // Cash Box Transactions
        private int _cashBoxTransactionsCount;
        private decimal _totalCashOut;
        private bool _hasCashOut;
        private string _cashOutStatus = "لم يتم أخذ أي مبلغ من الصندوق";



        public Shift? _currentShift;
        public ObservableCollection<CashBoxTransaction> _cashBoxTransactions;
        public ObservableCollection<Order> _orders;





        //public bool HasTransactions => _transactionDetails.Count > 0;
        //public bool NoTransactions => _transactionDetails.Count == 0;

        // Shift Properties
        public string ShiftNumber
        {
            get => _shiftNumber;
            set => SetProperty(ref _shiftNumber, value);
        }

        public string ShiftType
        {
            get => _shiftType;
            set => SetProperty(ref _shiftType, value);
        }

        public string ShiftStartTime => _shiftStartTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
        public string ShiftEndTime => _shiftEndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";

        public DateTime? ShiftStartTimeDate
        {
            get => _shiftStartTime;
            set
            {
                if (SetProperty(ref _shiftStartTime, value))
                    OnPropertyChanged(nameof(ShiftStartTime));
            }
        }

        public DateTime? ShiftEndTimeDate
        {
            get => _shiftEndTime;
            set
            {
                if (SetProperty(ref _shiftEndTime, value))
                    OnPropertyChanged(nameof(ShiftEndTime));
            }
        }

        public decimal StartingCash
        {
            get => _startingCash;
            set => SetProperty(ref _startingCash, value);
        }

        public decimal CurrentCashInBox
        {
            get => _currentCashInBox;
            set => SetProperty(ref _currentCashInBox, value);
        }

        // Sales Properties
        public int TotalInvoices
        {
            get => _totalInvoices;
            set => SetProperty(ref _totalInvoices, value);
        }

        public decimal TotalSales
        {
            get => _totalSales;
            set => SetProperty(ref _totalSales, value);
        }

        public decimal CashSales
        {
            get => _cashSales;
            set => SetProperty(ref _cashSales, value);
        }

        public decimal CardSales
        {
            get => _cardSales;
            set => SetProperty(ref _cardSales, value);
        }

        public decimal CreditSales
        {
            get => _creditSales;
            set => SetProperty(ref _creditSales, value);
        }

        public decimal AverageInvoice
        {
            get => _averageInvoice;
            set => SetProperty(ref _averageInvoice, value);
        }

        // Discounts Properties
        public int DiscountsUsed
        {
            get => _discountsUsed;
            set => SetProperty(ref _discountsUsed, value);
        }

        public decimal TotalDiscountAmount
        {
            get => _totalDiscountAmount;
            set => SetProperty(ref _totalDiscountAmount, value);
        }

        // Addons Properties
        public int AddonsSold
        {
            get => _addonsSold;
            set => SetProperty(ref _addonsSold, value);
        }

        public decimal AddonsRevenue
        {
            get => _addonsRevenue;
            set => SetProperty(ref _addonsRevenue, value);
        }

        // Customer Debts Properties
        public int CustomerDebtsCount
        {
            get => _customerDebtsCount;
            set => SetProperty(ref _customerDebtsCount, value);
        }

        public decimal TotalCustomerDebts
        {
            get => _totalCustomerDebts;
            set => SetProperty(ref _totalCustomerDebts, value);
        }

        // Cash Box Properties
        public int CashBoxTransactionsCount
        {
            get => _cashBoxTransactionsCount;
            set => SetProperty(ref _cashBoxTransactionsCount, value);
        }

        public decimal TotalCashOut
        {
            get => _totalCashOut;
            set => SetProperty(ref _totalCashOut, value);
        }

        public bool HasCashOut
        {
            get => _hasCashOut;
            set
            {
                SetProperty(ref _hasCashOut, value);
                OnPropertyChanged(nameof(CashOutStatus));
            }
        }

        private string? _customCashOutStatus;
        public string CashOutStatus
        {
            get => !string.IsNullOrEmpty(_customCashOutStatus)
                ? _customCashOutStatus
                : (_hasCashOut ? $"تم أخذ {_totalCashOut:N0} ريال من الصندوق" : "لم يتم أخذ أي مبلغ من الصندوق");
            set
            {
                SetProperty(ref _customCashOutStatus, value);
            }
        }

        public ObservableCollection<CashBoxTransaction> CashBoxTransactions => _cashBoxTransactions;
        public ObservableCollection<Order> Orders => _orders;

        // Deposit Breakdown by Currency
        private decimal _depositTotalYer;
        private int _depositCountYer;
        private decimal _depositTotalSar;
        private int _depositCountSar;
        private decimal _depositTotalUsd;
        private int _depositCountUsd;
        private int _depositTotalOrdersCount;

        public decimal DepositTotalYer
        {
            get => _depositTotalYer;
            set => SetProperty(ref _depositTotalYer, value);
        }

        public int DepositCountYer
        {
            get => _depositCountYer;
            set => SetProperty(ref _depositCountYer, value);
        }

        public decimal DepositTotalSar
        {
            get => _depositTotalSar;
            set => SetProperty(ref _depositTotalSar, value);
        }

        public int DepositCountSar
        {
            get => _depositCountSar;
            set => SetProperty(ref _depositCountSar, value);
        }

        public decimal DepositTotalUsd
        {
            get => _depositTotalUsd;
            set => SetProperty(ref _depositTotalUsd, value);
        }

        public int DepositCountUsd
        {
            get => _depositCountUsd;
            set => SetProperty(ref _depositCountUsd, value);
        }

        public int DepositTotalOrdersCount
        {
            get => _depositTotalOrdersCount;
            set
            {
                SetProperty(ref _depositTotalOrdersCount, value);
                OnPropertyChanged(nameof(HasDeposits));
            }
        }

        public bool HasDeposits => _depositTotalOrdersCount > 0;

        public ObservableCollection<Models.WalletDepositSummary> WalletSummaries { get; } = new();
        public bool HasWalletBreakdown => WalletSummaries.Count > 0;








    }
}
