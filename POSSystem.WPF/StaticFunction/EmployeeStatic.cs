using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.StaticFunction
{
    public class EmployeeStatic
    {

        public static async Task LoadEmployeeAdvancesAsync(int employeeId, IEmployeeService _employeeService)
        {
            try
            {
                var advances = await _employeeService.GetEmployeeAdvancesAsync(employeeId);
                // Update advances for selected employee
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل سلفيات الموظف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        public static async Task LoadEmployeeSalariesAsync(int employeeId, IEmployeeService _employeeService)
        {
            try
            {
                var salaries = await _employeeService.GetEmployeeSalariesAsync(employeeId);
                // Update salaries for selected employee
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل مرتبات الموظف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }





    }
}
