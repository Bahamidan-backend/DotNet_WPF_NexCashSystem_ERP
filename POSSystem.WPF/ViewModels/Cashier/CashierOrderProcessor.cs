using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using POSSystem.WPF.Models;
using POSSystem.WPF.StaticFunction;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.Services.Logging;

namespace POSSystem.WPF.ViewModels.Cashier
{
    public partial class CashierViewModel
    {
        public async Task PlaceOrder() => await PlaceOrderWithPayment(Models.PaymentMethod.Cash);
        public async Task PlaceOrderSplit() => await PlaceOrderWithPayment(Models.PaymentMethod.Split);

        public async Task PlaceOrderWithPayment(Models.PaymentMethod paymentMethod)
        {
            if (_currentShift == null) { await CheckActiveShiftAsync(); }
            if (_currentShift == null) { MessageBox.Show("يجب بدء مناوبة قبل إتمام الطلب.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (cashierProberty.OrderItems.Count == 0) { MessageBox.Show("الطلب فارغ.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            try
            {
                if (cashierProberty.OrderType == "delivery")
                {
                    if (string.IsNullOrWhiteSpace(cashierProberty.DeliveryCustomerName)) { MessageBox.Show("يرجى إدخال اسم العميل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                    if (string.IsNullOrWhiteSpace(cashierProberty.DeliveryPhone)) { MessageBox.Show("يرجى إدخال رقم الهاتف", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                    if (string.IsNullOrWhiteSpace(cashierProberty.DeliveryAddress)) { MessageBox.Show("يرجى إدخال العنوان", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                }

                decimal currentTaxPercent = settingsProberty?.TaxRate ?? SettingsService.LoadSettings().TaxRate;
                decimal effectiveTaxRate = Math.Max(0m, currentTaxPercent) / 100m;
                decimal calculatedTaxAmount = Math.Round(cashierProberty.SubTotal * effectiveTaxRate, 2);

                var order = new Order 
                { 
                    OrderNumber = cashierProberty.NextOrderNumber, 
                    OrderType = cashierProberty.OrderType, 
                    UserId = _currentUser.Id, 
                    ShiftId = _currentShift?.Id, 
                    SubTotal = cashierProberty.SubTotal, 
                    Total = cashierProberty.TotalAmount, 
                    TaxRate = effectiveTaxRate,
                    TaxAmount = calculatedTaxAmount,
                    PaymentMethod = paymentMethod, 
                    Status = cashierProberty.OrderType == "delivery" ? OrderStatus.Preparing : OrderStatus.Completed, 
                    CreatedAt = DateTime.Now, 
                    UpdatedAt = DateTime.Now, 
                    OrderItems = cashierProberty.OrderItems.ToList() 
                };

                if (cashierProberty.OrderType == "delivery")
                {
                    if (string.IsNullOrWhiteSpace(cashierProberty.SelectedDeliveryDriver) && cashierProberty.DeliveryDrivers.Count > 0)
                    {
                        cashierProberty.SelectedDeliveryDriver = cashierProberty.DeliveryDrivers[0];
                    }
                    order.Delivery = new Delivery { DeliveryCustomerName = cashierProberty.DeliveryCustomerName, DeliveryPhone = cashierProberty.DeliveryPhone, DeliveryAddress = cashierProberty.DeliveryAddress, DeliveryDriverName = cashierProberty.SelectedDeliveryDriver, DeliveryFee = cashierProberty.DeliveryFee };

                    await SaveOrUpdateDeliveryCustomerAsync(cashierProberty.DeliveryCustomerName, cashierProberty.DeliveryPhone, cashierProberty.DeliveryAddress);
                }

                if (cashierProberty.HasDiscount && cashierProberty.DiscountAmount > 0) { order.Discount = cashierProberty.DiscountAmount; if (!string.IsNullOrWhiteSpace(cashierProberty.DiscountCode)) order.Notes = (order.Notes ?? string.Empty) + $"Coupon:{cashierProberty.DiscountCode}"; }

                CustomerDebt? customerDebt = null;
                if (order.PaymentMethod == PaymentMethod.OnCredit)
                {
                    var popup = new Views.Popups.CustomerInfoPopup(order.Total);
                    if (popup.ShowDialog() != true) return;
                    customerDebt = new CustomerDebt 
                    { 
                        CustomerId = popup.SelectedCustomerId,
                        CustomerName = popup.CustomerName, 
                        PhoneNumber = popup.PhoneNumber, 
                        CreditLimit = popup.CreditLimit,
                        Amount = order.Total, 
                        IsPaid = false, 
                        UserId = _currentUser.Id,
                        ShiftId = _currentShift?.Id,
                        Notes = string.IsNullOrWhiteSpace(popup.Notes) 
                            ? order.Notes 
                            : string.IsNullOrWhiteSpace(order.Notes) 
                                ? popup.Notes 
                                : $"{order.Notes} | {popup.Notes}", 
                        DebtDate = order.CreatedAt, 
                        UpdatedAt = order.UpdatedAt.Value 
                    };
                }
                else if (order.PaymentMethod == PaymentMethod.MobilePayment)
                {
                    var depositPopup = new Views.Popups.DepositPaymentPopup(order.Total);
                    if (depositPopup.ShowDialog() != true) return;

                    string depositTag = Helpers.OrderDepositHelper.FormatDepositTag(
                        depositPopup.SelectedCurrency,
                        depositPopup.DepositAmount,
                        depositPopup.BankOrWallet,
                        depositPopup.ReferenceNumber);

                    order.Notes = string.IsNullOrWhiteSpace(order.Notes)
                        ? depositTag
                        : $"{order.Notes} {depositTag}";
                }
                else if (order.PaymentMethod == PaymentMethod.Split)
                {
                    var splitPopup = new Views.Popups.SplitPaymentPopup(order.Total);
                    if (splitPopup.ShowDialog() != true) return;

                    string splitTag = Helpers.OrderSplitHelper.FormatSplitTag(
                        splitPopup.CashAmount,
                        splitPopup.CardAmount,
                        splitPopup.BankOrWallet,
                        splitPopup.DebtAmount,
                        splitPopup.CustomerName);

                    order.Notes = string.IsNullOrWhiteSpace(order.Notes)
                        ? splitTag
                        : $"{order.Notes} {splitTag}";

                    if (splitPopup.DebtAmount > 0 && !string.IsNullOrWhiteSpace(splitPopup.CustomerName))
                    {
                        customerDebt = new CustomerDebt
                        {
                            CustomerId = splitPopup.SelectedCustomerId ?? 0,
                            CustomerName = splitPopup.CustomerName,
                            PhoneNumber = splitPopup.CustomerPhone,
                            Amount = splitPopup.DebtAmount,
                            IsPaid = false,
                            UserId = _currentUser.Id,
                            ShiftId = _currentShift?.Id,
                            Notes = $"متبقي آجل من فاتورة دفع مجزأ #{order.OrderNumber} | {order.Notes}",
                            DebtDate = order.CreatedAt,
                            UpdatedAt = order.UpdatedAt ?? DateTime.Now
                        };
                    }
                }

                // Snapshot items with their Product & Category references BEFORE saving & clearing cart
                var printItemsSnapshot = cashierProberty.OrderItems.Select(i => new OrderItem
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    Product = i.Product,
                    AddonId = i.AddonId,
                    Addon = i.Addon,
                    Size = i.Size,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice,
                    Notes = i.Notes
                }).ToList();

                order.OrderNumber = await _orderService.GenerateUniqueOrderNumberAsync(_currentUser.Id);
                var savedOrder = await _orderService.PlaceOrderTransactionAsync(order, paymentMethod, _currentUser, _currentShift, customerDebt);
                if (savedOrder != null && !string.IsNullOrWhiteSpace(savedOrder.OrderNumber))
                {
                    order.OrderNumber = savedOrder.OrderNumber;
                    order.Id = savedOrder.Id;
                }

                var printOrderSnapshot = new Order
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    OrderType = order.OrderType,
                    UserId = _currentUser.Id,
                    User = _currentUser,
                    ShiftId = _currentShift?.Id,
                    SubTotal = order.SubTotal,
                    Discount = order.Discount,
                    ServiceCharge = order.ServiceCharge,
                    TaxRate = order.TaxRate,
                    TaxAmount = order.TaxAmount,
                    Total = order.Total,
                    PaymentMethod = order.PaymentMethod,
                    Status = order.Status,
                    Notes = order.Notes,
                    Delivery = order.Delivery,
                    CreatedAt = order.CreatedAt,
                    OrderItems = printItemsSnapshot
                };

                bool printCustFlag = cashierProberty.PrintCustomerThisOrder;
                bool printKitFlag = cashierProberty.PrintKitchenThisOrder;
                // إذا لم يكن أي من الزرين مفعلاً، نعتمد إعدادات الطباعة الافتراضية (زبون + مطبخ)
                if (!printCustFlag && !printKitFlag)
                {
                    var currentSettings = SettingsService.LoadSettings();
                    printCustFlag = currentSettings?.PrintCustomerReceipt ?? true;
                    printKitFlag = currentSettings?.PrintKitchenReceipt ?? true;
                }

                var itemsCopy = cashierProberty.OrderItems.ToList();
                foreach (var item in itemsCopy) DeductProductFromInventory(item);
                foreach (var item in itemsCopy)
                {
                    if (item.Product != null)
                    {
                        item.Product.IsInOrder = false;
                        item.Product.Quantity = 0;
                    }
                }
                
                cashierProberty.OrderItems.Clear();
                FilterProducts();

                cashierProberty.SelectedDiscount = null; cashierProberty.DiscountCode = string.Empty; cashierProberty.DiscountAmount = 0; cashierProberty.HasDiscount = false;
                cashierProberty.DeliveryCustomerName = string.Empty; cashierProberty.DeliveryPhone = string.Empty; cashierProberty.DeliveryAddress = string.Empty; cashierProberty.SelectedDeliveryCustomer = null; cashierProberty.SelectedDeliveryDriver = cashierProberty.DeliveryDrivers.FirstOrDefault() ?? string.Empty; cashierProberty.DeliveryFee = 0;

                // دائماً نشغل الطباعة على خيط الواجهة الرئيسي (STA Dispatcher) تماماً مثل سجل الطلبات لضمان طباعة فاتورة الزبون وقصات المطبخ معاً بدون أي خطأ
                _ = Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        GloublStatic.PrintReceipt(
                            printOrderSnapshot,
                            debt: customerDebt,
                            overridePrintCustomer: printCustFlag,
                            overridePrintKitchen: printKitFlag);
                    }
                    catch (Exception ex)
                    {
                        AppLogger.LogError($"Order printing error: {ex.Message}", ex);
                    }
                }));

                OnPropertyChanged(nameof(cashierProberty.SubTotal)); OnPropertyChanged(nameof(cashierProberty.CountProduct)); OnPropertyChanged(nameof(cashierProberty.TotalAmount));
                _ordersViewModel.RefreshOrders();
            }
            catch (Exception ex) 
            { 
                AppLogger.LogError("Error saving cashier order", ex);
                var fullMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    fullMessage += "\nInner: " + ex.InnerException.Message;
                }
                MessageBox.Show($"خطأ في حفظ الطلب: {fullMessage}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); 
            }
            finally { await SequnceNumber(); }
        }

        private async Task SequnceNumber()
        {
            try
            {
                var GenercNumber = await _orderService.GenerateUniqueOrderNumberAsync(_currentUser.Id);
                cashierProberty.NextOrderNumber = GenercNumber;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating sequence number: {ex.Message}");
            }
        }

        public void ApplyDiscount()
        {
            if (cashierProberty.OrderItems.Count == 0)
            {
                ClearDiscount();
                MessageBox.Show("يرجى إضافة أصناف للفاتورة أولاً قبل تطبيق الخصم.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (cashierProberty.SelectedDiscount != null && !string.IsNullOrWhiteSpace(cashierProberty.SelectedDiscount.Code))
            {
                cashierProberty.DiscountCode = cashierProberty.SelectedDiscount.Code;
            }
            if (string.IsNullOrWhiteSpace(cashierProberty.DiscountCode))
            {
                ClearDiscount();
                MessageBox.Show("يرجى اختيار الخصم من القائمة المنسدلة أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var codeTrimmed = cashierProberty.DiscountCode.Trim();
            var discounts = _activeDiscounts ?? Enumerable.Empty<Discount>();
            var matched = cashierProberty.SelectedDiscount != null && cashierProberty.SelectedDiscount.IsActive
                ? cashierProberty.SelectedDiscount
                : discounts.FirstOrDefault(item => item != null && item.IsActive &&
                    (string.Equals(item.Code?.Trim(), codeTrimmed, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(item.Name?.Trim(), codeTrimmed, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(item.DisplayLabel, codeTrimmed, StringComparison.OrdinalIgnoreCase)));

            if (matched != null)
            {
                decimal pct = matched.Percentage > 0 ? matched.Percentage : matched.DiscountPercentage;
                decimal amt = matched.Amount > 0 ? matched.Amount : matched.DiscountAmount;
                if (pct > 0)
                {
                    cashierProberty.DiscountAmount = Math.Round(cashierProberty.SubTotal * (pct / 100m), 2);
                }
                else if (amt > 0)
                {
                    cashierProberty.DiscountAmount = Math.Min(cashierProberty.SubTotal, amt);
                }
                else
                {
                    cashierProberty.DiscountAmount = 0m;
                }
                cashierProberty.DiscountCode = matched.Code;
                cashierProberty.SelectedDiscount = matched;
                cashierProberty.HasDiscount = cashierProberty.DiscountAmount > 0;
                OnPropertyChanged(nameof(cashierProberty.SubTotal));
                OnPropertyChanged(nameof(cashierProberty.TotalAmount));
                return;
            }
            cashierProberty.DiscountAmount = 0;
            cashierProberty.HasDiscount = false;
        }

        public void HoldOrder()
        {
            if (cashierProberty.OrderItems.Count == 0) { MessageBox.Show("لا يوجد طلب لتعليقه.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            try
            {
                var heldOrder = new HeldOrder 
                { 
                    CustomerName = !string.IsNullOrEmpty(cashierProberty.DeliveryCustomerName) ? cashierProberty.DeliveryCustomerName : $"زبون {DateTime.Now:HH:mm}", 
                    CustomerPhone = cashierProberty.DeliveryPhone, 
                    OrderType = cashierProberty.OrderType, 
                    DiscountAmount = cashierProberty.DiscountAmount, 
                    DiscountCode = cashierProberty.DiscountCode, 
                    HasDiscount = cashierProberty.HasDiscount, 
                    TotalAmount = cashierProberty.TotalAmount, 
                    DeliveryAddress = cashierProberty.DeliveryAddress, 
                    DeliveryDriver = cashierProberty.SelectedDeliveryDriver, 
                    DeliveryFee = cashierProberty.DeliveryFee,
                    ShiftId = _currentShift?.Id,
                    UserId = _currentUser?.Id,
                    HoldTime = DateTime.Now
                };
                foreach (var item in cashierProberty.OrderItems)
                {
                    var newItem = new OrderItem { ProductId = item.ProductId, Product = item.Product, AddonId = item.AddonId, Addon = item.Addon, UnitPrice = item.UnitPrice, Quantity = item.Quantity, Size = item.Size, Notes = item.Notes, TotalPrice = item.TotalPrice, CreatedAt = item.CreatedAt };
                    if (item.addonAndOrderItems != null && item.addonAndOrderItems.Any()) { foreach (var addonItem in item.addonAndOrderItems) newItem.addonAndOrderItems.Add(new AddonAndOrderItem { AddonId = addonItem.AddonId, Addon = addonItem.Addon, OrderitemID = addonItem.OrderitemID }); }
                    heldOrder.Items.Add(newItem);
                }

                // Serialize snapshot for SQLite storage
                heldOrder.SerializeItems();

                // 1. Instant UI update (0ms lag)
                HeldOrders.Add(heldOrder);

                // 2. Persistent SQLite write in background
                _heldOrderService.SaveHeldOrderAsync(heldOrder).SafeFireAndForget();

                foreach (var item in cashierProberty.OrderItems)
                {
                    if (item.Product != null)
                    {
                        item.Product.IsInOrder = false;
                        item.Product.Quantity = 0;
                    }
                }
                cashierProberty.OrderItems.Clear(); FilterProducts();
                cashierProberty.SelectedDiscount = null; cashierProberty.DiscountCode = string.Empty; cashierProberty.DiscountAmount = 0; cashierProberty.HasDiscount = false;
                cashierProberty.DeliveryCustomerName = string.Empty; cashierProberty.DeliveryPhone = string.Empty; cashierProberty.DeliveryAddress = string.Empty; cashierProberty.SelectedDeliveryCustomer = null; cashierProberty.SelectedDeliveryDriver = cashierProberty.DeliveryDrivers.FirstOrDefault() ?? string.Empty; cashierProberty.DeliveryFee = 0;
                OnPropertyChanged(nameof(cashierProberty.SubTotal)); OnPropertyChanged(nameof(cashierProberty.CountProduct)); OnPropertyChanged(nameof(cashierProberty.TotalAmount));
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في تعليق الطلب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public void ResumeOrder(HeldOrder heldOrder)
        {
            if (heldOrder == null) return;
            if (cashierProberty.OrderItems.Count > 0) { if (MessageBox.Show("يوجد طلب حالي. هل تريد استبداله بالطلب المعلق؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return; foreach (var item in cashierProberty.OrderItems) { if (item.Product != null) item.Product.IsInOrder = false; } }
            try
            {
                cashierProberty.OrderItems.Clear();
                foreach (var item in heldOrder.Items)
                {
                    if (item.Product != null) item.Product.IsInOrder = true;
                    item.InitializeTotalPrice();
                    cashierProberty.OrderItems.Add(item);
                }
                cashierProberty.OrderType = heldOrder.OrderType; cashierProberty.DiscountCode = heldOrder.DiscountCode; cashierProberty.DiscountAmount = heldOrder.DiscountAmount; cashierProberty.HasDiscount = heldOrder.HasDiscount; cashierProberty.DeliveryCustomerName = heldOrder.CustomerName; cashierProberty.DeliveryPhone = heldOrder.CustomerPhone; cashierProberty.DeliveryAddress = heldOrder.DeliveryAddress; cashierProberty.SelectedDeliveryDriver = heldOrder.DeliveryDriver; cashierProberty.DeliveryFee = heldOrder.DeliveryFee;

                // Remove from in-memory collection and persistent DB
                HeldOrders.Remove(heldOrder);
                _heldOrderService.DeleteHeldOrderAsync(heldOrder.Id).SafeFireAndForget();

                OnPropertyChanged(nameof(cashierProberty.SubTotal)); OnPropertyChanged(nameof(cashierProberty.CountProduct)); OnPropertyChanged(nameof(cashierProberty.TotalAmount));
            }
            catch (Exception ex) { MessageBox.Show($"خطأ في استرجاع الطلب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public void DeleteHeldOrder(HeldOrder heldOrder)
        {
            if (heldOrder == null) return;
            try
            {
                HeldOrders.Remove(heldOrder);
                _heldOrderService.DeleteHeldOrderAsync(heldOrder.Id).SafeFireAndForget();
            }
            catch (Exception ex)
            {
                POSSystem.WPF.Services.Logging.AppLogger.LogError($"Error removing held order #{heldOrder.Id}", ex);
            }
        }

        public void ShowHeldOrders()
        {
            var popup = new Views.Popups.HeldOrdersPopup(this);
            var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive && x.IsLoaded && x.IsVisible && x != popup);
            if (activeWindow != null) popup.Owner = activeWindow;
            if (popup.ShowDialog() == true && popup.SelectedOrder != null) ResumeOrder(popup.SelectedOrder);
        }

        private async Task SaveOrUpdateDeliveryCustomerAsync(string name, string phone, string address)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            try
            {
                var trimmedName = name.Trim();
                var trimmedPhone = phone?.Trim() ?? string.Empty;
                var trimmedAddr = address?.Trim() ?? string.Empty;

                var existing = (await _unitOfWork.Customers.FindAsync(c => 
                    (!string.IsNullOrWhiteSpace(trimmedPhone) && c.PhoneNumber == trimmedPhone) ||
                    c.Name == trimmedName)).FirstOrDefault();

                if (existing != null)
                {
                    bool changed = false;
                    if (!string.IsNullOrWhiteSpace(trimmedAddr) && existing.Address != trimmedAddr)
                    {
                        existing.Address = trimmedAddr;
                        changed = true;
                    }
                    if (!string.IsNullOrWhiteSpace(trimmedPhone) && existing.PhoneNumber != trimmedPhone)
                    {
                        existing.PhoneNumber = trimmedPhone;
                        changed = true;
                    }
                    if (changed)
                    {
                        existing.UpdatedAt = DateTime.Now;
                        _unitOfWork.Customers.Update(existing);
                        await _unitOfWork.SaveChangesAsync();
                    }
                }
                else
                {
                    var newCustomer = new Customer
                    {
                        Name = trimmedName,
                        PhoneNumber = trimmedPhone,
                        Address = trimmedAddr,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    await _unitOfWork.Customers.AddAsync(newCustomer);
                    await _unitOfWork.SaveChangesAsync();

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (!cashierProberty.DeliveryCustomers.Any(c => c.Id == newCustomer.Id))
                        {
                            cashierProberty.DeliveryCustomers.Add(newCustomer);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving delivery customer: {ex.Message}");
            }
        }
    }
}
