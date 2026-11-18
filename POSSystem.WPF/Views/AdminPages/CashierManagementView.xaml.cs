using POSSystem.WPF.Models.Helper;
using POSSystem.WPF.ViewModels.Cashier;
using POSSystem.WPF.ViewModels;
using POSSystem.WPF.ViewModels.Reports;
using POSSystem.WPF.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Windows.Controls;
using System.Windows;

namespace POSSystem.WPF.Views.AdminPages
{
    public partial class CashierManagementView : UserControl
    {
        private CashierManagementViewModel _viewModel;

        public CashierManagementView()
        {
            InitializeComponent();
            _viewModel = new CashierManagementViewModel();
            DataContext = _viewModel;
            Loaded += async (s, e) => {
                if (_viewModel != null) {
                    await _viewModel.LoadCashiersListAsync();
                    await _viewModel.LoadCashierSalesReportAsync();
                }
            };
        }

        private void PeriodType_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.RadioButton radioButton && _viewModel != null)
            {
                var tag = radioButton.Tag?.ToString();
                if (tag == "Daily")
                    _viewModel.PeriodType = ReportPeriodType.Daily;
                else if (tag == "Monthly")
                    _viewModel.PeriodType = ReportPeriodType.Monthly;
                else if (tag == "Quarterly")
                    _viewModel.PeriodType = ReportPeriodType.Quarterly;
                else if (tag == "Yearly")
                    _viewModel.PeriodType = ReportPeriodType.Yearly;
            }
        }

        private void PrintSalesReport_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (_viewModel == null) return;
                
                var reports = _viewModel.CashierSalesReports?.ToList() ?? new List<CashierSalesReport>();
                var columnHeaders = new[] { "اسم الكاشير", "عدد المبيعات", "إجمالي الإيرادات", "مبيعات نقدية", "مبيعات دين", "متوسط المبيعات" };
                
                Services.PrintService.PrintReport(
                    "تقرير المبيعات",
                    "Cashier Sales Report",
                    reports,
                    columnHeaders,
                     (object item) => 
                     {
                        if (item == null) return new string[columnHeaders.Length];
                        var report = item as CashierSalesReport;
                        if (report == null) return new string[columnHeaders.Length];
                        return new[]
                        {
                            report.CashierName ?? "",
                            report.InvoiceCount.ToString(),
                            report.TotalRevenue.ToString("#,##0") + " ريال",
                            report.CashSales.ToString("#,##0") + " ريال",
                            report.DebtSales.ToString("#,##0") + " ريال",
                            report.AverageSaleAmount.ToString("#,##0") + " ريال"
                        };
                     },
                    $"إجمالي الكاشيرات: {reports.Count}\nإجمالي المبيعات: {reports.Sum(r => r.InvoiceCount)}\nإجمالي الإيرادات: {reports.Sum(r => r.TotalRevenue):#,##0} ريال",
                    preferPdf: true
                );
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ في حفظ التقرير: {ex.Message}", "خطأ", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void PrintDeliveryReport_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (_viewModel == null) return;
                
                var reports = _viewModel.DeliveryReports?.ToList() ?? new List<DeliveryReport>();
                var columnHeaders = new[] { "اسم صاحب التوصيل", "عدد الطلبات", "إجمالي قيمة الطلبات", "طلبات مكتملة", "طلبات قيد الانتظار", "متوسط قيمة الطلب" };
                
                Services.PrintService.PrintReport(
                    "تقرير التوصيل",
                    "Delivery Report",
                    reports,
                    columnHeaders,
                     (object item) => 
                     {
                        if (item == null) return new string[columnHeaders.Length];
                        var report = item as DeliveryReport;
                        if (report == null) return new string[columnHeaders.Length];
                        return new[]
                        {
                            report.DeliveryPersonName ?? "",
                            report.TotalOrders.ToString(),
                            report.TotalOrderValue.ToString("#,##0") + " ريال",
                            report.CompletedOrders.ToString(),
                            report.PendingOrders.ToString(),
                            report.AverageOrderValue.ToString("#,##0") + " ريال"
                        };
                     },
                    $"إجمالي عمال التوصيل: {reports.Count}\nإجمالي الطلبات: {reports.Sum(r => r.TotalOrders)}\nإجمالي قيمة الطلبات: {reports.Sum(r => r.TotalOrderValue):#,##0} ريال",
                    preferPdf: true
                );
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ في حفظ التقرير: {ex.Message}", "خطأ", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void PrintShiftReport_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (_viewModel == null) return;
                
                var reports = _viewModel.ShiftReports?.ToList() ?? new List<ShiftReport>();
                var columnHeaders = new[] { "رقم المناوبة", "الكاشير", "البداية", "النهاية", "المبيعات", "النقد", "الديون", "الفواتير", "المرتجع", "مبلغ المرتجع" };
                
                Services.PrintService.PrintReport(
                    "تقرير مناوبات الكاشير التفصيلي",
                    "Detailed Cashier Shifts Report",
                    reports,
                    columnHeaders,
                     (object item) => 
                     {
                        if (item == null) return new string[columnHeaders.Length];
                        var report = item as ShiftReport;
                        if (report == null) return new string[columnHeaders.Length];
                        return new[]
                        {
                            report.ShiftNumber?.Replace("SHIFT-", "") ?? "-",
                            report.EmployeeName ?? "",
                            report.StartTime.ToString("MM/dd HH:mm"),
                            report.EndTime.ToString("MM/dd HH:mm"),
                            report.TotalSales.ToString("#,##0"),
                            report.CashReceived.ToString("#,##0"),
                            report.TotalDebtsCreated.ToString("#,##0"),
                            report.InvoiceCount.ToString(),
                            report.ReturnedOrdersCount.ToString(),
                            report.ReturnedOrdersAmount.ToString("#,##0")
                        };
                     },
                    $"إجمالي المبيعات: {_viewModel.TotalShiftSalesSum:#,##0} ريال | " +
                    $"إجمالي النقد: {_viewModel.TotalCashReceivedSum:#,##0} ريال | " +
                    $"إجمالي الديون: {_viewModel.TotalDebtsSum:#,##0} ريال\n" +
                    $"إجمالي الفواتير: {_viewModel.TotalInvoiceCountSum} | " +
                    $"إجمالي المرتجعات: {_viewModel.TotalReturnedOrdersCountSum} ({_viewModel.TotalReturnedOrdersAmountSum:#,##0} ريال)",
                    preferPdf: true
                );
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ في حفظ التقرير: {ex.Message}", "خطأ", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void PrintCashBoxReport_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (_viewModel == null) return;
                
                var reports = _viewModel.CashBoxReports?.ToList() ?? new List<CashBoxReport>();
                var columnHeaders = new[] { "اسم الكاشير", "رقم المناوبة", "نوع المعاملة", "المبلغ", "الوصف", "التاريخ", "الوقت", "نوع المصروف", "ملاحظات" };
                
                Services.PrintService.PrintReport(
                    "تقرير الصندوق",
                    "Cash Box Report",
                    reports,
                    columnHeaders,
                     (object item) => 
                     {
                        if (item == null) return new string[columnHeaders.Length];
                        var report = item as CashBoxReport;
                        if (report == null) return new string[columnHeaders.Length];
                        return new[]
                        {
                            report.CashierName ?? "",
                            report.ShiftNumber ?? "",
                            report.TransactionType ?? "",
                            report.Amount.ToString("#,##0") + " ريال",
                            report.Description ?? "",
                            report.TransactionDate.ToString("yyyy/MM/dd"),
                            report.TransactionTime ?? "",
                            report.ExpenseTypeText ?? "",
                            report.Notes ?? ""
                        };
                     },
                    $"إجمالي المعاملات: {reports.Count}\nإجمالي المبلغ: {reports.Sum(r => r.Amount):#,##0} ريال",
                    preferPdf: true
                );
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ في حفظ التقرير: {ex.Message}", "خطأ", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void PrintDailySalesReport_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (_viewModel == null) return;
                
                var reports = _viewModel.DailySalesReports?.ToList() ?? new List<DailySalesReport>();
                var columnHeaders = new[] { "التاريخ", "عدد المبيعات", "إجمالي الإيرادات", "مبيعات نقدية", "مبيعات دين", "متوسط المبيعات" };
                
                Services.PrintService.PrintReport(
                    "تقرير المبيعات اليومي",
                    "Daily Sales Report",
                    reports,
                    columnHeaders,
                     (object item) => 
                     {
                        if (item == null) return new string[columnHeaders.Length];
                        var report = item as DailySalesReport;
                        if (report == null) return new string[columnHeaders.Length];
                        return new[]
                        {
                            report.Date.ToString("yyyy/MM/dd"),
                            report.InvoiceCount.ToString(),
                            report.TotalRevenue.ToString("#,##0") + " ريال",
                            report.CashSales.ToString("#,##0") + " ريال",
                            report.DebtSales.ToString("#,##0") + " ريال",
                            report.AverageSaleAmount.ToString("#,##0") + " ريال"
                        };
                     },
                    $"إجمالي الأيام: {reports.Count}\nإجمالي المبيعات: {reports.Sum(r => r.InvoiceCount)}\nإجمالي الإيرادات: {reports.Sum(r => r.TotalRevenue):#,##0} ريال",
                    preferPdf: true
                );
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ في حفظ التقرير: {ex.Message}", "خطأ", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PrintAuditReport_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (_viewModel == null || _viewModel.AuditSummary == null) return;

                var combinedList = new List<object>();
                foreach (var c in _viewModel.AuditCancellations)
                {
                    combinedList.Add(new
                    {
                        Type = "🚨 طلب ملغي",
                        Time = c.TimeString,
                        Number = c.OrderNumber,
                        Cashier = c.CashierName,
                        Amount = c.TotalAmount,
                        Details = c.ItemsSummary,
                        Notes = $"السبب: {c.Reason} ({c.DurationText})"
                    });
                }

                foreach (var d in _viewModel.AuditDiscounts)
                {
                    combinedList.Add(new
                    {
                        Type = "💰 خصم ممنوح",
                        Time = d.TimeString,
                        Number = d.OrderNumber,
                        Cashier = d.CashierName,
                        Amount = d.DiscountAmount,
                        Details = $"قبل الخصم: {d.SubTotal:N0} | الصافي: {d.Total:N0} ({d.DiscountPercentText})",
                        Notes = string.IsNullOrWhiteSpace(d.Notes) ? d.PaymentMethodText : $"{d.PaymentMethodText} - {d.Notes}"
                    });
                }

                var columnHeaders = new[] { "النوع", "الوقت", "رقم الفاتورة", "الكاشير", "المبلغ", "التفاصيل والأصناف", "السبب / الملاحظات" };

                string summary = $"حالة الرقابة: {_viewModel.AuditSummary.SecurityStatus}\n" +
                                 $"إجمالي مبالغ الخصومات: {_viewModel.AuditSummary.TotalDiscountAmount:N0} ريال ({_viewModel.AuditSummary.DiscountInvoicesCount} فواتير)\n" +
                                 $"إجمالي الطلبات الملغاة: {_viewModel.AuditSummary.TotalCancelledAmount:N0} ريال ({_viewModel.AuditSummary.CancelledOrdersCount} طلبات)\n" +
                                 $"إجمالي العمليات الحساسة: {_viewModel.AuditSummary.TotalSensitiveOperationsCount}";

                Services.PrintService.PrintReport(
                    "تقرير رقابة وتدقيق الكاشير والعمليات الحساسة",
                    "Cashier Sensitive Operations & Audit Log",
                    combinedList,
                    columnHeaders,
                    (object item) =>
                    {
                        dynamic obj = item;
                        return new[]
                        {
                            (string)obj.Type,
                            (string)obj.Time,
                            (string)obj.Number,
                            (string)obj.Cashier,
                            ((decimal)obj.Amount).ToString("#,##0") + " ريال",
                            (string)obj.Details,
                            (string)obj.Notes
                        };
                    },
                    summary,
                    preferPdf: true
                );
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ في حفظ التقرير: {ex.Message}", "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}

