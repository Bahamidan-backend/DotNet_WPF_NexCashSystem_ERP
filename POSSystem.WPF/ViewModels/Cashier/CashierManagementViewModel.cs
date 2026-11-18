using POSSystem.WPF.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using POSSystem.WPF.Business;
using POSSystem.WPF.Models.Helper;
using POSSystem.WPF.Shered.Commands;
using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.ViewModels.Reports;
using POSSystem.WPF.Data;
using POSSystem.WPF.Data.UnitOfWork;

namespace POSSystem.WPF.ViewModels.Cashier
{
    public partial class CashierManagementViewModel : BaseViewModel, POSSystem.WPF.Services.ISearchable
    {
        public CashierManagementViewModel()
        {
            var sp = POSSystem.WPF.App.ServiceProvider;
            _unitOfWork = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Data.UnitOfWork.IUnitOfWork>(sp);
            _reportService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IReportService>(sp);

            Commands = new CashierManagementCommands(this);
            _cashierSalesReports = new ObservableCollection<CashierSalesReport>();
            _dailySalesReports = new ObservableCollection<DailySalesReport>();
            _deliveryReports = new ObservableCollection<DeliveryReport>();

            _shiftPager = new Pagination<ShiftReport> { PageSize = 10 };
            CashBoxPager = new Pagination<CashBoxReport> { PageSize = 10 };

            cashierProberty = new CashierProberty();
            
            POSSystem.WPF.Helpers.TaskExtensions.RunSafe(async () =>
            {
                await LoadCashiersListAsync();
                await LoadCashierSalesReportAsync();
            });
        }

        public async Task LoadCashiersListAsync()
        {
            try
            {
                var users = await _unitOfWork.Users.GetAllAsync();
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _cashiers.Clear();
                    _cashiers.Add(new User { Id = 0, Name = "جميع الكاشيرات" });

                    // Only include cashier employees:
                    // 1. JobTitle is "كاشير" or contains "كاشير" or "cashier"
                    // 2. Or default system cashier user whose name/email contains "cashier"
                    var cashierUsers = users.Where(u =>
                        (!string.IsNullOrWhiteSpace(u.JobTitle) && (u.JobTitle.Contains("كاشير") || u.JobTitle.ToLower().Contains("cashier"))) ||
                        (!string.IsNullOrWhiteSpace(u.Name) && (u.Name.Contains("كاشير") || u.Name.ToLower().Contains("cashier"))) ||
                        (!string.IsNullOrWhiteSpace(u.Email) && u.Email.ToLower().Contains("cashier"))
                    ).OrderBy(u => u.Name);

                    foreach (var u in cashierUsers)
                    {
                        _cashiers.Add(u);
                    }
                    _selectedCashier = _cashiers.FirstOrDefault();
                    OnPropertyChanged(nameof(SelectedCashier));
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading cashiers: {ex.Message}");
            }
        }

        private void UpdateDateRange()
        {
            var today = DateTime.Today;
            switch (_periodType)
            {
                case ReportPeriodType.Daily:
                    SetDateRange(today, today.AddDays(1).AddTicks(-1));
                    break;
                case ReportPeriodType.Monthly:
                    SetDateRange(new DateTime(today.Year, today.Month, 1), 
                                 new DateTime(today.Year, today.Month, 1).AddMonths(1).AddTicks(-1));
                    break;
                case ReportPeriodType.Quarterly:
                    int quarter = (today.Month - 1) / 3 + 1;
                    SetDateRange(new DateTime(today.Year, (quarter - 1) * 3 + 1, 1), 
                                 new DateTime(today.Year, (quarter - 1) * 3 + 1, 1).AddMonths(3).AddTicks(-1));
                    break;
                case ReportPeriodType.Yearly:
                    SetDateRange(new DateTime(today.Year, 1, 1), new DateTime(today.Year, 12, 31, 23, 59, 59));
                    break;
            }
        }

        private void SetDateRange(DateTime start, DateTime end)
        {
            _startDate = start;
            _endDate = end;
            OnPropertyChanged(nameof(StartDate));
            OnPropertyChanged(nameof(EndDate));
            RefreshCurrentReport();
        }

        public async Task LoadDailySalesReportAsync()
        {
            await _dbSemaphore.WaitAsync();
            try
            {
                CurrentReportType = "Daily";
                _dailySalesReports.Clear();

                var results = await _reportService.GetDailySalesReportsAsync(StartDate, EndDate);
                foreach(var item in results) _dailySalesReports.Add(item);

                TotalDailySalesCountSum = results.Sum(r => r.InvoiceCount);
                TotalDailyRevenueSum = results.Sum(r => r.TotalRevenue);
                TotalDailyCashSalesSum = results.Sum(r => r.CashSales);
                TotalDailyCardSalesSum = results.Sum(r => r.CardSales);
                TotalDailyDebtSalesSum = results.Sum(r => r.DebtSales);

                OnPropertyChanged(nameof(DailySalesReports));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading daily sales report: {ex.Message}");
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        public async Task LoadCashierSalesReportAsync()
        {
            await _dbSemaphore.WaitAsync();
            try
            {
                CurrentReportType = "Sales";
                _cashierSalesReports.Clear();

                var results = (await _reportService.GetCashierSalesReportsAsync(StartDate, EndDate)).ToList();
                if (SelectedCashier != null && SelectedCashier.Id > 0)
                {
                    results = results.Where(r => r.UserId == SelectedCashier.Id).ToList();
                }
                if (!string.IsNullOrWhiteSpace(_searchQuery)) results = results.Where(r => r.CashierName != null && r.CashierName.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var item in results) _cashierSalesReports.Add(item);

                TotalCashierSalesCountSum = results.Sum(r => r.InvoiceCount);
                TotalCashierRevenueSum = results.Sum(r => r.TotalRevenue);
                TotalCashierCashSalesSum = results.Sum(r => r.CashSales);
                TotalCashierCardSalesSum = results.Sum(r => r.CardSales);
                TotalCashierDebtSalesSum = results.Sum(r => r.DebtSales);

                OnPropertyChanged(nameof(CashierSalesReports));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading cashier sales report: {ex.Message}");
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        public async Task LoadDeliveryReportAsync()
        {
            await _dbSemaphore.WaitAsync();
            try
            {
                CurrentReportType = "Delivery";
                _deliveryReports.Clear();
                
                var results = (await _reportService.GetDeliveryReportsAsync(StartDate, EndDate)).ToList();
                if (!string.IsNullOrWhiteSpace(_searchQuery)) results = results.Where(r => r.DeliveryPersonName != null && r.DeliveryPersonName.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var item in results) _deliveryReports.Add(item);

                OnPropertyChanged(nameof(DeliveryReports));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading delivery report: {ex.Message}");
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        public async Task LoadShiftReportAsync()
        {
            await _dbSemaphore.WaitAsync();
            try
            {
                CurrentReportType = "Shift";
                var results = (await _reportService.GetShiftReportsAsync(StartDate, EndDate)).ToList();
                if (SelectedCashier != null && SelectedCashier.Id > 0)
                {
                    results = results.Where(r => r.EmployeeName == SelectedCashier.Name).ToList();
                }
                _shiftPager.SetItems(results);

                TotalShiftSalesSum = results.Sum(r => r.TotalSales);
                TotalInvoiceCountSum = results.Sum(r => r.InvoiceCount);
                TotalReturnedOrdersCountSum = results.Sum(r => r.ReturnedOrdersCount);
                TotalReturnedOrdersAmountSum = results.Sum(r => r.ReturnedOrdersAmount);
                TotalDebtsSum = results.Sum(r => r.TotalDebtsCreated);
                TotalCashReceivedSum = results.Sum(r => r.CashReceived);

                OnPropertyChanged(nameof(ShiftReports));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading shift report: {ex.Message}");
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        public async Task LoadCashBoxReportAsync()
        {
            await _dbSemaphore.WaitAsync();
            try
            {
                CurrentReportType = "CashBox";
                var results = await _reportService.GetCashBoxReportsAsync(StartDate, EndDate);
                CashBoxPager.SetItems(results);
                OnPropertyChanged(nameof(CashBoxReports));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading cash box report: {ex.Message}");
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        public async Task LoadAuditReportAsync()
        {
            await _dbSemaphore.WaitAsync();
            try
            {
                CurrentReportType = "Audit";
                int? cashierId = SelectedCashier != null && SelectedCashier.Id > 0 ? SelectedCashier.Id : null;
                var summary = await _reportService.GetCashierAuditSummaryAsync(StartDate, EndDate, cashierId);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AuditSummary = summary;
                    _auditDiscounts.Clear();
                    foreach (var d in summary.Discounts) _auditDiscounts.Add(d);

                    _auditCancellations.Clear();
                    foreach (var c in summary.Cancellations) _auditCancellations.Add(c);

                    _auditLeftoverDisposals.Clear();
                    foreach (var ld in summary.LeftoverDisposals) _auditLeftoverDisposals.Add(ld);

                    _auditCashierRankings.Clear();
                    foreach (var r in summary.CashierRankings) _auditCashierRankings.Add(r);

                    OnPropertyChanged(nameof(AuditSummary));
                    OnPropertyChanged(nameof(AuditDiscounts));
                    OnPropertyChanged(nameof(AuditCancellations));
                    OnPropertyChanged(nameof(AuditLeftoverDisposals));
                    OnPropertyChanged(nameof(AuditCashierRankings));
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading audit report: {ex.Message}");
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        private string _searchQuery = string.Empty;

        public void ApplySearch(string query)
        {
            _searchQuery = query?.Trim() ?? string.Empty;
            RefreshCurrentReport();
        }

        public async void RefreshCurrentReport()
        {
            switch (CurrentReportType)
            {
                case "Sales": await LoadCashierSalesReportAsync(); break;
                case "Daily": await LoadDailySalesReportAsync(); break;
                case "Delivery": await LoadDeliveryReportAsync(); break;
                case "Shift": await LoadShiftReportAsync(); break;
                case "CashBox": await LoadCashBoxReportAsync(); break;
                case "Audit": await LoadAuditReportAsync(); break;
            }
        }
    }
}
