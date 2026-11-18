using POSSystem.WPF.Models;
using POSSystem.WPF.Views.Popups;
using POSSystem.WPF.ViewModels.Customers;
using POSSystem.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace POSSystem.WPF.Views.AdminPages
{
    public partial class CustomerManagementView : UserControl
    {
        private CustomerManagementViewModel _viewModel;

        public CustomerManagementView()
        {
            InitializeComponent();
            _viewModel = new CustomerManagementViewModel();
            DataContext = _viewModel;
            Loaded += async (_, _) =>
            {
                if (_viewModel != null)
                    await _viewModel.LoadDebtsAsync();
            };
        }


        private async void RefreshCustomers_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
                await _viewModel.LoadDebtsAsync();
        }

        private void AddNewCustomer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addCustomerControl = new AddCustomerDebtControl(_viewModel);
                if (Window.GetWindow(this) is AdminDashboardWindow adminWindow)
                {
                    adminWindow.ShowOverlay(addCustomerControl);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح نافذة إضافة عميل جديد: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintReport_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (_viewModel == null) return;
                
                var debts = _viewModel.Debts?.ToList() ?? new List<CustomerDebt>();
                var columnHeaders = new[] { "اسم العميل", "رقم الهاتف", "المبلغ", "المبلغ المتبقي", "تاريخ الدين", "تاريخ السداد", "الحالة" };
                
                Services.PrintService.PrintReport(
                    "تقرير العملاء والديون",
                    "Customers & Debts Report",
                    debts,
                    columnHeaders,
                    (object item) => 
                    {
                        var debt = item as CustomerDebt;
                        if (debt == null) return new string[columnHeaders.Length];
                        return new[]
                        {
                            debt.CustomerName ?? "",
                            debt.PhoneNumber ?? "-",
                            debt.Amount.ToString("#,##0") + " ريال",
                            debt.RemainingAmount.ToString("#,##0") + " ريال",
                            debt.DebtDate.ToString("yyyy/MM/dd"),
                            debt.PaymentDate?.ToString("yyyy/MM/dd") ?? "لم يتم السداد",
                            debt.IsPaid ? "مدفوع" : "غير مدفوع"
                        };
                    },
                    $"إجمالي الديون: {debts.Count}\nإجمالي المبلغ: {debts.Sum(d => d.Amount):#,##0} ريال\nإجمالي المتبقي: {debts.Sum(d => d.RemainingAmount):#,##0} ريال",
                    preferPdf: true
                );
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ في حفظ التقرير: {ex.Message}", "خطأ", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void RowActionsMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;
            if (button.ContextMenu is null) return;

            // Open the menu on normal left click (WPF default opens context menus on right click).
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            button.ContextMenu.IsOpen = true;
        }

    }
}

