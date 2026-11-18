using Microsoft.EntityFrameworkCore;
﻿using POSSystem.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using POSSystem.WPF.Models;
using POSSystem.WPF.Business;
using POSSystem.WPF.Business.Services;

namespace POSSystem.WPF.ViewModels.Returns
{
    public class ReturnConditionOption
    {
        public ReturnItemCondition Value { get; set; }
        public string Display { get; set; } = string.Empty;
    }

    public class ReturnOrderItemViewModel : BaseViewModel
    {
        private readonly OrderItem _originalItem;
        private int _returnQuantity;
        private string _reasonCode = "أخرى";
        private ReturnItemCondition _condition = ReturnItemCondition.Restock;

        public ReturnOrderItemViewModel(OrderItem originalItem, int previouslyReturnedQuantity = 0)
        {
            _originalItem = originalItem;
            _returnQuantity = 0;
            PreviouslyReturnedQuantity = previouslyReturnedQuantity;
        }

        public int PreviouslyReturnedQuantity { get; }
        public int OriginalOrderItemId => _originalItem.Id;
        public string ProductName => _originalItem.Name;
        public string Size => _originalItem.Size;
        public decimal UnitPrice => _originalItem.UnitPrice;
        public int MaxQuantity => Math.Max(0, _originalItem.Quantity - PreviouslyReturnedQuantity);

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

        public List<ReturnConditionOption> ConditionOptions { get; } = new()
        {
            new ReturnConditionOption { Value = ReturnItemCondition.Restock, Display = "🔄 إعادة للمخزون" },
            new ReturnConditionOption { Value = ReturnItemCondition.Waste, Display = "🗑️ هدر / تالف" }
        };

        public ReturnItemCondition Condition
        {
            get => _condition;
            set
            {
                if (_condition != value)
                {
                    _condition = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ConditionIndex));
                    OnPropertyChanged(nameof(IsWaste));
                    OnPropertyChanged(nameof(IsRestock));
                    OnPropertyChanged(nameof(ConditionDisplayText));
                }
            }
        }

        public int ConditionIndex
        {
            get => (int)Condition;
            set
            {
                if (value >= 0 && value <= 1)
                {
                    Condition = (ReturnItemCondition)value;
                }
            }
        }

        public bool IsWaste => Condition == ReturnItemCondition.Waste;
        public bool IsRestock => Condition == ReturnItemCondition.Restock;
        public string ConditionDisplayText => Condition == ReturnItemCondition.Waste ? "هدر / تالف" : "إعادة للمخزون";
        public string ConditionTag => Condition == ReturnItemCondition.Waste ? "[هدر]" : "[مخزون]";
    }

    public partial class ReturnOrderViewModel : BaseViewModel, POSSystem.WPF.Services.ISearchable
    {
        public ReturnOrderViewModel(User currentUser)
        {
            var sp = POSSystem.WPF.App.ServiceProvider;
            _unitOfWork = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Data.UnitOfWork.IUnitOfWork>(sp);
            _returnOrderService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.IReturnOrderService>(sp);
            _orderService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IOrderService>(sp);
            _shiftService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IShiftService>(sp);
            _currentUser = currentUser;

            SearchOrderCommand = new RelayCommand(async () => await SearchOrderAsync());
            ProcessReturnCommand = new RelayCommand(async () => await ProcessReturnAsync(), CanProcessReturn);
            AddAllToReturnCommand = new RelayCommand(AddAllToReturn, () => CurrentOrder != null && CanReturnFullOrder);
            SelectWalletQuickCommand = new RelayCommand<string>(wallet =>
            {
                if (!string.IsNullOrWhiteSpace(wallet))
                {
                    SelectedRefundWallet = wallet;
                }
            });

            LoadOrdersAsync().SafeFireAndForget();
        }

        private async Task LoadOrdersAsync()
        {
            try
            {
                // FIX ISSUE-PERF-001: Use bounded recent returnable orders query instead of loading all historical orders
                var orders = await _returnOrderService.GetOrdersAvailableForReturnAsync();
                var activeShift = await _shiftService.GetActiveShiftByUserIdAsync(_currentUser.Id);
                
                AllOrders.Clear();
                var filteredOrders = orders
                    .Where(x => x.Status != OrderStatus.Returned && x.Status != OrderStatus.Cancelled);

                if (!IsAdmin)
                {
                    if (activeShift != null)
                    {
                        filteredOrders = filteredOrders.Where(x => x.ShiftId == activeShift.Id);
                    }
                    else
                    {
                        filteredOrders = Enumerable.Empty<Order>();
                    }
                }

                var topOrders = filteredOrders
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(200);

                foreach (var o in topOrders) AllOrders.Add(o);
            }
            catch (Exception ex) { Console.WriteLine($"Error loading orders: {ex.Message}"); }
        }

        public void ApplySearch(string query)
        {
            SearchInvoiceNumber = query ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(SearchInvoiceNumber))
            {
                _ = SearchOrderAsync();
            }
        }

        public void RefreshOrders()
        {
            LoadOrdersAsync().SafeFireAndForget();
        }

        public void UpdateReturnType()
        {
            if (ReturnItems.Count == 0 || ReturnItems.All(x => x.ReturnQuantity == 0)) SelectedReturnType = "لا يوجد";
            else if (ReturnItems.All(x => x.ReturnQuantity == x.MaxQuantity)) SelectedReturnType = "كامل";
            else SelectedReturnType = "جزئي";
        }

        private async Task SearchOrderAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchInvoiceNumber))
            {
                MessageBox.Show("الرجاء إدخال رقم الفاتورة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // FIX ISSUE-PERF-001: Query SQLite directly by OrderNumber instead of loading the entire Orders table into memory
                var searchTrimmed = SearchInvoiceNumber.Trim();
                var candidateId = await _unitOfWork.GetContext().Orders
                    .AsNoTracking()
                    .Where(o => o.OrderNumber != null && EF.Functions.Like(o.OrderNumber, $"%{searchTrimmed}%"))
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => (int?)o.Id)
                    .FirstOrDefaultAsync();

                Order? order = candidateId.HasValue
                    ? await _unitOfWork.Orders.GetOrderWithItemsAsync(candidateId.Value)
                    : null;
                
                if (order == null)
                {
                    MessageBox.Show("الطلب غير موجود.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    CurrentOrder = null; ReturnItems.Clear(); return;
                }

                if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Returned)
                {
                    MessageBox.Show("لا يمكن إرجاع هذا الطلب لأنه ملغي أو تم إرجاعه بالكامل.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    CurrentOrder = null; ReturnItems.Clear(); return;
                }

                var activeShift = await _shiftService.GetActiveShiftByUserIdAsync(_currentUser.Id);
                
                if (!IsAdmin)
                {
                    if (activeShift == null)
                    {
                        MessageBox.Show("لا يوجد مناوبة فعالة حالياً لإرجاع الطلبات.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        CurrentOrder = null; ReturnItems.Clear(); return;
                    }
                    
                    if (order.ShiftId != activeShift.Id)
                    {
                        MessageBox.Show("عذراً، لا يمكنك إرجاع طلبات لا تنتمي إلى مناوبتك الحالية.", "صلاحيات محدودة", MessageBoxButton.OK, MessageBoxImage.Warning);
                        CurrentOrder = null; ReturnItems.Clear(); return;
                    }
                }

                CurrentOrder = order;
                ReturnItems.Clear();
                var cashier = await _unitOfWork.Users.GetByIdAsync(order.UserId);
                OrderCustomerName = await ResolveOrderCustomerNameAsync(order);
                OrderTime = order.CreatedAt.ToString("g");
                OrderCashierName = cashier?.FullName ?? order.UserId.ToString();
                CurrentOrderStatus = order.Status == OrderStatus.Completed ? "مكتمل" : order.Status == OrderStatus.Returned ? "تم الإرجاع" : order.Status == OrderStatus.Cancelled ? "ملغي" : "قيد المعالجة";

                // Auto-detect original payment method and advise cashier
                if (order.PaymentMethod == PaymentMethod.MobilePayment || order.DepositInfo != null)
                {
                    SetReturnMethodOption(PaymentMethod.MobilePayment);
                    if (!string.IsNullOrWhiteSpace(order.DepositBankOrWallet) && order.DepositBankOrWallet != "إيداع عام")
                    {
                        SelectedRefundWallet = order.DepositBankOrWallet;
                    }
                    OriginalPaymentNoticeText = $"💡 طريقة الدفع الأصلية للطلب: {order.PaymentMethodDisplay}";
                    HasOriginalPaymentNotice = true;
                }
                else if (order.PaymentMethod == PaymentMethod.OnCredit)
                {
                    SetReturnMethodOption(PaymentMethod.OnCredit);
                    OriginalPaymentNoticeText = "💡 هذا الطلب مسجل كـ دين آجل على العميل";
                    HasOriginalPaymentNotice = true;
                }
                else if (order.PaymentMethod == PaymentMethod.DebitCard || order.PaymentMethod == PaymentMethod.CreditCard)
                {
                    SetReturnMethodOption(PaymentMethod.DebitCard);
                    OriginalPaymentNoticeText = "💡 طريقة الدفع الأصلية للطلب: شبكة / بطاقة";
                    HasOriginalPaymentNotice = true;
                }
                else
                {
                    SetReturnMethodOption(PaymentMethod.Cash);
                    OriginalPaymentNoticeText = "💡 طريقة الدفع الأصلية للطلب: نقدي (كاش)";
                    HasOriginalPaymentNotice = true;
                }

                var previousReturns = await _returnOrderService.GetReturnsByOriginalOrderIdAsync(order.Id);
                var returnedQuantities = previousReturns.SelectMany(r => r.ReturnOrderItems).GroupBy(ri => ri.OriginalOrderItemId).ToDictionary(g => g.Key, g => g.Sum(ri => ri.Quantity));

                foreach (var item in order.OrderItems)
                {
                    int alreadyReturned = returnedQuantities.ContainsKey(item.Id) ? returnedQuantities[item.Id] : 0;
                    var vm = new ReturnOrderItemViewModel(item, alreadyReturned);
                    vm.PropertyChanged += (s, e) => { 
                        if (e.PropertyName == nameof(ReturnOrderItemViewModel.TotalAmount) || e.PropertyName == nameof(ReturnOrderItemViewModel.ReturnQuantity)) 
                        {
                            OnPropertyChanged(nameof(TotalReturnAmount)); UpdateReturnType();
                        }
                    };
                    ReturnItems.Add(vm);
                }
                OnPropertyChanged(nameof(TotalReturnAmount)); UpdateReturnType();
            }
            catch (Exception ex) { MessageBox.Show($"خطأ أثناء البحث عن الطلب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void AddAllToReturn()
        {
            if (CurrentOrder == null) return;
            foreach (var item in ReturnItems)
            {
                item.ReturnQuantity = item.MaxQuantity;
            }
            OnPropertyChanged(nameof(TotalReturnAmount)); UpdateReturnType();
            ((RelayCommand)ProcessReturnCommand).RaiseCanExecuteChanged();
        }

        private bool CanProcessReturn() => CurrentOrder != null && ReturnItems.Any(x => x.ReturnQuantity > 0) && (!string.IsNullOrWhiteSpace(ReturnReason) || !string.IsNullOrWhiteSpace(ReturnNotes));

        public void InitializeWithInvoice(string invoiceNumber)
        {
            SearchInvoiceNumber = invoiceNumber;
            SearchOrderAsync().SafeFireAndForget();
        }

        private async Task<string> ResolveOrderCustomerNameAsync(Order order)
        {
            if (order.OrderType == "delivery" && !string.IsNullOrWhiteSpace(order.Delivery?.DeliveryCustomerName))
                return order.Delivery.DeliveryCustomerName;

            if (order.PaymentMethod == PaymentMethod.OnCredit)
            {
                var debt = await _unitOfWork.CustomerDebts.FirstOrDefaultAsync(d => d.OrderId == order.Id);
                if (debt != null)
                {
                    if (debt.Customer == null && debt.CustomerId > 0)
                        debt.Customer = await _unitOfWork.Customers.GetByIdAsync(debt.CustomerId);

                    var customerName = debt.Customer?.Name ?? debt.CustomerName;
                    return string.IsNullOrWhiteSpace(customerName) ? "عميل آجل" : customerName;
                }

                return "عميل آجل";
            }

            return "عميل مباشر";
        }

        private async Task ProcessReturnAsync()
        {
            if (!CanProcessReturn()) return;
            try
            {
                var passwordPopup = new POSSystem.WPF.Views.Popups.ShiftPasswordPopup();
                if (passwordPopup.ShowDialog() != true) return;

                int currentUserId = _currentUser.Id; 
                
                var reasonParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(ReturnReason)) reasonParts.Add(ReturnReason.Trim());
                if (!string.IsNullOrWhiteSpace(ReturnNotes)) reasonParts.Add(ReturnNotes.Trim());

                if (ReturnMethod == PaymentMethod.MobilePayment)
                {
                    string walletInfo = $"[استرداد محفظة: {SelectedRefundWallet}";
                    if (!string.IsNullOrWhiteSpace(RefundReferenceNumber))
                        walletInfo += $" | مرجع: {RefundReferenceNumber.Trim()}";
                    walletInfo += "]";
                    reasonParts.Add(walletInfo);
                }

                var finalReason = string.Join(" - ", reasonParts);

                var returnOrder = new ReturnOrder
                {
                    OriginalOrderId = CurrentOrder!.Id,
                    CashierId = currentUserId,
                    Reason = finalReason,
                    ReturnMethod = ReturnMethod,
                    ReturnDate = DateTime.Now
                };

                foreach (var item in ReturnItems.Where(x => x.ReturnQuantity > 0))
                {
                    string itemReason = string.IsNullOrWhiteSpace(ReturnReason)
                        ? item.ConditionTag
                        : $"{item.ConditionTag} {ReturnReason}".Trim();

                    var model = new ReturnOrderItem
                    {
                        OriginalOrderItemId = item.OriginalOrderItemId,
                        Quantity = item.ReturnQuantity,
                        UnitPrice = item.UnitPrice,
                        ReasonCode = itemReason,
                        Condition = item.Condition
                    };
                    model.InitializeTotalAmount();
                    returnOrder.ReturnOrderItems.Add(model);
                }

                if (!returnOrder.ReturnOrderItems.Any()) return;

                if (await _returnOrderService.ProcessReturnAsync(returnOrder))
                {
                    POSSystem.WPF.StaticFunction.GloublStatic.PrintReturnReceipt(returnOrder, CurrentOrder);
                    MessageBox.Show("تم تنفيذ عملية الإرجاع وإصدار إشعار دائن (Credit Note) بنجاح.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    SearchInvoiceNumber = string.Empty; 
                    CurrentOrder = null; 
                    ReturnItems.Clear(); 
                    ReturnReason = string.Empty; 
                    ReturnNotes = string.Empty;
                    RefundReferenceNumber = string.Empty;
                    HasOriginalPaymentNotice = false;
                    OnPropertyChanged(nameof(TotalReturnAmount)); UpdateReturnType();
                    await LoadOrdersAsync();
                }
            }
            catch (Exception ex) { MessageBox.Show($"حدث خطأ أثناء تنفيذ الإرجاع: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
}
