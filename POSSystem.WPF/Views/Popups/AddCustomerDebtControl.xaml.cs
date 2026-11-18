using POSSystem.WPF.Models;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Views.AdminPages;
using POSSystem.WPF.Views;
using POSSystem.WPF.ViewModels.Customers;
using POSSystem.WPF.ViewModels;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.WPF.Data;
using POSSystem.WPF.Data.UnitOfWork;

namespace POSSystem.WPF.Views.Popups
{
    public partial class AddCustomerDebtControl : UserControl
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CustomerManagementViewModel? _parentViewModel;
        private readonly IServiceScope _scope;

        public AddCustomerDebtControl(CustomerManagementViewModel? parentViewModel = null)
        {
            InitializeComponent();
            _parentViewModel = parentViewModel;
            _scope = App.ServiceProvider.CreateScope();
            var context = _scope.ServiceProvider.GetRequiredService<POSDbContext>();
            _unitOfWork = new UnitOfWork(context);
            Unloaded += (_, _) => 
            {
                _unitOfWork.Dispose();
                _scope.Dispose();
            };
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = CustomerNameTextBox.Text?.Trim() ?? string.Empty;
                string phone = PhoneNumberTextBox.Text?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("يرجى إدخال اسم العميل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    CustomerNameTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(phone))
                {
                    MessageBox.Show("يرجى إدخال رقم الهاتف", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    PhoneNumberTextBox.Focus();
                    return;
                }

                decimal limit = 0m;
                string limitRaw = (CreditLimitTextBox.Text ?? "0").Replace(",", "").Trim();
                if (!string.IsNullOrWhiteSpace(limitRaw) &&
                    (!decimal.TryParse(limitRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out limit) || limit < 0))
                {
                    MessageBox.Show("يرجى إدخال سقف مديونية صحيح (0 تعني مفتوح)", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    CreditLimitTextBox.Focus();
                    return;
                }

                var context = _unitOfWork.GetContext();
                var existing = await context.Customers.FirstOrDefaultAsync(c =>
                    c.Name.ToLower() == name.ToLower() ||
                    (!string.IsNullOrWhiteSpace(phone) && c.PhoneNumber == phone));

                if (existing != null)
                {
                    existing.Name = name;
                    existing.PhoneNumber = phone;
                    existing.CreditLimit = limit;
                    existing.IsActive = true;
                    existing.UpdatedAt = DateTime.Now;
                    context.Customers.Update(existing);

                    // Also sync CreditLimit on any existing CustomerDebts for this customer
                    var relatedDebts = await context.CustomerDebts
                        .Where(d => d.CustomerId == existing.Id || d.CustomerName == existing.Name)
                        .ToListAsync();
                    foreach (var d in relatedDebts)
                    {
                        d.CustomerId = existing.Id;
                        d.PhoneNumber = phone;
                        d.CreditLimit = limit;
                    }
                }
                else
                {
                    var newCustomer = new Customer
                    {
                        Name = name,
                        PhoneNumber = phone,
                        CreditLimit = limit,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    await context.Customers.AddAsync(newCustomer);
                }

                await _unitOfWork.SaveChangesAsync();
                MessageBox.Show("تم حفظ بيانات العميل بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                if (_parentViewModel != null)
                {
                    await _parentViewModel.LoadDebtsAsync();
                }

                CloseOverlay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ العميل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CloseOverlay();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseOverlay();
        }

        private void CloseOverlay()
        {
            var window = Window.GetWindow(this) as AdminDashboardWindow;
            window?.CloseOverlay();
        }
    }
}
