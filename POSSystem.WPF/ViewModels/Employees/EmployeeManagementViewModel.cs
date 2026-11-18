using POSSystem.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using POSSystem.WPF.Models;
using POSSystem.WPF.Services;
using POSSystem.WPF.Shered.Commands;
using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.StaticFunction;
using POSSystem.WPF.Views.Popups;

namespace POSSystem.WPF.ViewModels.Employees
{
    public partial class EmployeeManagementViewModel : BaseViewModel, POSSystem.WPF.Services.ISearchable
    {
        public void ApplySearch(string query)
        {
            employeeManagementProberty.SearchText = query;
        }
        private readonly Debouncer _searchDebouncer = new Debouncer(500);

        public EmployeeManagementViewModel()
        {
            var sp = POSSystem.WPF.App.ServiceProvider;
            _employeeService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IEmployeeService>(sp);
            employeeManagementProberty = new EmployeeManagementProberty(_employeeService);
            Commands = new EmployeeManagementCommands(this);
            employeeManagementProberty.PropertyChanged += (s, e) => { 
                if (e.PropertyName == nameof(employeeManagementProberty.SearchText)) 
                    _searchDebouncer.RunAsync(() => SearchEmployeesAsync()).SafeFireAndForget(); 
            };
            InitializeAsync().SafeFireAndForget();
        }

        public async Task InitializeAsync() { await LoadEmployeesAsync(); await LoadUnpaidAdvancesAsync(); await LoadUnpaidSalariesAsync(); }

        public async Task LoadEmployeesAsync()
        {
            try
            {
                var employees = await _employeeService.GetAllEmployeesAsync();
                _allEmployees = employees.ToList();
                employeeManagementProberty.Employees = new ObservableCollection<User>(_allEmployees);
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في تحميل الموظفين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public async Task SearchEmployeesAsync()
        {
            try
            {
                var searchText = employeeManagementProberty.SearchText?.ToLower();
                var filtered = _allEmployees.Where(e => string.IsNullOrWhiteSpace(searchText) || (e.Name?.ToLower().Contains(searchText) ?? false) || (e.Phone?.Contains(searchText) ?? false) || (e.JobTitle?.ToLower().Contains(searchText) ?? false) || (e.Email?.ToLower().Contains(searchText) ?? false)).ToList();
                employeeManagementProberty.Employees = new ObservableCollection<User>(filtered);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error during search: {ex.Message}"); }
        }

        public async Task LoadUnpaidAdvancesAsync()
        {
            try
            {
                var advances = await _employeeService.GetAllAdvancesAsync();
                employeeManagementProberty.Advances = new ObservableCollection<EmployeeAdvance>(advances);
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في تحميل السلفيات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public async Task LoadUnpaidSalariesAsync()
        {
            try
            {
                var salaries = await _employeeService.GetAllUnpaidSalariesAsync();
                var activeSalaries = salaries.Where(s => s.Employee != null && s.Employee.IsActive).ToList();
                employeeManagementProberty.Salaries = new ObservableCollection<EmployeeSalary>(activeSalaries);
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في تحميل المرتبات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public void AddEmployee()
        {
            try { var popup = new AddEmployeePopup(); var owner = Application.Current.MainWindow; if (owner != null && owner != popup && owner.IsLoaded) popup.Owner = owner; else popup.WindowStartupLocation = WindowStartupLocation.CenterScreen; if (popup.ShowDialog() == true) LoadEmployeesAsync().SafeFireAndForget(); }
            catch (Exception ex) { MessageBox.Show($"خطأ في فتح نافذة إضافة موظف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public void EditEmployee(User? employee)
        {
            if (employee == null) return;
            try { var popup = new EditEmployeePopup(employee); var owner = Application.Current.MainWindow; if (owner != null && owner != popup && owner.IsLoaded) popup.Owner = owner; else popup.WindowStartupLocation = WindowStartupLocation.CenterScreen; if (popup.ShowDialog() == true) LoadEmployeesAsync().SafeFireAndForget(); }
            catch (Exception ex) { MessageBox.Show($"خطأ في فتح نافذة تعديل موظف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public void ShowEmployeeStatement(User? employee)
        {
            employee ??= employeeManagementProberty.SelectedEmployee;
            if (employee == null)
            {
                MessageBox.Show("يرجى تحديد موظف من القائمة أولاً لعرض كشف حسابه.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try
            {
                var popup = new Views.Popups.EmployeeStatementPopup(employee);
                var owner = Application.Current.MainWindow;
                if (owner != null && owner != popup && owner.IsLoaded)
                    popup.Owner = owner;
                else
                    popup.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                popup.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح كشف حساب الموظف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void DeleteEmployee(User? employee)
        {
            if (employee == null) return;
            if (employee.Role == UserRole.Admin || employee.Id == 1)
            {
                MessageBox.Show("لا يمكن حذف حساب المدير الرئيسي للنظام.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show($"هل أنت متأكد من حذف الموظف {employee.Name} نهائياً؟ سيتم حذف جميع سجلاته المرتبطة (المرتبات، السلفيات، والتقارير) ولا يمكن التراجع عن هذه العملية.", "تأكيد الحذف النهائي", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try 
                { 
                    await _employeeService.DeleteEmployeeAsync(employee.Id); 
                    employeeManagementProberty._employees.Remove(employee); 
                    _allEmployees.Remove(employee); 
                    
                    // Refresh other lists just in case
                    POSSystem.WPF.Helpers.TaskExtensions.RunSafe(async () => { await LoadUnpaidSalariesAsync(); await LoadUnpaidAdvancesAsync(); });
                    
                    MessageBox.Show("تم حذف الموظف وجميع سجلاته بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information); 
                }
                catch (Exception ex) { MessageBox.Show($"خطأ في حذف الموظف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        public void AddAdvance()
        {
            try { var popup = new AddEmployeeAdvancePopup(employeeManagementProberty.SelectedEmployee); var owner = Application.Current.MainWindow; if (owner != null && owner != popup && owner.IsLoaded) popup.Owner = owner; else popup.WindowStartupLocation = WindowStartupLocation.CenterScreen; if (popup.ShowDialog() == true) { POSSystem.WPF.Helpers.TaskExtensions.RunSafe(async () => { await LoadUnpaidAdvancesAsync(); await LoadUnpaidSalariesAsync(); if (employeeManagementProberty.SelectedEmployee != null) await EmployeeStatic.LoadEmployeeAdvancesAsync(employeeManagementProberty.SelectedEmployee.Id, _employeeService); }); } }
            catch (Exception ex) { MessageBox.Show($"خطأ في فتح نافذة إضافة سلفية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public void EditAdvance(EmployeeAdvance? advance)
        {
            if (advance == null) return;
            MessageBox.Show("ميزة تعديل السلفية قيد التطوير", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public async void DeleteAdvance(EmployeeAdvance? advance)
        {
            if (advance == null) return;
            var empName = advance.Employee?.Name ?? "الموظف";
            if (MessageBox.Show(
                $"هل أنت متأكد من حذف السلفة بمبلغ ({advance.Amount:N0} ريال) للموظف '{empName}'؟\n\n" +
                "سيؤدي ذلك إلى:\n" +
                "1. عكس وإلغاء حركة الصرف من صندوق الكاشير وإعادة الرصيد للدرج فوراً.\n" +
                "2. حذف سند الصرف المرتبط بالسلفة.\n" +
                "3. إلغاء استقطاع السلفة من مرتبات الموظف القادمة.",
                "تأكيد حذف السلفة",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    await _employeeService.DeleteAdvanceAsync(advance.Id);
                    employeeManagementProberty._advances.Remove(advance);
                    await LoadUnpaidAdvancesAsync();
                    await LoadUnpaidSalariesAsync();
                    if (employeeManagementProberty.SelectedEmployee != null)
                        await EmployeeStatic.LoadEmployeeAdvancesAsync(employeeManagementProberty.SelectedEmployee.Id, _employeeService);

                    MessageBox.Show("تم حذف السلفة وعكس حركة الصندوق بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في حذف السلفة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void RepayAdvance(EmployeeAdvance? advance)
        {
            if (advance == null) return;
            try { var popup = new RepayAdvancePopup(advance); var owner = Application.Current.MainWindow; if (owner != null && owner != popup && owner.IsLoaded) popup.Owner = owner; else popup.WindowStartupLocation = WindowStartupLocation.CenterScreen; if (popup.ShowDialog() == true) { POSSystem.WPF.Helpers.TaskExtensions.RunSafe(async () => { await LoadUnpaidAdvancesAsync(); if (employeeManagementProberty.SelectedEmployee != null) await EmployeeStatic.LoadEmployeeAdvancesAsync(employeeManagementProberty.SelectedEmployee.Id, _employeeService); }); } }
            catch (Exception ex) { MessageBox.Show($"خطأ في فتح نافذة التسديد: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public void RepayInstallment(EmployeeAdvance? advance)
        {
            if (advance == null) return;
            // For now, we use the same repayment popup as it allows partial payments (installments)
            // But we could customize it further if needed.
            RepayAdvance(advance);
        }

        public async void MarkAdvanceAsRepaid(EmployeeAdvance? advance)
        {
            if (advance == null) return;
            if (MessageBox.Show($"هل أنت متأكد من تسجيل السلفية كمسددة؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try { await _employeeService.MarkAdvanceAsRepaidAsync(advance.Id); advance.IsRepaid = true; employeeManagementProberty._advances.Remove(advance); LoadUnpaidSalariesAsync().SafeFireAndForget(); MessageBox.Show("تم تسجيل السلفية كمسددة", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information); }
                catch (Exception ex) { MessageBox.Show($"خطأ في تحديث السلفية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        public async Task ProcessSalaries()
        {
            if (MessageBox.Show("هل تريد إنشاء سجلات المرتبات المستحقة لجميع الموظفين بناءً على تواريخ تعيينهم؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try { await _employeeService.ProcessMonthlySalariesAsync(DateTime.Now); await LoadUnpaidSalariesAsync(); MessageBox.Show("تم إنشاء سجلات المرتبات بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information); }
                catch (Exception ex) { MessageBox.Show($"خطأ في معالجة المرتبات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        public async void PaySalary(EmployeeSalary? salary)
        {
            if (salary == null) return;
            try
            {
                var popup = new PaySalaryPopup(salary);
                var owner = Application.Current.MainWindow;
                if (owner != null && owner != popup && owner.IsLoaded)
                    popup.Owner = owner;
                else
                    popup.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                if (popup.ShowDialog() == true)
                {
                    employeeManagementProberty._salaries.Remove(salary);
                    await LoadUnpaidSalariesAsync();
                    await LoadUnpaidAdvancesAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في صرف الراتب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task PayAllSalaries()
        {
            try
            {
                var unpaidSalaries = await _employeeService.GetAllUnpaidSalariesAsync();
                var activeSalaries = unpaidSalaries.Where(s => s.Employee != null && s.Employee.IsActive && !s.IsPaid).ToList();
                if (activeSalaries.Count == 0)
                {
                    MessageBox.Show("لا توجد مرتبات غير مدفوعة حالياً.", "معلومة", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var popup = new PaySalaryPopup(activeSalaries);
                var owner = Application.Current.MainWindow;
                if (owner != null && owner != popup && owner.IsLoaded)
                    popup.Owner = owner;
                else
                    popup.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                if (popup.ShowDialog() == true)
                {
                    await LoadUnpaidSalariesAsync();
                    await LoadUnpaidAdvancesAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في صرف المرتبات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task LoadAllAdvancesForReportAsync()
        {
            try 
            { 
                var advances = await _employeeService.GetAllAdvancesAsync(); 
                decimal totalAmount = 0m;
                decimal totalPaid = 0m;
                decimal totalUnpaid = 0m;

                foreach (var advance in advances) 
                {
                    totalAmount += advance.Amount;
                    totalPaid += advance.PaidAmount;
                    totalUnpaid += advance.RemainingAmount;
                }
                
                employeeManagementProberty.AdvancesReportPagination.SetItems(advances);
                
                employeeManagementProberty.TotalAdvancesAmount = totalAmount;
                employeeManagementProberty.TotalAdvancesPaid = totalPaid;
                employeeManagementProberty.TotalAdvancesUnpaid = totalUnpaid;

                OnPropertyChanged(nameof(employeeManagementProberty.Advances)); 
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في تحميل تقرير السلفيات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public async Task LoadAllSalariesForReportAsync()
        {
            try 
            { 
                var startDate = DateTime.Now.AddMonths(-12); 
                var endDate = DateTime.Now; 
                var allSalaries = await _employeeService.GetSalariesByDateRangeAsync(startDate, endDate); 
                decimal totalBase = 0m;
                decimal totalDeduction = 0m;
                decimal totalNet = 0m;

                foreach (var salary in allSalaries) 
                {
                    totalBase += salary.BaseSalary;
                    totalDeduction += salary.AdvanceDeduction;
                    totalNet += salary.NetSalary;
                }
                
                employeeManagementProberty.SalariesReportPagination.SetItems(allSalaries);
                
                employeeManagementProberty.TotalSalariesBase = totalBase;
                employeeManagementProberty.TotalSalariesDeduction = totalDeduction;
                employeeManagementProberty.TotalSalariesNet = totalNet;

                OnPropertyChanged(nameof(employeeManagementProberty.Salaries)); 
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في تحميل تقرير المرتبات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public void PrintEmployeesReport()
        {
            try
            {
                var employees = employeeManagementProberty._employees.ToList();
                var columnHeaders = new[] { "الاسم", "البريد الإلكتروني", "الهاتف", "المسمى الوظيفي", "الراتب الأساسي", "تاريخ التعيين" };
                
                Services.PrintService.PrintReport(
                    "تقرير الموظفين",
                    "Employees Report",
                    employees,
                    columnHeaders,
                    (object item) => 
                    {
                        if (item == null) return new string[columnHeaders.Length];
                        var employee = item as User;
                        if (employee == null) return new string[columnHeaders.Length];
                        return new[]
                        {
                            employee.Name ?? "",
                            employee.Email ?? "",
                            employee.Phone ?? "",
                            employee.JobTitle ?? "",
                            employee.BaseSalary?.ToString("#,##0") + " ريال" ?? "",
                            employee.HireDate?.ToString("yyyy/MM/dd") ?? ""
                        };
                    },
                    $"إجمالي الموظفين: {employees.Count}",
                    preferPdf: true
                );
            }
            catch (Exception ex) 
            { 
                MessageBox.Show($"خطأ في حفظ التقرير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); 
            }
        }

        public void PrintAdvancesReport()
        {
            try
            {
                var advances = employeeManagementProberty._advances.ToList();
                var columnHeaders = new[] { "الموظف", "المبلغ", "المبلغ المتبقي", "تاريخ السلفية", "تاريخ السداد", "طريقة السداد", "الحالة" };
                
                Services.PrintService.PrintReport(
                    "تقرير السلفيات",
                    "Advances Report",
                    advances,
                    columnHeaders,
                    (object item) => 
                    {
                        if (item == null) return new string[columnHeaders.Length];
                        var advance = item as EmployeeAdvance;
                        if (advance == null) return new string[columnHeaders.Length];
                        return new[]
                        {
                            advance.Employee?.Name ?? "",
                            advance.Amount.ToString("#,##0") + " ريال",
                            advance.RemainingAmount.ToString("#,##0") + " ريال",
                            advance.AdvanceDate.ToString("yyyy/MM/dd"),
                            advance.RepaymentDate?.ToString("yyyy/MM/dd") ?? "",
                            advance.RepaymentMethodText ?? "",
                            advance.IsRepaid ? "مسددة" : "غير مسددة"
                        };
                    },
                    $"إجمالي السلفيات: {advances.Count}\nإجمالي المبلغ: {advances.Sum(a => a.Amount):#,##0} ريال",
                    preferPdf: true
                );
            }
            catch (Exception ex) 
            { 
                MessageBox.Show($"خطأ في حفظ التقرير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); 
            }
        }

        public void PrintSalariesReport()
        {
            try
            {
                var salaries = employeeManagementProberty._salaries.ToList();
                var columnHeaders = new[] { "الموظف", "البيان / تفاصيل الفترة", "الراتب المستحق", "خصم السلفيات", "الصافي", "تاريخ الراتب", "تاريخ الدفع", "الحالة" };
                
                Services.PrintService.PrintReport(
                    "تقرير المرتبات",
                    "Salaries Report",
                    salaries,
                    columnHeaders,
                    (object item) => 
                    {
                        if (item == null) return new string[columnHeaders.Length];
                        var salary = item as EmployeeSalary;
                        if (salary == null) return new string[columnHeaders.Length];
                        return new[]
                        {
                            salary.Employee?.Name ?? "",
                            salary.Notes ?? "راتب شهري",
                            salary.BaseSalary.ToString("#,##0") + " ريال",
                            salary.AdvanceDeduction.ToString("#,##0") + " ريال",
                            salary.NetSalary.ToString("#,##0") + " ريال",
                            salary.SalaryDate.ToString("yyyy/MM/dd"),
                            salary.PaymentDate.ToString("yyyy/MM/dd"),
                            salary.IsPaid ? "مدفوع" : "غير مدفوع"
                        };
                    },
                    $"إجمالي المرتبات: {salaries.Count}\nإجمالي الصافي: {salaries.Sum(s => s.NetSalary):#,##0} ريال",
                    preferPdf: true
                );
            }
            catch (Exception ex) 
            { 
                MessageBox.Show($"خطأ في حفظ التقرير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); 
            }
        }
    }
}
