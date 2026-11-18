using POSSystem.WPF.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using POSSystem.WPF.Models;
using POSSystem.WPF.Business;
using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.Views.Popups;

namespace POSSystem.WPF.ViewModels.Suppliers
{
    public partial class SupplierManagementViewModel : BaseViewModel
    {
        private readonly Debouncer _searchDebouncer = new Debouncer(500);

        public SupplierManagementViewModel()
        {
            _supplierService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.ISupplierService>(POSSystem.WPF.App.ServiceProvider);
            Property = new SupplierManagementProberty(_supplierService);
            Commands = new SupplierManagementCommands(this);

            Property.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Property.SelectedSupplier) && Property.SelectedSupplier != null) LoadSupplierHistoryAsync(Property.SelectedSupplier.Id).SafeFireAndForget();
                if (e.PropertyName == nameof(Property.SearchText)) _searchDebouncer.RunAsync(() => LoadSuppliersAsync()).SafeFireAndForget();
            };
            InitializeAsync().SafeFireAndForget();
            ServiceLocator.SuppliersChanged += async () => { if (!_isInternalChange) await LoadSuppliersAsync(); };
        }

        private bool _isInternalChange = false;

        public async Task InitializeAsync() { await LoadSuppliersAsync(); UpdateSummaryCards(); }

        public async Task LoadSuppliersAsync()
        {
            try
            {
                var suppliers = (await _supplierService.GetAllSuppliersAsync()).ToList();
                if (!string.IsNullOrWhiteSpace(Property.SearchText)) suppliers = suppliers.Where(s => s.Name.Contains(Property.SearchText, StringComparison.OrdinalIgnoreCase) || (s.VatNumber != null && s.VatNumber.Contains(Property.SearchText)) || (s.PhoneNumber != null && s.PhoneNumber.Contains(Property.SearchText))).ToList();
                Property.Suppliers.Clear();
                foreach (var s in suppliers) Property.Suppliers.Add(s);
                UpdateSummaryCards(); Property.NotifySuppliersChanged(); SuppliersChanged?.Invoke();
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في تحميل الموردين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void UpdateSummaryCards()
        {
            Property.TotalSuppliersCount = Property.Suppliers.Count;
            Property.TotalSuppliersDebt = Property.Suppliers.Sum(s => s.TotalBalance);
            Property.OverdueSuppliersCount = Property.Suppliers.Count(s => s.TotalBalance > 0);
        }

        public async void AddSupplier()
        {
            try { var popup = new AddEditSupplierPopup(); var owner = Application.Current.MainWindow; if (owner != null && owner.IsLoaded) popup.Owner = owner; if (popup.ShowDialog() == true) { _isInternalChange = true; try { await LoadSuppliersAsync(); ServiceLocator.NotifySuppliersChanged(); } finally { _isInternalChange = false; } } }
            catch (Exception ex) { MessageBox.Show($"خطأ في إضافة مورد: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public async void EditSupplier(Supplier? supplier)
        {
            if (supplier == null) return;
            try { var popup = new AddEditSupplierPopup(supplier); var owner = Application.Current.MainWindow; if (owner != null && owner.IsLoaded) popup.Owner = owner; if (popup.ShowDialog() == true) { _isInternalChange = true; try { await LoadSuppliersAsync(); ServiceLocator.NotifySuppliersChanged(); } finally { _isInternalChange = false; } } }
            catch (Exception ex) { MessageBox.Show($"خطأ في تعديل مورد: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public async void DeleteSupplier(Supplier? supplier)
        {
            if (supplier == null) return;
            if (MessageBox.Show($"هل أنت متأكد من حذف المورد {supplier.Name}؟", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try { await _supplierService.DeleteSupplierAsync(supplier.Id); Property.Suppliers.Remove(supplier); Property.NotifySuppliersChanged(); UpdateSummaryCards(); _isInternalChange = true; try { SuppliersChanged?.Invoke(); ServiceLocator.NotifySuppliersChanged(); } finally { _isInternalChange = false; } }
                catch (Exception ex) { MessageBox.Show($"خطأ في حذف المورد: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        public async void RecordPayment()
        {
            if (Property.SelectedSupplier == null) { MessageBox.Show("الرجاء اختيار مورد أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            try { var popup = new RecordSupplierPaymentPopup(Property.SelectedSupplier); var owner = Application.Current.MainWindow; if (owner != null && owner.IsLoaded) popup.Owner = owner; if (popup.ShowDialog() == true) { _isInternalChange = true; try { await LoadSuppliersAsync(); ServiceLocator.NotifySuppliersChanged(); } finally { _isInternalChange = false; } } }
            catch (Exception ex) { MessageBox.Show($"خطأ في تسجيل دفعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public async Task LoadSupplierHistoryAsync(int supplierId)
        {
            try
            {
                var purchases = await _supplierService.GetSupplierPurchasesAsync(supplierId);
                var payments = await _supplierService.GetSupplierPaymentsAsync(supplierId);
                var ledger = await _supplierService.GetSupplierLedgerAsync(supplierId);

                Property.SelectedSupplierPurchases.Clear(); 
                foreach (var p in purchases) Property.SelectedSupplierPurchases.Add(p);

                Property.SelectedSupplierPayments.Clear(); 
                foreach (var p in payments) Property.SelectedSupplierPayments.Add(p);

                Property.SelectedSupplierLedger.Clear();
                foreach (var item in ledger) Property.SelectedSupplierLedger.Add(item);
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في تحميل تاريخ العمليات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
}
