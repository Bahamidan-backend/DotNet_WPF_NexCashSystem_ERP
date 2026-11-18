using POSSystem.WPF.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using POSSystem.WPF.Business;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Models;
using POSSystem.WPF.Models.Helper;
using POSSystem.WPF.Shered.Proberty;

namespace POSSystem.WPF.ViewModels.Cashier
{
    public partial class CashBoxReportViewModel : BaseViewModel
    {
        public CashBoxReportViewModel(User user)
        {
            _currentUser = user;
            var sp = POSSystem.WPF.App.ServiceProvider;
            _reportService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IReportService>(sp);

            _cashBoxTransactions = new ObservableCollection<CashBoxTransaction>();
            _orders = new ObservableCollection<Order>();
            _transactionDetails = new ObservableCollection<CashBoxTransactionDetail>();

            TransactionsPager = new Pagination<CashBoxTransactionDetail>
            {
                PageSize = 6
            };

            RefreshReportCommand = new RelayCommand(async () => await LoadReportAsync());
            CashBoxReportProberty = new CashBoxReportProberty();

            Application.Current.Dispatcher.InvokeAsync(async () => await LoadReportAsync()).Task.SafeFireAndForget();

            _returnOrderService = ServiceLocator.ReturnOrderService;
            if (_returnOrderService != null)
            {
                _returnOrderService.ReturnProcessed += OnReturnProcessed;
            }

            _shiftService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IShiftService>(sp);
            if (_shiftService != null)
            {
                _shiftService.ShiftStarted += OnShiftChanged;
                _shiftService.ShiftEnded += OnShiftChanged;
            }
        }

        private void OnReturnProcessed(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(async () => {
                await LoadReportAsync();
            });
        }

        private void OnShiftChanged(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(async () => {
                await LoadReportAsync();
            });
        }

        public override void Dispose()
        {
            if (_isDisposed) return;
            
            if (_returnOrderService != null)
            {
                _returnOrderService.ReturnProcessed -= OnReturnProcessed;
            }

            if (_shiftService != null)
            {
                _shiftService.ShiftStarted -= OnShiftChanged;
                _shiftService.ShiftEnded -= OnShiftChanged;
            }

            base.Dispose();
        }

        public async Task LoadReportAsync()
        {
            try
            {
                var reportDto = await _reportService.GetUserShiftReportAsync(_currentUser);

                _currentShift = reportDto.Shift;

                if (_currentShift != null && _currentShift.IsActive)
                {
                    CashBoxReportProberty.ShiftNumber = _currentShift.ShiftNumber;
                    CashBoxReportProberty.ShiftType = _currentShift.TypeText;
                    CashBoxReportProberty.ShiftStartTimeDate = _currentShift.StartTime;
                    CashBoxReportProberty.ShiftEndTimeDate = _currentShift.EndTime;
                    CashBoxReportProberty.StartingCash = _currentShift.StartingCash;
                    CashBoxReportProberty.CurrentCashInBox = reportDto.CurrentCashInBox;
                    CashBoxReportProberty.CashOutStatus = null; // resets to standard calculation

                    _cashBoxTransactions.Clear();
                    foreach (var t in reportDto.Transactions) _cashBoxTransactions.Add(t);

                    _transactionDetails.Clear();
                    foreach (var td in reportDto.TransactionDetails) _transactionDetails.Add(td);

                    _orders.Clear();
                    foreach (var o in reportDto.Orders) _orders.Add(o);

                    TransactionsPager.SetItems(_transactionDetails);

                    OnPropertyChanged(nameof(TransactionDetails));
                    OnPropertyChanged(nameof(HasTransactions));
                    OnPropertyChanged(nameof(NoTransactions));

                    CashBoxReportProberty.CashBoxTransactionsCount = reportDto.CashBoxTransactionsCount;
                    CashBoxReportProberty.TotalCashOut = reportDto.TotalCashOut;
                    CashBoxReportProberty.HasCashOut = reportDto.HasCashOut;

                    CashBoxReportProberty.TotalInvoices = reportDto.TotalInvoices;
                    CashBoxReportProberty.TotalSales = reportDto.TotalSales;
                    CashBoxReportProberty.CashSales = reportDto.CashSales;
                    CashBoxReportProberty.CardSales = reportDto.CardSales;
                    CashBoxReportProberty.CreditSales = reportDto.CreditSales;
                    CashBoxReportProberty.AverageInvoice = reportDto.AverageInvoice;

                    CashBoxReportProberty.DiscountsUsed = reportDto.DiscountsUsed;
                    CashBoxReportProberty.TotalDiscountAmount = reportDto.TotalDiscountAmount;

                    CashBoxReportProberty.AddonsRevenue = reportDto.AddonsRevenue;
                    CashBoxReportProberty.AddonsSold = reportDto.AddonsSold;

                    CashBoxReportProberty.CustomerDebtsCount = reportDto.CustomerDebtsCount;
                    CashBoxReportProberty.TotalCustomerDebts = reportDto.TotalCustomerDebts;

                    // Calculate Deposit Details by Currency
                    decimal depYer = 0; int countYer = 0;
                    decimal depSar = 0; int countSar = 0;
                    decimal depUsd = 0; int countUsd = 0;
                    int totalDepositOrders = 0;

                    foreach (var order in reportDto.Orders.Where(o => o.PaymentMethod == PaymentMethod.MobilePayment || (!string.IsNullOrWhiteSpace(o.Notes) && o.Notes.Contains("[إيداع:"))))
                    {
                        totalDepositOrders++;
                        var info = OrderDepositHelper.ParseDepositInfo(order.Notes);
                        decimal amt = info != null && info.Amount > 0 ? info.Amount : order.Total;
                        string curr = info != null ? info.Currency : "ريال يمني";

                        if (curr.Contains("يمني"))
                        {
                            depYer += amt;
                            countYer++;
                        }
                        else if (curr.Contains("سعودي"))
                        {
                            depSar += amt;
                            countSar++;
                        }
                        else if (curr.Contains("دولار"))
                        {
                            depUsd += amt;
                            countUsd++;
                        }
                        else
                        {
                            depYer += amt;
                            countYer++;
                        }
                    }

                    CashBoxReportProberty.DepositTotalYer = depYer;
                    CashBoxReportProberty.DepositCountYer = countYer;
                    CashBoxReportProberty.DepositTotalSar = depSar;
                    CashBoxReportProberty.DepositCountSar = countSar;
                    CashBoxReportProberty.DepositTotalUsd = depUsd;
                    CashBoxReportProberty.DepositCountUsd = countUsd;
                    CashBoxReportProberty.DepositTotalOrdersCount = totalDepositOrders;
                }
                else
                {
                    _currentShift = null;
                    CashBoxReportProberty.ShiftNumber = "لا توجد مناوبة نشطة";
                    CashBoxReportProberty.ShiftType = "-";
                    CashBoxReportProberty.ShiftStartTimeDate = null;
                    CashBoxReportProberty.ShiftEndTimeDate = null;

                    CashBoxReportProberty.StartingCash = 0;
                    CashBoxReportProberty.CurrentCashInBox = 0;
                    CashBoxReportProberty.TotalInvoices = 0;
                    CashBoxReportProberty.TotalSales = 0;
                    CashBoxReportProberty.CashSales = 0;
                    CashBoxReportProberty.CardSales = 0;
                    CashBoxReportProberty.CreditSales = 0;
                    CashBoxReportProberty.AverageInvoice = 0;
                    CashBoxReportProberty.TotalCashOut = 0;
                    CashBoxReportProberty.HasCashOut = false;
                    CashBoxReportProberty.CashOutStatus = "🔒 الصندوق مقفل - لا توجد مناوبة نشطة حالياً. يرجى فتح مناوبة جديدة.";

                    CashBoxReportProberty.DiscountsUsed = 0;
                    CashBoxReportProberty.TotalDiscountAmount = 0;
                    CashBoxReportProberty.AddonsRevenue = 0;
                    CashBoxReportProberty.AddonsSold = 0;
                    CashBoxReportProberty.CustomerDebtsCount = 0;
                    CashBoxReportProberty.TotalCustomerDebts = 0;
                    CashBoxReportProberty.CashBoxTransactionsCount = 0;

                    CashBoxReportProberty.DepositTotalYer = 0;
                    CashBoxReportProberty.DepositCountYer = 0;
                    CashBoxReportProberty.DepositTotalSar = 0;
                    CashBoxReportProberty.DepositCountSar = 0;
                    CashBoxReportProberty.DepositTotalUsd = 0;
                    CashBoxReportProberty.DepositCountUsd = 0;
                    CashBoxReportProberty.DepositTotalOrdersCount = 0;

                    _cashBoxTransactions.Clear();
                    _transactionDetails.Clear();
                    _orders.Clear();
                    TransactionsPager.SetItems(_transactionDetails);

                    OnPropertyChanged(nameof(TransactionDetails));
                    OnPropertyChanged(nameof(HasTransactions));
                    OnPropertyChanged(nameof(NoTransactions));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل التقرير: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
