using POSSystem.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business;
using POSSystem.WPF.Models;
using POSSystem.WPF.Services;
using POSSystem.WPF.Shered.Commands;
using POSSystem.WPF.Views.Popups;
using POSSystem.WPF.Views;

namespace POSSystem.WPF.ViewModels.Customers
{
    public partial class CustomerManagementViewModel : BaseViewModel, POSSystem.WPF.Services.ISearchable
    {
        public void ApplySearch(string query)
        {
            SearchText = query;
        }

        public CustomerManagementViewModel()
        {
            var sp = POSSystem.WPF.App.ServiceProvider;
            _customerDebtService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.ICustomerDebtService>(sp);
            _orderService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IOrderService>(sp);
            _shiftService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IShiftService>(sp);
            
            _debts = new POSSystem.WPF.Helpers.ObservableRangeCollection<CustomerDebt>();
            _unpaidDebts = new POSSystem.WPF.Helpers.ObservableRangeCollection<CustomerDebt>();
            Commands = new CustomerManagementCommands(this);
            InitializeAsync().SafeFireAndForget();
        }

        public async Task InitializeAsync() => await LoadDebtsAsync();

        private async Task<List<CustomerDebt>> BuildUnifiedCustomersAndDebtsListAsync()
        {
            ServiceLocator.UnitOfWork.ClearEntityTracking();
            var context = ServiceLocator.UnitOfWork.GetContext();

            var allDebts = (await _customerDebtService.GetAllDebtsAsync()).ToList();
            var allCustomers = await context.Customers
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var customerById = allCustomers.ToDictionary(c => c.Id);
            var customerByName = new Dictionary<string, Customer>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in allCustomers)
            {
                if (!string.IsNullOrWhiteSpace(c.Name) && !customerByName.ContainsKey(c.Name.Trim()))
                    customerByName[c.Name.Trim()] = c;
            }

            // Sync CreditLimit and Phone onto debt rows
            foreach (var debt in allDebts)
            {
                Customer? matchedCust = null;
                if (debt.CustomerId > 0 && customerById.TryGetValue(debt.CustomerId, out var byId))
                    matchedCust = byId;
                else if (!string.IsNullOrWhiteSpace(debt.CustomerName) && customerByName.TryGetValue(debt.CustomerName.Trim(), out var byName))
                    matchedCust = byName;

                if (matchedCust != null)
                {
                    debt.CustomerId = matchedCust.Id;
                    debt.Customer ??= matchedCust;
                    if (string.IsNullOrWhiteSpace(debt.PhoneNumber))
                        debt.PhoneNumber = matchedCust.PhoneNumber;
                    if (!debt.CreditLimit.HasValue || debt.CreditLimit.Value == 0)
                        debt.CreditLimit = matchedCust.CreditLimit;
                }
            }

            // Filter debts based on ShowUnpaidOnly if needed, BUT always include registered Customers so newly added customers always appear (Note 13)
            var filteredDebts = ShowUnpaidOnly
                ? allDebts.Where(d => !d.IsPaid).ToList()
                : allDebts.ToList();

            var representedCustomerIds = new HashSet<int>(
                filteredDebts.Where(d => d.CustomerId > 0).Select(d => d.CustomerId));
            var representedNames = new HashSet<string>(
                filteredDebts.Where(d => !string.IsNullOrWhiteSpace(d.CustomerName)).Select(d => d.CustomerName.Trim()),
                StringComparer.OrdinalIgnoreCase);

            foreach (var cust in allCustomers)
            {
                bool alreadyShown = representedCustomerIds.Contains(cust.Id) ||
                                    (!string.IsNullOrWhiteSpace(cust.Name) && representedNames.Contains(cust.Name.Trim()));
                if (!alreadyShown)
                {
                    string limitNote = cust.CreditLimit > 0
                        ? $"عميل مسجل | سقف المديونية: {cust.CreditLimit:N0} ريال"
                        : "عميل مسجل | سقف المديونية: مفتوح";

                    filteredDebts.Add(new CustomerDebt
                    {
                        Id = 0,
                        CustomerId = cust.Id,
                        Customer = cust,
                        CustomerName = cust.Name,
                        PhoneNumber = cust.PhoneNumber ?? string.Empty,
                        CreditLimit = cust.CreditLimit,
                        Amount = 0m,
                        PaidAmount = 0m,
                        IsPaid = true,
                        DebtDate = cust.CreatedAt,
                        PaymentDate = null,
                        Notes = limitNote,
                        CreatedAt = cust.CreatedAt,
                        UpdatedAt = cust.UpdatedAt
                    });
                }
            }

            return filteredDebts
                .OrderBy(d => d.IsPaid)
                .ThenByDescending(d => d.DebtDate)
                .ToList();
        }

        public async Task LoadDebtsAsync()
        {
            try
            {
                await _customerDebtService.ConsolidateOpenSameDayDebtsAsync();

                var unifiedList = await BuildUnifiedCustomersAndDebtsListAsync();
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    string q = SearchText.Trim();
                    unifiedList = unifiedList.Where(d =>
                        (d.CustomerName != null && d.CustomerName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                        (d.PhoneNumber != null && d.PhoneNumber.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                        (d.Notes != null && d.Notes.Contains(q, StringComparison.OrdinalIgnoreCase))).ToList();
                }

                _debts.ReplaceRange(unifiedList);
                _unpaidDebts.ReplaceRange(unifiedList.Where(d => !d.IsPaid && d.Amount > 0).ToList());

                DebtsPager.SetItems(unifiedList);

                await UpdateStatisticsAsync();
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في تحميل العملاء والديون: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private async Task UpdateStatisticsAsync()
        {
            try
            {
                TotalUnpaidDebts = await _customerDebtService.GetTotalUnpaidDebtsAsync();
                TotalPaidDebts = await _customerDebtService.GetTotalPaidDebtsAsync();
                UnpaidDebtsCount = _unpaidDebts.Count;
                OldDebtsCount = (await _customerDebtService.GetOldDebtsAsync(30)).Count();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error updating stats: {ex.Message}"); }
        }

        public void EditDebt(CustomerDebt? debt)
        {
            if (debt == null) return;
            try
            {
                var popup = new EditCustomerDebtPopup(debt);
                var owner = Application.Current.MainWindow;
                if (owner != null && owner != popup && owner.IsLoaded) popup.Owner = owner;
                else popup.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                if (popup.ShowDialog() == true) LoadDebtsAsync().SafeFireAndForget();
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في فتح نافذة التعديل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public async void DeleteDebt(CustomerDebt? debt)
        {
            if (debt == null) return;
            if (debt.Id == 0 && debt.CustomerId > 0)
            {
                if (MessageBox.Show($"هل تريد حذف العميل '{debt.CustomerName}' من قائمة العملاء؟", "تأكيد حذف العميل", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        var context = ServiceLocator.UnitOfWork.GetContext();
                        var cust = await context.Customers.FirstOrDefaultAsync(c => c.Id == debt.CustomerId);
                        if (cust != null)
                        {
                            cust.IsActive = false;
                            context.Customers.Update(cust);
                            await ServiceLocator.UnitOfWork.SaveChangesAsync();
                        }
                        await LoadDebtsAsync();
                        MessageBox.Show("تم حذف العميل بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex) { MessageBox.Show($"خطأ في حذف العميل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
                return;
            }

            if (MessageBox.Show($"هل تريد حذف دين {debt.CustomerName} بقيمة {debt.Amount} ريال؟", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    await _customerDebtService.DeleteDebtAsync(debt.Id);
                    await LoadDebtsAsync();
                    MessageBox.Show("تم حذف الدين بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show($"خطأ في حذف الدين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        public async void MarkAsPaid(CustomerDebt? debt)
        {
            if (debt == null || debt.Id == 0 || debt.IsPaid) return;
            var currentUser = AdminDashboardWindow._currentUser;
            if (currentUser == null) { MessageBox.Show("لم يتم العثور على مستخدم مسجل الدخول", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); return; }
            var activeShift = await _shiftService.GetActiveShiftByUserIdAsync(currentUser.Id);
            string shiftInfo = activeShift != null ? $"\nسيتم تسجيل المبلغ في مناوبة: {activeShift.ShiftNumber}" : "\n(لا توجد مناوبة نشطة)";
            if (MessageBox.Show($"هل تريد تسجيل سداد دين كامل لـ {debt.CustomerName} بقيمة {debt.RemainingAmount} ريال؟{shiftInfo}", "تأكيد السداد", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    await _customerDebtService.MarkDebtAsPaidAsync(debt.Id, currentUser.Id, activeShift?.Id);

                    var paidDebt = new CustomerDebt 
                    { 
                        Id = debt.Id,
                        CustomerName = debt.CustomerName,
                        PhoneNumber = debt.PhoneNumber,
                        Amount = debt.Amount,
                        PaidAmount = debt.Amount,
                        IsPaid = true,
                        PaymentDate = DateTime.Now,
                        UserId = currentUser.Id,
                        ShiftId = activeShift?.Id
                    };

                    await LoadDebtsAsync();

                    MessageBox.Show("تم تسجيل السداد بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                    _ = Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            var dummyOrder = new Order { Total = paidDebt.Amount, OrderItems = new System.Collections.Generic.List<OrderItem>(), OrderNumber = "DEBT-" + paidDebt.Id, CreatedAt = DateTime.Now };
                            POSSystem.WPF.StaticFunction.GloublStatic.PrintReceipt(dummyOrder, paidDebt);
                        }
                        catch { }
                    }));
                }
                catch (Exception ex) { MessageBox.Show($"خطأ في تسجيل السداد: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        public void RepayDebt(CustomerDebt? debt)
        {
            if (debt == null || debt.Id == 0)
            {
                MessageBox.Show("هذا العميل ليس عليه مديونية مفتوحة حالياً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try
            {
                var popup = new RepayCustomerDebtPopup(debt);
                var owner = Application.Current.MainWindow;
                if (owner != null && owner.IsLoaded) popup.Owner = owner;
                if (popup.ShowDialog() == true) { LoadDebtsAsync().SafeFireAndForget(); }
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في فتح نافذة التسديد: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public void NavigateToPayments() => ServiceLocator.NavigationService.ShowPaymentsView();

        public void ViewProducts(CustomerDebt? debt)
        {
            if (debt == null || debt.Id == 0)
            {
                MessageBox.Show("لا توجد فواتير دين مرتبطة بهذا السجل.", "معلومة", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try
            {
                var popup = new DebtProductsPopup(debt);
                var owner = Application.Current.MainWindow;
                if (owner != null && owner.IsLoaded) popup.Owner = owner;
                popup.ShowDialog();
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في فتح تفاصيل المنتجات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public void ViewLedger(CustomerDebt? debt)
        {
            if (debt == null) return;
            try
            {
                var popup = new CustomerLedgerPopup(debt);
                var owner = Application.Current.MainWindow;
                if (owner != null && owner.IsLoaded) popup.Owner = owner;
                popup.ShowDialog();
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في فتح كشف حساب العميل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public async Task SearchDebtsAsync()
        {
            await LoadDebtsAsync();
        }

        public void ToggleView() => ShowUnpaidOnly = !ShowUnpaidOnly;
        public void AddDebt() { }
    }
}
