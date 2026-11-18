using System.Windows.Controls;
using POSSystem.WPF.ViewModels.Reports;
using POSSystem.WPF.ViewModels;
using System;
using System.Collections.Generic;

namespace POSSystem.WPF.Views.AdminPages
{
    public partial class MonthlyReportsView : UserControl
    {
        private GeneralReportsViewModel _viewModel;

        public MonthlyReportsView()
        {
            InitializeComponent();
            _viewModel = new GeneralReportsViewModel();
            DataContext = _viewModel;
            Loaded += (s, e) => { _viewModel?.GenerateReportCommand?.Execute(null); };
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
                else if (tag == "Yearly")
                    _viewModel.PeriodType = ReportPeriodType.Yearly;
            }
        }

        private void PrintReport_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (_viewModel == null) 
                {
                    System.Windows.MessageBox.Show("لا يمكن الوصول إلى البيانات", "خطأ", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                var sections = new List<Services.ReportSection>();

                // 1. Financial Summary Section
                sections.Add(new Services.ReportSection
                {
                    Title = "الخلاصة المالية والإحصائيات العامة",
                    SummaryItems = new List<string>
                    {
                        $"صافي الإيرادات: {_viewModel.generalReportProberty.TotalRevenue:#,##0} ريال",
                        $"إجمالي التكاليف التشغيلية (مصاريف تشغيل + رواتب مستحقة + تكلفة بضاعة مباعة COGS): {_viewModel.generalReportProberty.TotalCosts:#,##0} ريال",
                        $"صافي الربح الحقيقي: {_viewModel.generalReportProberty.NetProfit:#,##0} ريال",
                        $"إجمالي المشتريات الرأسمالية للمخزون: {_viewModel.generalReportProberty.TotalPurchases:#,##0} ريال (أصول متداولة لا تخصم مباشرة من أرباح التشغيل)",
                        $"---",
                        $"إجمالي المبيعات: {_viewModel.generalReportProberty.TotalSales:#,##0} ريال (عدد المعاملات: {_viewModel.generalReportProberty.SalesCount})",
                        $"صافي المبيعات: {_viewModel.generalReportProberty.NetSales:#,##0} ريال",
                        $"مرتجعات المبيعات: {_viewModel.generalReportProberty.TotalOrderReturns:#,##0} ريال",
                        $"تكلفة البضاعة المباعة (COGS من وصفات المطبخ والمستودع): {_viewModel.generalReportProberty.TotalCOGS:#,##0} ريال",
                        $"إجمالي المصروفات التشغيلية: {_viewModel.generalReportProberty.TotalExpenses:#,##0} ريال",
                        $"إجمالي الديون القائمة: {_viewModel.generalReportProberty.TotalDebts:#,##0} ريال (لعدد {_viewModel.generalReportProberty.RegularCustomers} عملاء)",
                        $"إجمالي الرواتب المستحقة للفترة: {_viewModel.generalReportProberty.TotalSalaries:#,##0} ريال (لعدد {_viewModel.generalReportProberty.TotalEmployees} موظفين)",
                        $"إجمالي السلفيات القائمة: {_viewModel.generalReportProberty.TotalAdvances:#,##0} ريال",
                        $"أفضل المنتجات مبيعاً: {_viewModel.generalReportProberty.TopProduct}"
                    }
                });

                // 2. Shifts Section
                var shiftRows = new List<string[]>();
                foreach (var shift in _viewModel.generalReportProberty.ShiftReports)
                {
                    shiftRows.Add(new[]
                    {
                        shift.ShiftNumber ?? "-",
                        shift.EmployeeName ?? "-",
                        shift.StartTime.ToString("yyyy/MM/dd HH:mm"),
                        shift.EndTime.ToString("yyyy/MM/dd HH:mm"),
                        shift.TotalSales.ToString("#,##0") + " ريال",
                        shift.CashReceived.ToString("#,##0") + " ريال",
                        shift.TotalDebtsCreated.ToString("#,##0") + " ريال",
                        shift.InvoiceCount.ToString()
                    });
                }

                sections.Add(new Services.ReportSection
                {
                    Title = "تقرير المناويات التفصيلي",
                    ColumnHeaders = new[] { "رقم المناوبة", "الموظف", "وقت البدء", "وقت الانتهاء", "المبيعات", "نقد مستلم", "ديون", "الفواتير" },
                    Rows = shiftRows
                });

                // 3. Cash Box Section
                var cashBoxRows = new List<string[]>();
                foreach (var cb in _viewModel.generalReportProberty.CashBoxReports)
                {
                    cashBoxRows.Add(new[]
                    {
                        cb.EmployeeName ?? "-",
                        cb.TransactionDate.ToString("yyyy/MM/dd"),
                        cb.FirstTransactionTime.ToString("HH:mm"),
                        cb.LastTransactionTime.ToString("HH:mm"),
                        cb.TotalAmount.ToString("#,##0") + " ريال",
                        cb.TransactionCount.ToString(),
                        cb.Description ?? "-"
                    });
                }

                sections.Add(new Services.ReportSection
                {
                    Title = "تقرير سحب الصندوق (النثريات)",
                    ColumnHeaders = new[] { "الموظف", "التاريخ", "من ساعة", "إلى ساعة", "الإجمالي", "العدد", "الوصف" },
                    Rows = cashBoxRows
                });

                // 4. Sales Section
                var salesRows = new List<string[]>();
                if (_viewModel.generalReportProberty.DailySalesBreakdown != null)
                {
                    foreach (var s in _viewModel.generalReportProberty.DailySalesBreakdown)
                    {
                        salesRows.Add(new[]
                        {
                            s.Date.ToString("yyyy/MM/dd"),
                            s.TotalAmount.ToString("#,##0") + " ريال",
                            s.CashSales.ToString("#,##0") + " ريال",
                            s.ElectronicSales.ToString("#,##0") + " ريال",
                            s.DebtSales.ToString("#,##0") + " ريال",
                            s.PaidDebtSales.ToString("#,##0") + " ريال",
                            s.UnpaidDebtSales.ToString("#,##0") + " ريال"
                        });
                    }
                }
                
                if (salesRows.Any())
                {
                    sections.Add(new Services.ReportSection
                    {
                        Title = "تفاصيل المبيعات اليومية",
                        ColumnHeaders = new[] { "التاريخ", "إجمالي المبيعات", "مبيعات نقدية", "إلكترونية", "إجمالي الدين", "دين مسدد", "دين غير مسدد" },
                        Rows = salesRows
                    });
                }

                // 5. Employees Section
                var employeeRows = new List<string[]>();
                if (_viewModel.generalReportProberty.EmployeesDetails != null)
                {
                    foreach (var emp in _viewModel.generalReportProberty.EmployeesDetails)
                    {
                        employeeRows.Add(new[]
                        {
                            emp.EmployeeName,
                            emp.Role,
                            emp.BaseSalary.ToString("#,##0") + " ريال",
                            emp.TotalAdvances.ToString("#,##0") + " ريال"
                        });
                    }
                }

                if (employeeRows.Any())
                {
                    sections.Add(new Services.ReportSection
                    {
                        Title = "تقرير الموظفين التفصيلي",
                        ColumnHeaders = new[] { "اسم الموظف", "المنصب/الصلاحية", "الراتب الأساسي", "السلف المتبقية" },
                        Rows = employeeRows
                    });
                }

                // 6. Customers Section
                var customerRows = new List<string[]>();
                if (_viewModel.generalReportProberty.CustomersDetails != null)
                {
                    foreach (var cust in _viewModel.generalReportProberty.CustomersDetails)
                    {
                        customerRows.Add(new[]
                        {
                            cust.CustomerName,
                            cust.TotalDebt.ToString("#,##0") + " ريال",
                            cust.PaidAmount.ToString("#,##0") + " ريال",
                            cust.RemainingAmount.ToString("#,##0") + " ريال"
                        });
                    }
                }

                if (customerRows.Any())
                {
                    sections.Add(new Services.ReportSection
                    {
                        Title = "تقرير العملاء والديون",
                        ColumnHeaders = new[] { "اسم العميل", "إجمالي الدين", "المبلغ المسدد", "المبلغ المتبقي" },
                        Rows = customerRows
                    });
                }

                // 7. Expenses Section
                var expenseRows = new List<string[]>();
                if (_viewModel.generalReportProberty.ExpensesDetails != null)
                {
                    foreach (var exp in _viewModel.generalReportProberty.ExpensesDetails)
                    {
                        expenseRows.Add(new[]
                        {
                            exp.Name ?? "",
                            exp.Category ?? "",
                            exp.PaymentMethodText ?? "",
                            exp.Amount.ToString("#,##0") + " ريال",
                            exp.ExpenseDate.ToString("yyyy/MM/dd")
                        });
                    }
                }

                if (expenseRows.Any())
                {
                    sections.Add(new Services.ReportSection
                    {
                        Title = "تقرير المصروفات التفصيلي",
                        ColumnHeaders = new[] { "اسم المصروف", "الفئة", "طريقة الدفع / المحفظة", "المبلغ", "التاريخ" },
                        Rows = expenseRows
                    });
                }

                var periodText = _viewModel.PeriodType switch
                {
                    ReportPeriodType.Daily => _viewModel.ReportDate.ToString("yyyy-MM-dd"),
                    ReportPeriodType.Monthly => _viewModel.ReportDate.ToString("yyyy/MM"),
                    ReportPeriodType.Yearly => _viewModel.ReportDate.Year.ToString(),
                    _ => _viewModel.ReportDate.ToString("yyyy/MM")
                };

                Services.PrintService.PrintMultiSectionReport(
                    "التقرير العام الشامل",
                    $"الفترة المحددة: {periodText}",
                    sections,
                    $"تم استخراج هذا التقرير بتاريخ {DateTime.Now:yyyy/MM/dd HH:mm} ويشمل كافة البيانات المالية والعمليات التشغيلية للفترة المذكورة.",
                    preferPdf: true
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Print general report error: {ex.Message}\n{ex.StackTrace}");
                System.Windows.MessageBox.Show($"حدث خطأ أثناء محاولة حفظ التقرير: {ex.Message}", "خطأ", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        private void DataGrid_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new System.Windows.Input.MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((System.Windows.Controls.Control)sender).Parent as System.Windows.UIElement;
                parent?.RaiseEvent(eventArg);
            }
        }
    }
}

