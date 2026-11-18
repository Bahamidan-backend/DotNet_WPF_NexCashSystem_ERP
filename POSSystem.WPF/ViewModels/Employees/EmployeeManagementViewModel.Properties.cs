using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.Models;
using System.Collections.Generic;

namespace POSSystem.WPF.ViewModels.Employees
{
    public partial class EmployeeManagementViewModel
    {
        private readonly IEmployeeService _employeeService;
        public EmployeeManagementProberty employeeManagementProberty { get; }
        private List<User> _allEmployees = new List<User>();
    }
}
