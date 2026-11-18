using POSSystem.WPF.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Models;
using POSSystem.WPF.Business;
using POSSystem.WPF.Business.Services.interfase;

namespace POSSystem.WPF.ViewModels.Purchases
{
    public class PurchaseReturnItemViewModel : BaseViewModel
    {
        private readonly PurchaseItem _originalItem;
        private int _returnQuantity;
        private string _reasonCode = "أخرى";

        public PurchaseReturnItemViewModel(PurchaseItem originalItem, int previouslyReturnedQuantity = 0, decimal warehouseStock = -1)
        {
            _originalItem = originalItem;
            _returnQuantity = 0;
            PreviouslyReturnedQuantity = previouslyReturnedQuantity;
            // -1 means warehouse stock is unknown — fall back to invoice quantity
            WarehouseStock = warehouseStock >= 0 ? warehouseStock : int.MaxValue;
        }

        public int PreviouslyReturnedQuantity { get; }
        /// <summary>Current warehouse quantity for this item (used to cap MaxQuantity).</summary>
        public decimal WarehouseStock { get; }
        public int OriginalPurchaseItemId => _originalItem.Id;
        public string ProductName => _originalItem.DisplayName;
        public decimal UnitPrice => _originalItem.UnitPrice;

        /// <summary>
        /// وحدة الشراء المعتمدة (العبوة) للصنف، مع تجنب وحدات الكيلو والجرام
        /// </summary>
        public string UnitName
        {
            get
            {
                var product = _originalItem.Product;
                if (product?.PortionUnit != null)
                {
                    return PortionUnitHelper.GetDisplayName(product.PortionUnit.Value);
                }
                return "عبوة/وحدة";
            }
        }

        /// <summary>
        /// Max returnable = MIN(invoice qty - already returned, current warehouse stock).
        /// This prevents returning items that have already been sold.
        /// </summary>
        public int MaxQuantity => Math.Max(0,
            Math.Min(
                _originalItem.Quantity - PreviouslyReturnedQuantity,
                (int)Math.Floor(WarehouseStock)
            ));

        public int ReturnQuantity
        {
            get => _returnQuantity;
            set
            {
                if (value >= 0 && value <= MaxQuantity)
                {
                    _returnQuantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalAmount));
                    OnPropertyChanged(nameof(IsSelectedForReturn));
                }
            }
        }

        public bool IsSelectedForReturn => ReturnQuantity > 0;
        public decimal TotalAmount => ReturnQuantity * UnitPrice;
        public string ReasonCode { get => _reasonCode; set { _reasonCode = value; OnPropertyChanged(); } }
    }

    public partial class PurchaseReturnViewModel : BaseViewModel, POSSystem.WPF.Services.ISearchable
    {
        public PurchaseReturnViewModel(User currentUser)
        {
            var sp = POSSystem.WPF.App.ServiceProvider;
            _unitOfWork = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Data.UnitOfWork.IUnitOfWork>(sp);
            _purchaseReturnService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IPurchaseReturnService>(sp);
            _purchaseService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IPurchaseService>(sp);
            _currentUser = currentUser;

            SearchPurchaseCommand = new RelayCommand(async () => await SearchPurchaseAsync());
            ProcessReturnCommand = new RelayCommand(async () => await ProcessReturnAsync(), CanProcessReturn);
            AddAllToReturnCommand = new RelayCommand(AddAllToReturn);
            ClearSupplierFilterCommand = new RelayCommand(() => SelectedSupplierFilter = null);
            SelectRefundChannelQuickCommand = new RelayCommand<string>(ch =>
            {
                if (!string.IsNullOrWhiteSpace(ch))
                    SelectedRefundChannel = ch;
            });

            LoadPurchasesAsync().SafeFireAndForget();
            ServiceLocator.InventoryChanged += async () => await LoadPurchasesAsync();
            ServiceLocator.SuppliersChanged += async () => await LoadPurchasesAsync();
        }

        private async Task LoadPurchasesAsync()
        {
            try
            {
                // Load all active suppliers for flexible filtering
                var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
                AllSuppliers.Clear();
                foreach (var s in suppliers.Where(s => !s.IsDeleted).OrderBy(s => s.Name))
                {
                    AllSuppliers.Add(s);
                }

                var purchases = await _unitOfWork.Purchases
                    .IncludeMultiple(p => p.PurchaseReturns)
                    .ToListAsync();

                AllPurchases.Clear();
                foreach (var p in purchases.OrderByDescending(x => x.PurchaseDate))
                {
                    decimal totalReturned = p.PurchaseReturns?.Sum(r => r.TotalAmount) ?? 0;
                    if (totalReturned < p.TotalAmount) AllPurchases.Add(p);
                }

                ApplySupplierFilter();
            }
            catch (Exception ex) { Console.WriteLine($"Error loading purchases: {ex.Message}"); }
        }

        public void ApplySearch(string query)
        {
            SearchInvoiceNumber = query ?? string.Empty;
            ApplySupplierFilter();
        }

        public void ApplySupplierFilter()
        {
            FilteredPurchases.Clear();
            var query = AllPurchases.AsEnumerable();

            if (SelectedSupplierFilter != null)
            {
                query = query.Where(p => 
                    (p.SupplierId.HasValue && p.SupplierId.Value == SelectedSupplierFilter.Id) ||
                    (!string.IsNullOrWhiteSpace(p.SupplierName) && p.SupplierName.Equals(SelectedSupplierFilter.Name, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrWhiteSpace(SearchInvoiceNumber))
            {
                var q = SearchInvoiceNumber.Trim();
                query = query.Where(p =>
                    (p.PurchaseNumber != null && p.PurchaseNumber.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (p.InvoiceNumber != null && p.InvoiceNumber.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (p.SupplierName != null && p.SupplierName.Contains(q, StringComparison.OrdinalIgnoreCase)));
            }

            foreach (var p in query)
            {
                FilteredPurchases.Add(p);
            }
        }

        private async Task SearchPurchaseAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchInvoiceNumber))
            {
                MessageBox.Show("الرجاء إدخال رقم فاتورة المشتريات أو اختيارها من القائمة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var purchases = await _unitOfWork.Purchases.GetAllAsync();
                var purchase = purchases.FirstOrDefault(p => 
                    (p.PurchaseNumber != null && p.PurchaseNumber.Contains(SearchInvoiceNumber, StringComparison.OrdinalIgnoreCase)) || 
                    (p.InvoiceNumber != null && p.InvoiceNumber.Contains(SearchInvoiceNumber, StringComparison.OrdinalIgnoreCase)));
                
                if (purchase == null)
                {
                    MessageBox.Show("فاتورة المشتريات غير موجودة.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    CurrentPurchase = null;
                    ReturnItems.Clear();
                    return;
                }

                var detailedPurchase = await _purchaseService.GetPurchaseWithItemsAsync(purchase.Id);
                if (detailedPurchase == null) return;

                CurrentPurchase = detailedPurchase;
                ReturnItems.Clear();
                SupplierName = detailedPurchase.SupplierName;
                PurchaseDate = detailedPurchase.PurchaseDate.ToString("g");
                OriginalPaymentMethod = detailedPurchase.PaymentMethodDisplay;

                if (detailedPurchase.PaymentMethod == PaymentMethod.OnCredit)
                {
                    SelectedSettlementMode = PurchaseReturnSettlementMode.DebitNote;
                }
                else
                {
                    // إذا كانت الفاتورة مدفوعة (عبر حاسب كريمي أو أي محفظة أو نقداً)، نختار تلقائياً استرداد المبلغ لنفس المحفظة/الحساب
                    SelectedSettlementMode = PurchaseReturnSettlementMode.CashReceipt;
                    var depWallet = detailedPurchase.DepositInfo?.BankOrWallet;
                    if (!string.IsNullOrWhiteSpace(depWallet))
                    {
                        if (!AvailableRefundChannels.Contains(depWallet))
                            AvailableRefundChannels.Insert(0, depWallet);
                        SelectedRefundChannel = depWallet;
                    }
                    else if (detailedPurchase.PaymentMethod == PaymentMethod.Cash)
                    {
                        SelectedRefundChannel = "الصندوق الرئيسي";
                    }
                }

                var previousReturns = await _unitOfWork.PurchaseReturns.FindAsync(r => r.OriginalPurchaseId == detailedPurchase.Id);
                var returnIds = previousReturns.Select(r => r.Id).ToList();
                var previousReturnItems = await _unitOfWork.PurchaseReturnItems.FindAsync(ri => returnIds.Contains(ri.PurchaseReturnId));
                
                var returnedQuantities = previousReturnItems
                    .GroupBy(ri => ri.OriginalPurchaseItemId)
                    .ToDictionary(g => g.Key, g => g.Sum(ri => ri.Quantity));

                if (detailedPurchase.PurchaseItems != null)
                {
                    // Fetch all warehouse products & addons ONCE to get current stock and portion units
                    var allWarehouseProducts = await _unitOfWork.warehouseFromProduct.GetAllAsync();
                    var warehouseById = allWarehouseProducts.ToDictionary(w => w.Id);

                    var allAddons = await _unitOfWork.Addons.GetAllAsync();
                    var addonsById = allAddons.ToDictionary(a => a.Id);

                    foreach (var item in detailedPurchase.PurchaseItems)
                    {
                        int alreadyReturned = returnedQuantities.ContainsKey(item.Id) ? returnedQuantities[item.Id] : 0;

                        // Get current warehouse stock for this item
                        decimal warehouseStock = -1;
                        if (item.ProductId.HasValue && warehouseById.TryGetValue(item.ProductId.Value, out var wp))
                        {
                            warehouseStock = wp.StockQuantity;
                            if (item.Product == null) item.Product = wp;
                        }
                        else if (item.AddonId.HasValue && addonsById.TryGetValue(item.AddonId.Value, out var addon))
                        {
                            warehouseStock = addon.Quantity;
                            if (item.Addon == null) item.Addon = addon;
                        }

                        var vm = new PurchaseReturnItemViewModel(item, alreadyReturned, warehouseStock);
                        vm.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(PurchaseReturnItemViewModel.TotalAmount) || e.PropertyName == nameof(PurchaseReturnItemViewModel.ReturnQuantity))
                            {
                                OnPropertyChanged(nameof(TotalReturnAmount));
                                ((RelayCommand)ProcessReturnCommand).RaiseCanExecuteChanged();
                            }
                        };
                        ReturnItems.Add(vm);
                    }
                }

                OnPropertyChanged(nameof(TotalReturnAmount));
            }
            catch (Exception ex) { MessageBox.Show($"خطأ أثناء البحث عن الفاتورة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void AddAllToReturn()
        {
            if (CurrentPurchase == null) return;
            foreach (var item in ReturnItems) item.ReturnQuantity = item.MaxQuantity;
            OnPropertyChanged(nameof(TotalReturnAmount));
            ((RelayCommand)ProcessReturnCommand).RaiseCanExecuteChanged();
        }

        private bool CanProcessReturn() => CurrentPurchase != null && ReturnItems.Any(x => x.ReturnQuantity > 0) && (!string.IsNullOrWhiteSpace(ReturnReason) || !string.IsNullOrWhiteSpace(ReturnNotes));

        private async Task ProcessReturnAsync()
        {
            if (!CanProcessReturn()) return;
            try
            {
                var finalReason = string.Join(" - ", new[] { ReturnReason, ReturnNotes }.Where(s => !string.IsNullOrWhiteSpace(s)));
                var returnNumber = await _purchaseReturnService.GenerateReturnNumberAsync();
                if (SelectedSettlementMode == PurchaseReturnSettlementMode.CashReceipt && !string.IsNullOrWhiteSpace(SelectedRefundChannel))
                {
                    finalReason = string.IsNullOrWhiteSpace(finalReason)
                        ? $"[استرداد إلى: {SelectedRefundChannel.Trim()}]"
                        : $"{finalReason} - [استرداد إلى: {SelectedRefundChannel.Trim()}]";
                }

                var purchaseReturn = new PurchaseReturn
                {
                    ReturnNumber = returnNumber,
                    OriginalPurchaseId = CurrentPurchase!.Id,
                    UserId = _currentUser.Id,
                    Reason = finalReason,
                    ReturnMethod = ReturnMethod,
                    SettlementMode = SelectedSettlementMode,
                    RefundChannel = SelectedRefundChannel,
                    ReturnDate = DateTime.Now
                };

                foreach (var item in ReturnItems.Where(x => x.ReturnQuantity > 0))
                {
                    purchaseReturn.PurchaseReturnItems.Add(new PurchaseReturnItem
                    {
                        OriginalPurchaseItemId = item.OriginalPurchaseItemId,
                        Quantity = item.ReturnQuantity,
                        UnitPrice = item.UnitPrice,
                        TotalAmount = item.TotalAmount,
                        ReasonCode = string.IsNullOrWhiteSpace(ReturnReason) ? "أخرى" : ReturnReason
                    });
                }

                if (!purchaseReturn.PurchaseReturnItems.Any()) return;

                var savedReturn = await _purchaseReturnService.ProcessReturnAsync(purchaseReturn);
                if (savedReturn != null)
                {
                    string successMsg = SelectedSettlementMode == PurchaseReturnSettlementMode.CashReceipt
                        ? $"تم تنفيذ عملية الإرجاع للمورد بنجاح، وتم استرداد المبلغ ({savedReturn.TotalAmount:N0} ريال) وإيداعه في ({SelectedRefundChannel})."
                        : "تم تنفيذ عملية الإرجاع للمورد بنجاح، وتم إصدار إشعار مدين وخصم المبلغ من رصيد المورد.";

                    MessageBox.Show(successMsg, "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    SearchInvoiceNumber = string.Empty;
                    CurrentPurchase = null;
                    ReturnItems.Clear();
                    ReturnReason = string.Empty;
                    ReturnNotes = string.Empty;
                    OnPropertyChanged(nameof(TotalReturnAmount));
                    await LoadPurchasesAsync();
                    ServiceLocator.NotifyInventoryChanged();
                    ServiceLocator.NotifySuppliersChanged();
                }
            }
            catch (Exception ex) { MessageBox.Show($"حدث خطأ أثناء تنفيذ الإرجاع: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
}
