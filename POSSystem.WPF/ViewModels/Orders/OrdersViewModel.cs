using POSSystem.WPF.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using POSSystem.WPF.Business.Helpers;
using POSSystem.WPF.Models;
using POSSystem.WPF.Business;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Services;
using POSSystem.WPF.Shered.Commands;
using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.Data;
using POSSystem.WPF.Data.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

namespace POSSystem.WPF.ViewModels.Orders
{
    public partial class OrdersViewModel : BaseViewModel
    {
        public OrdersViewModel(User user)
        {
            _currentUser = user;
            
            var sp = POSSystem.WPF.App.ServiceProvider;
            _unitOfWork = sp.GetRequiredService<IUnitOfWork>();
            _orderService = sp.GetRequiredService<IOrderService>();
            
            Commands = new OrdersCommands(this);
            ordersProberty = new OrdersProberty();

            LoadOrdersAsync().SafeFireAndForget();

            _shiftService = sp.GetRequiredService<IShiftService>();
            _shiftService.ShiftStarted += OnShiftChanged;
            _shiftService.ShiftEnded += OnShiftChanged;
        }

        private void OnShiftChanged(object? sender, EventArgs e)
        {
            LoadOrdersAsync().SafeFireAndForget();
        }

        public override void Dispose()
        {
            if (_isDisposed) return;
            
            if (_shiftService != null)
            {
                _shiftService.ShiftStarted -= OnShiftChanged;
                _shiftService.ShiftEnded -= OnShiftChanged;
            }

            base.Dispose();
        }

        public void LoadOrders() => LoadOrdersAsync().SafeFireAndForget();

        public async Task LoadOrdersAsync()
        {
            await _dbSemaphore.WaitAsync();
            try
            {
                ordersProberty.Orders.Clear();

                var shiftService = POSSystem.WPF.App.ServiceProvider.GetRequiredService<IShiftService>();
                var activeShift = await shiftService.GetActiveShiftByUserIdAsync(_currentUser.Id);
                
                if (activeShift == null)
                {
                    ordersProberty.ActiveShiftStart = null;
                    UpdateStatistics();
                    return; 
                }

                ordersProberty.ActiveShiftStart = activeShift.StartTime;

                var orders = await _orderService.GetOrdersByDateRangeAsync(activeShift.StartTime.Date, DateTime.Today.AddDays(1));
                
                var shiftOrders = orders
                    .Where(o => o.ShiftId == activeShift.Id || (o.UserId == _currentUser.Id && o.CreatedAt >= activeShift.StartTime))
                    .OrderByDescending(o => o.CreatedAt)
                    .ToList();
                
                if (shiftOrders != null && shiftOrders.Any())
                {
                    foreach (var order in shiftOrders)
                    {
                        ordersProberty.Orders.Add(order);
                    }
                }

                ordersProberty.FilterOrders();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading orders: {ex.Message}");
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        public void UpdateStatistics()
        {
            var today = DateTime.Today;
            var todayOrders = ordersProberty.Orders
                .Where(o => o.CreatedAt.Date == today || (ordersProberty.ActiveShiftStart != null && o.CreatedAt >= ordersProberty.ActiveShiftStart))
                .ToList();
            
            ordersProberty.TotalOrdersToday = todayOrders.Count;
            ordersProberty.CompletedOrdersToday = todayOrders.Count(o => o.Status == OrderStatus.Completed);
            ordersProberty.PreparingOrdersToday = todayOrders.Count(o => o.Status == OrderStatus.Preparing || o.Status == OrderStatus.Ready);
            
            ordersProberty.TotalSalesToday = SalesMetricsHelper.SumGrossSales(todayOrders);
        }
        
        public void RefreshOrders()
        {
            LoadOrders();
        }

        public void ViewOrderDetails(Order order)
        {
            if (order != null)
            {
                ordersProberty.OrderType = order.OrderTypeText;
                ordersProberty.CountProduct = order.OrderItems.Count();
                OrderDetailsRequested?.Invoke(this, order);
            }
        }

        public void ReprintOrder(Order order)
        {
            if (order != null)
            {
                _ = Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    POSSystem.WPF.StaticFunction.GloublStatic.PrintReceipt(order);
                }));
                MessageBox.Show($" تم إعادة طباعة فاتورة {order.OrderNumber}", 
                    "طباعة", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public async void CancelOrder(Order order)
        {
            if (order == null) return;

            if (order.Status == OrderStatus.Cancelled)
            {
                MessageBox.Show("هذه الفاتورة ملغاة مسبقاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (order.Status == OrderStatus.Returned || order.Status == OrderStatus.PartiallyReturned)
            {
                MessageBox.Show("لا يمكن إلغاء فاتورة تم عمل مرتجع لها. يرجى استخدام شاشة المرتجعات.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!PermissionGuard.HasPermission(_currentUser, SystemPermissions.SalesVoidOrder))
            {
                MessageBox.Show("ليس لديك صلاحية إلغاء الفواتير (Void).", "صلاحية غير متوفرة", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialogResult = ShowVoidOrReopenDialog(order);
            if (dialogResult.Action == VoidDialogAction.None)
                return;

            try
            {
                // Ensure full order items are loaded before voiding/reopening
                var fullOrder = await _orderService.GetOrderWithItemsAsync(order.Id) ?? order;

                string fullReason = dialogResult.Action == VoidDialogAction.VoidAndReopen
                    ? $"[إلغاء وإعادة فتح للتعديل] {dialogResult.Reason}"
                    : dialogResult.Reason;

                await _orderService.CancelOrderAsync(
                    order.Id,
                    fullReason,
                    _currentUser?.Id,
                    restoreInventory: dialogResult.RestoreInventory);

                order.Status = OrderStatus.Cancelled;
                ServiceLocator.NotifyInventoryChanged();
                ServiceLocator.NotifyCashBoxChanged();
                
                ordersProberty.FilterOrders();
                UpdateStatistics();

                if (dialogResult.Action == VoidDialogAction.VoidAndReopen)
                {
                    ReopenOrderInCashierRequested?.Invoke(this, fullOrder);
                    MessageBox.Show(
                        $"تم إلغاء الفاتورة #{order.OrderNumber} وتحميل أصنافها في سلة الكاشير لتعديلها وإصدار فاتورة جديدة.",
                        "إلغاء وإعادة فتح للتعديل",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    string stockMsg = dialogResult.RestoreInventory
                        ? "واسترجاع الكميات إلى المخزون الجاهز"
                        : "دون إرجاع المخزون (لأن الطلب تم استهلاكه/تحضيره)";
                    MessageBox.Show(
                        $"تم إلغاء الفاتورة #{order.OrderNumber} {stockMsg} وعكس أثرها المالي بنجاح.",
                        "نجاح الإلغاء",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء إلغاء الطلب: {ex.Message}", 
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private enum VoidDialogAction
        {
            None,
            VoidOnly,
            VoidAndReopen
        }

        private (VoidDialogAction Action, string Reason, bool RestoreInventory) ShowVoidOrReopenDialog(Order order)
        {
            var win = new Window
            {
                Title = $"إلغاء أو تعديل الفاتورة #{order.OrderNumber}",
                Width = 480,
                Height = 390,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                FlowDirection = FlowDirection.RightToLeft,
                ResizeMode = ResizeMode.NoResize,
                Background = Brushes.White
            };

            var root = new StackPanel { Margin = new Thickness(22) };
            root.Children.Add(new TextBlock
            {
                Text = $"❌ إلغاء الفاتورة السريع أو إعادة فتحها للتعديل (#{order.OrderNumber})",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                Margin = new Thickness(0, 0, 0, 6)
            });

            root.Children.Add(new TextBlock
            {
                Text = $"الإجمالي: {order.Total:N0} ريال | الدفع: {order.PaymentMethodDisplay} | الحالة: {order.StatusText}",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                Margin = new Thickness(0, 0, 0, 14)
            });

            root.Children.Add(new TextBlock
            {
                Text = "سبب الإلغاء / التعديل *",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 6)
            });

            var reasonCombo = new ComboBox
            {
                Height = 38,
                FontSize = 13,
                IsEditable = true,
                Margin = new Thickness(0, 0, 0, 12)
            };
            reasonCombo.Items.Add("خطأ في إدخال الأصناف أو الكميات");
            reasonCombo.Items.Add("تراجع العميل قبل الاستلام");
            reasonCombo.Items.Add("خطأ في اختيار طريقة الدفع");
            reasonCombo.Items.Add("طلب مكرر بالخطأ");
            reasonCombo.SelectedIndex = 0;
            root.Children.Add(reasonCombo);

            var restoreStockCheck = new CheckBox
            {
                Content = "إرجاع كميات الأصناف إلى المخزون الجاهز / المستودع (إذا لم يتم استهلاك الطلب)",
                IsChecked = true,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                Margin = new Thickness(0, 4, 0, 18)
            };
            root.Children.Add(restoreStockCheck);

            VoidDialogAction chosenAction = VoidDialogAction.None;

            var reopenBtn = new Button
            {
                Content = "🔄 إلغاء الفاتورة وإعادة فتحها في الكاشير للتعديل",
                Height = 42,
                Background = new SolidColorBrush(Color.FromRgb(2, 132, 199)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var voidOnlyBtn = new Button
            {
                Content = "❌ إلغاء الفاتورة نهائياً فقط (Void)",
                Height = 40,
                Background = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var cancelBtn = new Button
            {
                Content = "تراجع",
                Height = 34,
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                BorderThickness = new Thickness(0)
            };

            reopenBtn.Click += (s, e) =>
            {
                chosenAction = VoidDialogAction.VoidAndReopen;
                restoreStockCheck.IsChecked = true; // Reopening in cart requires restoring the original reservation first
                win.DialogResult = true;
                win.Close();
            };

            voidOnlyBtn.Click += (s, e) =>
            {
                chosenAction = VoidDialogAction.VoidOnly;
                win.DialogResult = true;
                win.Close();
            };

            cancelBtn.Click += (s, e) =>
            {
                chosenAction = VoidDialogAction.None;
                win.DialogResult = false;
                win.Close();
            };

            root.Children.Add(reopenBtn);
            root.Children.Add(voidOnlyBtn);
            root.Children.Add(cancelBtn);

            win.Content = root;
            win.ShowDialog();

            string reason = string.IsNullOrWhiteSpace(reasonCombo.Text)
                ? "إلغاء فاتورة من شاشة الطلبات"
                : reasonCombo.Text.Trim();

            return (chosenAction, reason, restoreStockCheck.IsChecked == true);
        }

        public async void CompleteOrder(Order order)
        {
            if (order != null && order.Status == OrderStatus.Preparing)
            {
                try
                {
                    order.Status = OrderStatus.Completed;
                    order.CompletedAt = DateTime.Now;
                    
                    await _orderService.UpdateOrderAsync(order);
                    
                    ordersProberty.FilterOrders();
                    UpdateStatistics();
                    
                    MessageBox.Show("تم تغيير حالة الطلب إلى مكتمل", 
                        "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"حدث خطأ أثناء تحديث حالة الطلب: {ex.Message}", 
                        "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
    
    public static class OrderExtensions
    {
        public static string GetStatusText(this Order order)
        {
            return order.Status switch
            {
                OrderStatus.Completed => "مكتمل",
                OrderStatus.Preparing => "قيد التحضير",
                OrderStatus.Ready => "جاهز للتسليم",
                OrderStatus.Served => "قيد التوصيل",
                OrderStatus.Cancelled => "ملغي",
                OrderStatus.Pending => "مدفوع",
                _ => "غير محدد"
            };
        }
        
        public static string GetPaymentMethodText(this Order order)
        {
            return "نقدي";
        }
        
        public static int GetItemsCount(this Order order)
        {
            return order.OrderItems?.Sum(item => item.Quantity) ?? 0;
        }
        
        public static bool CanCancel(this Order order)
        {
            return order.Status != OrderStatus.Cancelled && order.Status != OrderStatus.Returned && order.Status != OrderStatus.PartiallyReturned;
        }
    }
}
