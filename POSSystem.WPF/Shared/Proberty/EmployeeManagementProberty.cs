using POSSystem.WPF.StaticFunction;
using POSSystem.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.Shered.Proberty
{
    public class EmployeeManagementProberty:BaseModel
    {

        public IEmployeeService _EmployeeService;
        public EmployeeManagementProberty(IEmployeeService employeeService)
        {
            _EmployeeService = employeeService;
            _employees = new ObservableCollection<User>();
            _advances = new ObservableCollection<EmployeeAdvance>();
            _salaries = new ObservableCollection<EmployeeSalary>();
            AdvancesReportPagination = new Pagination<EmployeeAdvance>();
            SalariesReportPagination = new Pagination<EmployeeSalary>();
        }



       public ObservableCollection<User> _employees;
       public ObservableCollection<EmployeeAdvance> _advances;
       public ObservableCollection<EmployeeSalary> _salaries;
        private User? _selectedEmployee;
        private EmployeeAdvance? _selectedAdvance;
        private EmployeeSalary? _selectedSalary;
        private string _searchText = "";
        private string _currentView = "Employees";


        public ObservableCollection<User> Employees 
        { 
            get => _employees; 
            set { _employees = value; OnPropertyChanged(); } 
        }
        public ObservableCollection<EmployeeAdvance> Advances 
        { 
            get => _advances; 
            set { _advances = value; OnPropertyChanged(); } 
        }
        public ObservableCollection<EmployeeSalary> Salaries 
        { 
            get => _salaries; 
            set { _salaries = value; OnPropertyChanged(); } 
        }

        public Pagination<EmployeeAdvance> AdvancesReportPagination { get; }
        public Pagination<EmployeeSalary> SalariesReportPagination { get; }

        private decimal _totalAdvancesAmount;
        public decimal TotalAdvancesAmount
        {
            get => _totalAdvancesAmount;
            set { _totalAdvancesAmount = value; OnPropertyChanged(); }
        }

        private decimal _totalAdvancesPaid;
        public decimal TotalAdvancesPaid
        {
            get => _totalAdvancesPaid;
            set { _totalAdvancesPaid = value; OnPropertyChanged(); }
        }

        private decimal _totalAdvancesUnpaid;
        public decimal TotalAdvancesUnpaid
        {
            get => _totalAdvancesUnpaid;
            set { _totalAdvancesUnpaid = value; OnPropertyChanged(); }
        }

        private decimal _totalSalariesBase;
        public decimal TotalSalariesBase
        {
            get => _totalSalariesBase;
            set { _totalSalariesBase = value; OnPropertyChanged(); }
        }

        private decimal _totalSalariesDeduction;
        public decimal TotalSalariesDeduction
        {
            get => _totalSalariesDeduction;
            set { _totalSalariesDeduction = value; OnPropertyChanged(); }
        }

        private decimal _totalSalariesNet;
        public decimal TotalSalariesNet
        {
            get => _totalSalariesNet;
            set { _totalSalariesNet = value; OnPropertyChanged(); }
        }

        public User? SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                _selectedEmployee = value;
                OnPropertyChanged();
                if (value != null)
                {
                    POSSystem.WPF.Helpers.TaskExtensions.RunSafe(async () =>
                    {
                        await EmployeeStatic.LoadEmployeeAdvancesAsync(value.Id, _EmployeeService);
                        await EmployeeStatic.LoadEmployeeSalariesAsync(value.Id, _EmployeeService);
                    });
                }
            }
        }

        public EmployeeAdvance? SelectedAdvance
        {
            get => _selectedAdvance;
            set
            {
                _selectedAdvance = value;
                OnPropertyChanged();
            }
        }

        public EmployeeSalary? SelectedSalary
        {
            get => _selectedSalary;
            set
            {
                _selectedSalary = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                //FilterEmployees();
            }
        }

        public string CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEmployeesView));
                OnPropertyChanged(nameof(IsAdvancesView));
                OnPropertyChanged(nameof(IsSalariesView));
                OnPropertyChanged(nameof(IsAdvanceReportView));
                OnPropertyChanged(nameof(IsSalaryReportView));
            }
        }

        public bool IsEmployeesView => CurrentView == "Employees";
        public bool IsAdvancesView => CurrentView == "Advances";
        public bool IsSalariesView => CurrentView == "Salaries";
        public bool IsAdvanceReportView => CurrentView == "AdvanceReport";
        public bool IsSalaryReportView => CurrentView == "SalaryReport";






        //public static async void LoadEmployeeAdvancesAsync(int employeeId,IEmployeeService _employeeService)
        //{
        //    try
        //    {
        //        var advances = await _employeeService.GetEmployeeAdvancesAsync(employeeId);
        //        // Update advances for selected employee
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"خطأ في تحميل سلفيات الموظف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}



    }
}
