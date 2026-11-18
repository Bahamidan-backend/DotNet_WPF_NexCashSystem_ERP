using POSSystem.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using POSSystem.WPF.Business.Helpers;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;
using POSSystem.WPF.Models.Helper;
using POSSystem.WPF.Shered.Proberty;

namespace POSSystem.WPF.ViewModels.Reports
{
    public enum ReportPeriodType
    {
        Daily,
        Monthly,
        Quarterly,
        Yearly
    }

    public partial class GeneralReportsViewModel : BaseViewModel
    {
        public GeneralReportsViewModel()
        {
            var sp = POSSystem.WPF.App.ServiceProvider;
            _unitOfWork = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Data.UnitOfWork.IUnitOfWork>(sp);
            _saleService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.ISaleService>(sp);
            _expenseService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IExpenseService>(sp);
            _employeeService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IEmployeeService>(sp);
            _customerDebtService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.ICustomerDebtService>(sp);
            _purchaseService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IPurchaseService>(sp);
            _orderService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IOrderService>(sp);
            _shiftService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IShiftService>(sp);
            _cashBoxTransactionService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.ICashBoxTransactionService>(sp);
            _warehouseService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IWarehouseService>(sp);
            var userService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IUserService>(sp);
            var addonService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IAddonService>(sp);
            _reportService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IReportService>(sp);
            _financialEngineService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IFinancialEngineService>(sp);

            generalReportProberty = new GeneralReportProberty();

            GenerateReportCommand = new RelayCommand(async () => await GenerateReportAsync());
            ExportReportCommand = new RelayCommand(() => ExportReport());

            GenerateReportAsync().SafeFireAndForget();
        }

        private (DateTime startDate, DateTime endDate) GetDateRange()
        {
            return _periodType switch
            {
                ReportPeriodType.Daily => (ReportDate.Date, ReportDate.Date.AddDays(1).AddTicks(-1)),
                ReportPeriodType.Monthly => (
                    new DateTime(ReportDate.Year, ReportDate.Month, 1),
                    new DateTime(ReportDate.Year, ReportDate.Month, 1).AddMonths(1).AddTicks(-1)
                ),
                ReportPeriodType.Yearly => (
                    new DateTime(ReportDate.Year, 1, 1),
                    new DateTime(ReportDate.Year, 12, 31, 23, 59, 59)
                ),
                _ => (ReportDate.Date, ReportDate.Date.AddDays(1).AddTicks(-1))
            };
        }

        private System.Threading.CancellationTokenSource? _reportCts;

        private async Task GenerateReportAsync()
        {
            _reportCts?.Cancel();
            _reportCts = new System.Threading.CancellationTokenSource();
            var token = _reportCts.Token;

            generalReportProberty.IsLoading = true;
            try
            {
                var (startDate, endDate) = GetDateRange();

                // FIX ISSUE-ARCH-001: Execute report queries sequentially to prevent concurrent operations on the shared EF Core POSDbContext instance
                await LoadShiftReportsAsync(startDate, endDate);
                await LoadCashBoxReportsAsync(startDate, endDate);
                await LoadFinancialStatementAsync(startDate, endDate);
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine($"Error generating report: {ex.Message}\n{ex.StackTrace}");
                    MessageBox.Show("حدث خطأ أثناء توليد التقرير. يرجى المحاولة مرة أخرى.", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                if (!token.IsCancellationRequested)
                    generalReportProberty.IsLoading = false;
            }
        }



        private async Task LoadShiftReportsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                generalReportProberty.ShiftReports.Clear();

                var shifts = await _unitOfWork.Shifts.GetShiftsByDateRangeAsync(startDate, endDate);
                var closedShiftNumbers = shifts
                    .Where(s => s != null && !s.IsActive && !string.IsNullOrEmpty(s.ShiftNumber))
                    .Select(s => s.ShiftNumber!)
                    .ToHashSet();

                if (closedShiftNumbers.Count == 0)
                {
                    generalReportProberty.RefreshShiftSummary();
                    OnPropertyChanged(nameof(generalReportProberty.ShiftReports));
                    return;
                }

                var allShiftReports = await _reportService.GetShiftReportsAsync(startDate, endDate);
                foreach (var report in allShiftReports.Where(r => closedShiftNumbers.Contains(r.ShiftNumber)))
                    generalReportProberty.ShiftReports.Add(report);

                generalReportProberty.RefreshShiftSummary();
                OnPropertyChanged(nameof(generalReportProberty.ShiftReports));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadShiftReportsAsync: {ex.Message}");
            }
        }

        private async Task LoadCashBoxReportsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                generalReportProberty.CashBoxReports.Clear();

                var transactions = await _cashBoxTransactionService.GetTransactionsByDateRangeAsync(startDate, endDate);

                var expenseTransactions = transactions?
                    .Where(t => t != null && (t.Type == CashBoxTransactionType.Expense || 
                                              t.Type == CashBoxTransactionType.Advance || 
                                              t.Type == CashBoxTransactionType.Debt))
                    .ToList() ?? new List<CashBoxTransaction>();

                var groupedByUserAndDate = expenseTransactions
                    .GroupBy(t => new { t.UserId, Date = t.TransactionDate.Date })
                    .ToList();

                foreach (var group in groupedByUserAndDate)
                {
                    var user = group.First().User;
                    var date = group.Key.Date;

                    var totalAmount = group.Sum(t => t.Amount);
                    var transactionCount = group.Count();
                    var firstTransaction = group.OrderBy(t => t.TransactionDate).First();
                    var lastTransaction = group.OrderByDescending(t => t.TransactionDate).First();

                    generalReportProberty.CashBoxReports.Add(new CashBoxTransactionReport
                    {
                        EmployeeName = user?.Name ?? "غير محدد",
                        TransactionDate = date,
                        FirstTransactionTime = firstTransaction.TransactionDate,
                        LastTransactionTime = lastTransaction.TransactionDate,
                        TotalAmount = totalAmount,
                        TransactionCount = transactionCount,
                        Description = firstTransaction.Description ?? "سحب من الصندوق"
                    });
                }

                OnPropertyChanged(nameof(generalReportProberty.CashBoxReports));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadCashBoxReportsAsync: {ex.Message}");
            }
        }

        private static decimal GetSizePortionMultiplier(string? size)
        {
            if (string.IsNullOrWhiteSpace(size)) return 1m;
            var s = size.Trim();
            if (s.Contains("نص") && !s.Contains("ونص")) return 0.5m;
            if (s.Contains("ونص")) return 1.5m;
            if (s.Contains("ربع")) return 0.25m;
            if (s.Contains("ثمن")) return 0.125m;
            if (s.Contains("نفرين") || s.Contains("حبتين")) return 2m;
            return 1m;
        }

        private static string InferProductUnitLabel(string productName, IEnumerable<string> sizes)
        {
            var sizeList = sizes.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (sizeList.Any(s => s.Contains("حبة"))) return "حبة";
            if (sizeList.Any(s => s.Contains("نفر"))) return "نفر";
            if (sizeList.Any(s => s.Contains("ذبيحة"))) return "ذبيحة";
            var name = productName ?? string.Empty;
            if (name.Contains("دجاج") || name.Contains("شواية") || name.Contains("مظبي") || name.Contains("فحم") || name.Contains("زبادي") || name.Contains("ماء") || name.Contains("مشروب") || name.Contains("بيبسي") || name.Contains("عصير"))
                return "حبة";
            if (name.Contains("رز") || name.Contains("أرز") || name.Contains("مندي") || name.Contains("مضغوط") || name.Contains("زربيان") || name.Contains("لحم") || name.Contains("إيدام") || name.Contains("سلطة"))
                return "نفر";
            return "وحدة / حبة";
        }

        private static string ResolveDepartmentCategoryName(string productName, string? explicitCategoryName, bool isAddon)
        {
            if (!string.IsNullOrWhiteSpace(explicitCategoryName))
                return explicitCategoryName.Trim();

            var n = (productName ?? string.Empty).Trim();
            if (n.Contains("رز") || n.Contains("أرز") || n.Contains("ارز") || n.Contains("بيشاور") || n.Contains("زربيان") || n.Contains("مضغوط") || n.Contains("كبسة") || n.Contains("شعبي"))
                return "قسم الأرز";
            if (n.Contains("دجاج") || n.Contains("شواية") || n.Contains("فحم") || n.Contains("مظبي") || n.Contains("بروست") || n.Contains("مقلقل دجاج"))
                return "قسم الدجاج";
            if (n.Contains("لحم") || n.Contains("تيس") || n.Contains("خروف") || n.Contains("حنيذ") || n.Contains("مقلقل") || n.Contains("كبدة") || n.Contains("راس") || n.Contains("رأس"))
                return "قسم اللحوم";
            if (n.Contains("مشكل") || n.Contains("سحاوق") || n.Contains("سلطة") || n.Contains("زبادي") || n.Contains("فتة") || n.Contains("ملوخية") || n.Contains("بامية") || n.Contains("إيدام") || isAddon)
                return "قسم المشكلات والإضافات";

            return "أصناف عامة";
        }

        public static async Task<List<ProductSalesReportItem>> LoadProductSalesBreakdownForRangeAsync(POSSystem.WPF.Data.POSDbContext db, DateTime startDate, DateTime endOfDay)
        {
            var rawItems = await db.OrderItems
                .AsNoTracking()
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                    .ThenInclude(p => p!.Category)
                .Include(oi => oi.Addon)
                    .ThenInclude(a => a!.Category)
                .Include(oi => oi.addonAndOrderItems)
                    .ThenInclude(aoi => aoi.Addon)
                        .ThenInclude(a => a!.Category)
                .Include(oi => oi.ReturnOrderItems)
                .Where(oi => oi.Order != null &&
                             oi.Order.Status != OrderStatus.Cancelled &&
                             oi.Order.Status != OrderStatus.Returned &&
                             oi.Order.CreatedAt >= startDate && oi.Order.CreatedAt <= endOfDay)
                .ToListAsync();

            var validEntries = new List<(int KeyId, string ProductName, string CategoryName, string SizeName, int NetUnits, decimal PortionQty, decimal Revenue, int OrderId)>();

            foreach (var oi in rawItems)
            {
                int returnedQty = oi.ReturnOrderItems?.Sum(r => r.Quantity) ?? 0;
                int netUnits = Math.Max(0, oi.Quantity - returnedQty);
                if (netUnits <= 0) continue;

                string prodName = oi.Product?.Name ?? oi.Addon?.Name ?? (!string.IsNullOrWhiteSpace(oi.Name) ? oi.Name : "صنف عام");
                int dashIdx = prodName.IndexOf(" - ");
                if (dashIdx > 0 && !string.IsNullOrWhiteSpace(oi.Size))
                {
                    prodName = prodName.Substring(0, dashIdx).Trim();
                }

                string? rawCat = oi.Product?.Category?.Name
                    ?? (!string.IsNullOrWhiteSpace(oi.Product?.CategoryName) ? oi.Product!.CategoryName : null)
                    ?? oi.Addon?.Category?.Name;

                string catName = ResolveDepartmentCategoryName(prodName, rawCat, oi.AddonId.HasValue);

                string sizeDisplay = string.IsNullOrWhiteSpace(oi.Size) ? "عادي / حبة" : POSSystem.WPF.Converters.SizeTranslatorConverter.TranslateSize(oi.Size);
                decimal mult = GetSizePortionMultiplier(sizeDisplay);
                decimal portionQty = netUnits * mult;
                decimal lineRev = netUnits * oi.UnitPrice;
                int keyId = oi.ProductId ?? (oi.AddonId.HasValue ? -oi.AddonId.Value : 0);

                validEntries.Add((keyId, prodName, catName, sizeDisplay, netUnits, portionQty, lineRev, oi.OrderId));

                // تضمين الإضافات والمشكلات المرتبطة بالطلب (مثل المشكلات، السحاوق، السلطات) في الجرد اليومي
                if (oi.addonAndOrderItems != null && oi.addonAndOrderItems.Count > 0)
                {
                    foreach (var linked in oi.addonAndOrderItems)
                    {
                        if (linked.Addon == null) continue;
                        string addonName = linked.Addon.Name?.Trim() ?? "إضافة";
                        string addonCat = ResolveDepartmentCategoryName(addonName, linked.Addon.Category?.Name, true);
                        int addonKeyId = -Math.Abs(linked.Addon.Id);
                        decimal addonRev = netUnits * linked.Addon.Price;
                        validEntries.Add((addonKeyId, addonName, addonCat, "حبة / صحن", netUnits, netUnits, addonRev, oi.OrderId));
                    }
                }
            }

            var grouped = validEntries
                .GroupBy(x => new { x.KeyId, Name = x.ProductName.Trim() })
                .Select(g =>
                {
                    var sizesList = g.GroupBy(x => x.SizeName)
                        .OrderByDescending(sg => sg.Sum(x => x.NetUnits))
                        .Select(sg => $"{sg.Sum(x => x.NetUnits)} ({sg.Key})")
                        .ToList();

                    string unitLabel = InferProductUnitLabel(g.Key.Name, g.Select(x => x.SizeName));

                    return new ProductSalesReportItem
                    {
                        ProductId = g.Key.KeyId,
                        ProductName = g.Key.Name,
                        CategoryName = g.First().CategoryName,
                        TotalSoldQuantity = Math.Round(g.Sum(x => x.PortionQty), 2),
                        UnitLabel = unitLabel,
                        TotalUnitsCount = g.Sum(x => x.NetUnits),
                        SizesBreakdownText = string.Join("  |  ", sizesList),
                        OrdersCount = g.Select(x => x.OrderId).Distinct().Count(),
                        TotalRevenue = g.Sum(x => x.Revenue)
                    };
                })
                .OrderByDescending(x => x.TotalSoldQuantity)
                .ThenByDescending(x => x.TotalRevenue)
                .ToList();

            return grouped;
        }

        private async Task LoadFinancialStatementAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var data = await _financialEngineService.GenerateFinancialStatementAsync(startDate, endDate);

                // 1. المبيعات والإيرادات
                generalReportProberty.TotalSales = data.TotalGrossSales;
                generalReportProberty.TotalOrderReturns = data.TotalReturns;
                generalReportProberty.NetSales = data.NetSales;
                generalReportProberty.TotalRevenue = data.TotalRevenue;
                generalReportProberty.SalesCount = data.SalesCount;
                generalReportProberty.TotalCashSales = data.CashSales;
                generalReportProberty.TotalElectronicSales = data.ElectronicSales;
                generalReportProberty.TotalDebtSales = data.DebtSales;
                generalReportProberty.TopProduct = data.TopSellingProduct;

                // 2. تكلفة البضاعة المباعة والمصروفات والأرباح
                generalReportProberty.TotalCOGS = data.TotalCOGS;
                generalReportProberty.TotalExpenses = data.TotalExpenses;
                generalReportProberty.CashExpensesTotal = data.CashExpensesTotal;
                generalReportProberty.WalletExpensesTotal = data.WalletExpensesTotal;
                generalReportProberty.TotalSalaries = data.TotalSalaries;
                generalReportProberty.TotalStockWasteAndShortage = data.TotalStockWasteAndShortage;
                generalReportProberty.TotalCosts = data.TotalOperatingCosts;
                generalReportProberty.NetProfit = data.NetProfit;
                generalReportProberty.ExpensesByCategory = data.ExpensesDetails
                    .GroupBy(e => e.Category)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
                generalReportProberty.ExpensesDetails.Clear();
                foreach (var expItem in data.ExpensesDetails)
                {
                    generalReportProberty.ExpensesDetails.Add(expItem);
                }

                // 3. المشتريات
                generalReportProberty.TotalPurchases = data.TotalPurchases;
                generalReportProberty.TotalPurchaseReturns = data.TotalPurchaseReturns;

                // 4. ديون العملاء
                generalReportProberty.TotalDebts = data.TotalCustomerDebts;
                generalReportProberty.DebtCount = data.CustomerDebtCount;
                generalReportProberty.RegularCustomers = data.RegularCustomerCount;
                generalReportProberty.TotalPaidDebts = data.TotalPaidDebts;
                generalReportProberty.TotalUnpaidDebts = data.TotalUnpaidDebts;

                generalReportProberty.CustomersDetails.Clear();
                foreach (var cust in data.CustomersDetails)
                {
                    generalReportProberty.CustomersDetails.Add(cust);
                }

                // 5. الموظفين
                generalReportProberty.TotalEmployees = data.TotalEmployees;
                generalReportProberty.TotalAdvances = data.TotalAdvances;

                generalReportProberty.EmployeesDetails.Clear();
                foreach (var emp in data.EmployeesDetails)
                {
                    generalReportProberty.EmployeesDetails.Add(emp);
                }

                // 6. التوزيع اليومي للمبيعات
                generalReportProberty.DailySalesBreakdown.Clear();
                foreach (var item in data.DailySalesBreakdown)
                {
                    generalReportProberty.DailySalesBreakdown.Add(item);
                }

                // 7. تقرير مبيعات المنتجات التفصيلي (كمية الحبات والأنفار المباعة لكل صنف)
                var db = _unitOfWork.GetContext();
                var endOfDay = endDate.Date.AddDays(1).AddTicks(-1);
                var productSales = await LoadProductSalesBreakdownForRangeAsync(db, startDate.Date, endOfDay);
                generalReportProberty.ProductSalesBreakdown.Clear();
                foreach (var ps in productSales)
                {
                    generalReportProberty.ProductSalesBreakdown.Add(ps);
                }
                if (productSales.Any())
                {
                    var top = productSales.First();
                    generalReportProberty.TopProduct = $"{top.ProductName} ({top.TotalSoldQuantityText})";
                }

                // 8. جرد وتفصيل حسابات المصروفات والجهات
                generalReportProberty.ExpenseAccountsBreakdown.Clear();
                var expensesInPeriod = data.ExpensesDetails ?? new List<ExpenseReportItem>();
                decimal totalExpSum = expensesInPeriod.Sum(e => e.Amount);
                var accountGroups = expensesInPeriod
                    .GroupBy(e => string.IsNullOrWhiteSpace(e.Category) ? "حساب مصاريف عامة" : e.Category.Trim())
                    .Select(g =>
                    {
                        decimal cashAmt = g.Where(e => e.PaymentMethodText == null || e.PaymentMethodText.Contains("نقدي")).Sum(e => e.Amount);
                        decimal walletAmt = g.Where(e => e.PaymentMethodText != null && !e.PaymentMethodText.Contains("نقدي")).Sum(e => e.Amount);
                        decimal totAmt = g.Sum(e => e.Amount);
                        return new ExpenseAccountSummaryItem
                        {
                            AccountName = g.Key,
                            TransactionsCount = g.Count(),
                            CashAmount = cashAmt,
                            WalletAmount = walletAmt,
                            TotalAmount = totAmt,
                            Percentage = totalExpSum > 0 ? Math.Round((totAmt / totalExpSum) * 100m, 1) : 0m
                        };
                    })
                    .OrderByDescending(a => a.TotalAmount)
                    .ToList();

                foreach (var acc in accountGroups)
                {
                    generalReportProberty.ExpenseAccountsBreakdown.Add(acc);
                }

                NotifyAllFinancialProperties();
            }
            catch (Exception ex)
            {
                Services.Logging.AppLogger.LogError("Error in LoadFinancialStatementAsync", ex);
            }
        }

        private void NotifyAllFinancialProperties()
        {
            OnPropertyChanged(nameof(generalReportProberty.TotalSales));
            OnPropertyChanged(nameof(generalReportProberty.TotalOrderReturns));
            OnPropertyChanged(nameof(generalReportProberty.NetSales));
            OnPropertyChanged(nameof(generalReportProberty.TotalRevenue));
            OnPropertyChanged(nameof(generalReportProberty.SalesCount));
            OnPropertyChanged(nameof(generalReportProberty.TotalCashSales));
            OnPropertyChanged(nameof(generalReportProberty.TotalElectronicSales));
            OnPropertyChanged(nameof(generalReportProberty.TotalDebtSales));
            OnPropertyChanged(nameof(generalReportProberty.TopProduct));

            OnPropertyChanged(nameof(generalReportProberty.TotalCOGS));
            OnPropertyChanged(nameof(generalReportProberty.TotalExpenses));
            OnPropertyChanged(nameof(generalReportProberty.TotalSalaries));
            OnPropertyChanged(nameof(generalReportProberty.TotalCosts));
            OnPropertyChanged(nameof(generalReportProberty.NetProfit));

            OnPropertyChanged(nameof(generalReportProberty.TotalPurchases));
            OnPropertyChanged(nameof(generalReportProberty.TotalPurchaseReturns));

            OnPropertyChanged(nameof(generalReportProberty.TotalDebts));
            OnPropertyChanged(nameof(generalReportProberty.DebtCount));
            OnPropertyChanged(nameof(generalReportProberty.RegularCustomers));
            OnPropertyChanged(nameof(generalReportProberty.TotalPaidDebts));
            OnPropertyChanged(nameof(generalReportProberty.TotalUnpaidDebts));
            OnPropertyChanged(nameof(generalReportProberty.CustomersDetails));

            OnPropertyChanged(nameof(generalReportProberty.TotalEmployees));
            OnPropertyChanged(nameof(generalReportProberty.TotalAdvances));
            OnPropertyChanged(nameof(generalReportProberty.EmployeesDetails));

            OnPropertyChanged(nameof(generalReportProberty.DailySalesBreakdown));
            OnPropertyChanged(nameof(generalReportProberty.ProductSalesBreakdown));
            OnPropertyChanged(nameof(generalReportProberty.TotalSoldProductsTypesCount));
            OnPropertyChanged(nameof(generalReportProberty.TotalSoldPortionsSum));
            OnPropertyChanged(nameof(generalReportProberty.TotalProductsSalesRevenue));
            OnPropertyChanged(nameof(generalReportProberty.ExpenseAccountsBreakdown));
        }

        private void ExportReport()
        {
            MessageBox.Show("سيتم تصدير التقرير", "معلومة", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
