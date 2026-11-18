using System.Windows.Input;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;
using POSSystem.WPF.ViewModels.Employees;
using POSSystem.WPF.ViewModels;

namespace POSSystem.WPF.Shered.Commands
{
    public class EmployeeManagementCommands
    {
        public ICommand AddEmployeeCommand { get; }
        public ICommand EditEmployeeCommand { get; }
        public ICommand DeleteEmployeeCommand { get; }
        public ICommand ShowEmployeeStatementCommand { get; }

        public ICommand AddAdvanceCommand { get; }
        public ICommand EditAdvanceCommand { get; }
        public ICommand DeleteAdvanceCommand { get; }
        public ICommand MarkAdvanceAsRepaidCommand { get; }
        public ICommand RepayAdvanceCommand { get; }
        public ICommand RepayInstallmentCommand { get; }

        public ICommand ProcessSalariesCommand { get; }
        public ICommand PaySalaryCommand { get; }
        public ICommand PayAllSalariesCommand { get; }

        public ICommand ShowEmployeesViewCommand { get; }
        public ICommand ShowAdvancesViewCommand { get; }
        public ICommand ShowSalariesViewCommand { get; }
        public ICommand ShowAdvanceReportCommand { get; }
        public ICommand ShowSalaryReportCommand { get; }

        public ICommand PrintEmployeesReportCommand { get; }
        public ICommand PrintAdvancesReportCommand { get; }
        public ICommand PrintSalariesReportCommand { get; }

        public EmployeeManagementCommands(EmployeeManagementViewModel vm)
        {
            AddEmployeeCommand = new RelayCommand(() => vm.AddEmployee());
            EditEmployeeCommand = new RelayCommand<User>(vm.EditEmployee, e => e != null);
            DeleteEmployeeCommand = new RelayCommand<User>(vm.DeleteEmployee, e => e != null);
            ShowEmployeeStatementCommand = new RelayCommand<User>(vm.ShowEmployeeStatement);

            AddAdvanceCommand = new RelayCommand(() => vm.AddAdvance());
            EditAdvanceCommand = new RelayCommand<EmployeeAdvance>(vm.EditAdvance, a => a != null);
            DeleteAdvanceCommand = new RelayCommand<EmployeeAdvance>(vm.DeleteAdvance, a => a != null);
            MarkAdvanceAsRepaidCommand =
                new RelayCommand<EmployeeAdvance>(vm.MarkAdvanceAsRepaid, a => a != null && !a.IsRepaid);
            RepayAdvanceCommand = 
                new RelayCommand<EmployeeAdvance>(vm.RepayAdvance, a => a != null && !a.IsRepaid);
            RepayInstallmentCommand =
                new RelayCommand<EmployeeAdvance>(vm.RepayInstallment, a => a != null && !a.IsRepaid);

            ProcessSalariesCommand = new RelayCommand(() => vm.ProcessSalaries().SafeFireAndForget());
            PaySalaryCommand =
                new RelayCommand<EmployeeSalary>(vm.PaySalary, s => s != null && !s.IsPaid);
            PayAllSalariesCommand = new RelayCommand(() => vm.PayAllSalaries().SafeFireAndForget());

            ShowEmployeesViewCommand =
                new RelayCommand(() => vm.employeeManagementProberty.CurrentView = "Employees");

            ShowAdvancesViewCommand = new RelayCommand(() =>
            {
                vm.employeeManagementProberty.CurrentView = "Advances";
                _ = vm.LoadUnpaidAdvancesAsync();
            });

            ShowSalariesViewCommand = new RelayCommand(() =>
            {
                vm.employeeManagementProberty.CurrentView = "Salaries";
                _ = vm.LoadUnpaidSalariesAsync();
            });

            ShowAdvanceReportCommand = new RelayCommand(() =>
            {
                vm.employeeManagementProberty.CurrentView = "AdvanceReport";
                _ = vm.LoadAllAdvancesForReportAsync();
            });

            ShowSalaryReportCommand = new RelayCommand(() =>
            {
                vm.employeeManagementProberty.CurrentView = "SalaryReport";
                _ = vm.LoadAllSalariesForReportAsync();
            });

            PrintEmployeesReportCommand = new RelayCommand(() => vm.PrintEmployeesReport());
            PrintAdvancesReportCommand = new RelayCommand(() => vm.PrintAdvancesReport());
            PrintSalariesReportCommand = new RelayCommand(() => vm.PrintSalariesReport());
        }
    }
}
