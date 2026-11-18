using System;
using System.Collections.Generic;
using POSSystem.WPF.Helpers;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using POSSystem.WPF.Models;
using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.Shered.Commands;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.ViewModels.Suppliers;

namespace POSSystem.WPF.ViewModels.Purchases
{
    public partial class PurchaseManagementViewModel : BaseViewModel, POSSystem.WPF.Services.ISearchable
    {
        public void ApplySearch(string query)
        {
            PurchaseManagementProberty.ReportSupplierName = query;
            ExecuteLoadReportAsync().SafeFireAndForget();
        }
        public PurchaseManagementViewModel()
        {
            var sp = POSSystem.WPF.App.ServiceProvider;
            _warehouseService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IWarehouseService>(sp);
            _purchaseService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IPurchaseService>(sp);
            _addonService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IAddonService>(sp);
            _supplierService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.ISupplierService>(sp);
            _purchaseReturnService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IPurchaseReturnService>(sp);
            _receiptVoucherService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<POSSystem.WPF.Business.Services.interfase.IReceiptVoucherService>(sp);
            PurchaseManagementProberty = new PurchaseManagementProberty();
            PurchaseManagementProberty.OnPaymentChannelChanged = () => _ = RefreshWalletAndChannelBalancesAsync();
            SupplierVM = new SupplierManagementViewModel();

            Commands = new PurchaseManagementCommands(this);
            LoadDataAsync().SafeFireAndForget();

            PurchaseManagementProberty.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PurchaseManagementProberty.SelectedSupplierId) ||
                    e.PropertyName == nameof(PurchaseManagementProberty.SupplierName) ||
                    e.PropertyName == nameof(PurchaseManagementProberty.SelectedWarehouse) ||
                    e.PropertyName == nameof(PurchaseManagementProberty.PaymentMethod))
                {
                    ((RelayCommand)Commands.SaveCommand).RaiseCanExecuteChanged();
                }
            };

            SupplierVM.SuppliersChanged += async () => await RefreshSuppliersAsync();
            ServiceLocator.InventoryChanged += OnInventoryChanged;
            ServiceLocator.AddonsChanged += OnAddonsChanged;
        }

        public override void Dispose()
        {
            if (_isDisposed) return;
            ServiceLocator.InventoryChanged -= OnInventoryChanged;
            ServiceLocator.AddonsChanged -= OnAddonsChanged;
            base.Dispose();
        }

        private void OnAddonsChanged()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
            {
                var addons = await _addonService.GetAllAddonsAsync();
                PurchaseManagementProberty.Addons = new ObservableCollection<Addon>(addons);
            }));
        }

        private void OnInventoryChanged()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
            {
                await RefreshWarehousesAsync();
            }));
        }

        public async Task LoadDataAsync()
        {
            try
            {
                var warehouses = await _warehouseService.GetAllWarehousesAsync();
                var addons = await _addonService.GetAllAddonsAsync();
                
                PurchaseManagementProberty.Warehouses = new ObservableCollection<Warehouse>(warehouses);
                PurchaseManagementProberty.Addons = new ObservableCollection<Addon>(addons);

                await RefreshSuppliersAsync();
                
                if (PurchaseManagementProberty.SelectedWarehouse != null)
                {
                    PurchaseManagementProberty.LoadProductsForWarehouse(PurchaseManagementProberty.SelectedWarehouse.Id);
                }

                await RefreshWalletAndChannelBalancesAsync();
                await ExecuteLoadReportAsync();
                await ExecuteLoadReturnReportAsync();
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"حدث خطأ أثناء تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        public async Task RefreshWarehousesAsync()
        {
            try
            {
                var warehouses = await _warehouseService.GetAllWarehousesAsync();
                var currentSelectedId = PurchaseManagementProberty.SelectedWarehouse?.Id;

                PurchaseManagementProberty.Warehouses = new ObservableCollection<Warehouse>(warehouses);

                if (currentSelectedId.HasValue)
                {
                    PurchaseManagementProberty.SelectedWarehouse = PurchaseManagementProberty.Warehouses.FirstOrDefault(w => w.Id == currentSelectedId.Value);
                }
            }
            catch (Exception)
            {
            }
        }

        public async Task RefreshSuppliersAsync()
        {
            try
            {
                var allSuppliers = await _supplierService.GetAllSuppliersAsync();
                PurchaseManagementProberty.AllSuppliers = new ObservableCollection<Supplier>(allSuppliers);

                var allNames = allSuppliers.Select(s => s.Name)
                                .OrderBy(s => s)
                                .ToList();

                PurchaseManagementProberty.Suppliers = new ObservableCollection<string>(allNames);
            }
            catch (Exception)
            {
            }
        }

        public async Task ExecuteLoadReportAsync()
        {
            try
            {
                var purchases = await _purchaseService.GetByDateRangeAsync(PurchaseManagementProberty.ReportStartDate, PurchaseManagementProberty.ReportEndDate);

                var searchTerm = PurchaseManagementProberty.ReportSupplierName?.Trim();
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var matchedSup = PurchaseManagementProberty.AllSuppliers?
                        .FirstOrDefault(s => POSSystem.WPF.Data.Repositories.PurchaseRepository.IsArabicSupplierMatch(s.Name, searchTerm));

                    purchases = purchases.Where(p =>
                        (matchedSup != null && p.SupplierId == matchedSup.Id) ||
                        POSSystem.WPF.Data.Repositories.PurchaseRepository.IsArabicSupplierMatch(p.SupplierName, searchTerm) ||
                        POSSystem.WPF.Data.Repositories.PurchaseRepository.IsArabicSupplierMatch(p.Supplier?.Name, searchTerm) ||
                        POSSystem.WPF.Data.Repositories.PurchaseRepository.IsArabicSupplierMatch(p.Name, searchTerm) ||
                        (p.PurchaseNumber != null && p.PurchaseNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
                }

                var purchaseList = purchases.OrderByDescending(p => p.PurchaseDate).ToList();
                PurchaseManagementProberty.PurchaseReport = new ObservableCollection<Purchase>(purchaseList);
                PurchaseManagementProberty.PurchasePager.SetItems(purchaseList);
                PurchaseManagementProberty.TotalReportAmount = purchaseList.Sum(p => p.TotalAmount);

                await LoadReturnTotalsForReportAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحميل التقرير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadReturnTotalsForReportAsync()
        {
            var searchTerm = PurchaseManagementProberty.ReportSupplierName?.Trim() ?? string.Empty;
            var returns = await _purchaseReturnService.SearchAsync(
                searchTerm,
                PurchaseManagementProberty.ReportStartDate,
                PurchaseManagementProberty.ReportEndDate);
            var returnList = returns.ToList();
            PurchaseManagementProberty.TotalReturnReportAmount = returnList.Sum(r => r.TotalAmount);
        }

        public async Task ExecuteLoadReturnReportAsync()
        {
            try
            {
                var searchTerm = PurchaseManagementProberty.ReportSupplierName?.Trim() ?? string.Empty;
                var returns = await _purchaseReturnService.SearchAsync(searchTerm, PurchaseManagementProberty.ReportStartDate, PurchaseManagementProberty.ReportEndDate);
                var returnList = returns.OrderByDescending(r => r.ReturnDate).ToList();
                PurchaseManagementProberty.PurchaseReturnReport = new ObservableCollection<PurchaseReturn>(returnList);
                PurchaseManagementProberty.TotalReturnReportAmount = returnList.Sum(r => r.TotalAmount);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحميل تقرير المرتجعات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateTotalInvoiceAmount()
        {
            PurchaseManagementProberty.TotalInvoiceAmount = PurchaseManagementProberty.PurchaseItems.Sum(x => x.TotalPrice);
            ((RelayCommand)Commands.SaveCommand).RaiseCanExecuteChanged();
        }

        public bool CanExecuteAddToList()
        {
            if (PurchaseManagementProberty.IsAddonMode)
                return PurchaseManagementProberty.SelectedAddon != null && PurchaseManagementProberty.Quantity > 0 && (PurchaseManagementProberty.IsInternalProduction || PurchaseManagementProberty.Cost > 0);
            else
                return PurchaseManagementProberty.SelectedProduct != null && PurchaseManagementProberty.Quantity > 0 && PurchaseManagementProberty.Cost > 0;
        }

        public void ExecuteAddToList()
        {
            if (!PurchaseManagementProberty.IsAddonMode && PurchaseManagementProberty.SelectedProduct != null)
            {
                if (PurchaseManagementProberty.IsBagOrPackageProduct &&
                    (!PurchaseManagementProberty.PackageWeightPerUnit.HasValue || PurchaseManagementProberty.PackageWeightPerUnit.Value <= 0))
                {
                    MessageBox.Show(
                        "يرجى إدخال حجم الكيس بالكيلو (مثال: 40 كيلو) في حقل (حجم الكيس) ليتم ضربه في الكمية وتخزينه بالكيلو في المخزون.",
                        "تنبيه حجم الكيس",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            var purchaseitem = new PurchaseItem
            {
                Quantity = PurchaseManagementProberty.Quantity,
                UnitPrice = PurchaseManagementProberty.Cost,
                TotalPrice = PurchaseManagementProberty.SubTotal
            };

            if (PurchaseManagementProberty.IsAddonMode)
            {
                if (PurchaseManagementProberty.SelectedAddon == null) return;
                purchaseitem.AddonId = PurchaseManagementProberty.SelectedAddon.Id;
                purchaseitem.Addon = PurchaseManagementProberty.SelectedAddon;
            }
            else
            {
                if (PurchaseManagementProberty.SelectedProduct == null) return;
                purchaseitem.ProductId = PurchaseManagementProberty.SelectedProduct.Id;
                purchaseitem.Product = PurchaseManagementProberty.SelectedProduct;
                if (PurchaseManagementProberty.IsCarcassProduct && PurchaseManagementProberty.PortionsPerCarcass.HasValue && PurchaseManagementProberty.PortionsPerCarcass.Value > 0)
                {
                    purchaseitem.Notes = $"أنفار: {PurchaseManagementProberty.PortionsPerCarcass.Value}";
                }
                else if (PurchaseManagementProberty.IsBagOrPackageProduct && PurchaseManagementProberty.PackageWeightPerUnit.HasValue && PurchaseManagementProberty.PackageWeightPerUnit.Value > 0)
                {
                    decimal bagW = PurchaseManagementProberty.PackageWeightPerUnit.Value;
                    decimal totalKg = PurchaseManagementProberty.Quantity * bagW;
                    purchaseitem.Notes = $"حجم الكيس: {bagW:0.##} كجم - إجمالي المخزون: {totalKg:0.##} كجم";
                }
            }

            purchaseitem.PropertyChanged += PurchaseItem_PropertyChanged;
            PurchaseManagementProberty.PurchaseItems.Add(purchaseitem);

            UpdateTotalInvoiceAmount();

            PurchaseManagementProberty.SelectedProduct = null;
            PurchaseManagementProberty.SelectedAddon = null;
            PurchaseManagementProberty.IsInternalProduction = false;
            PurchaseManagementProberty.IsCarcassProduct = false;
            PurchaseManagementProberty.PortionsPerCarcass = null;
            PurchaseManagementProberty.IsBagOrPackageProduct = false;
            PurchaseManagementProberty.PackageWeightPerUnit = null;
            PurchaseManagementProberty.Quantity = 1;
            PurchaseManagementProberty.Cost = 0;
        }

        private void PurchaseItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PurchaseItem.TotalPrice) ||
                e.PropertyName == nameof(PurchaseItem.Quantity) ||
                e.PropertyName == nameof(PurchaseItem.UnitPrice))
            {
                UpdateTotalInvoiceAmount();
            }
        }

        public void ExecuteRemoveFromList(PurchaseItem item)
        {
            if (item != null)
            {
                item.PropertyChanged -= PurchaseItem_PropertyChanged;
                PurchaseManagementProberty.PurchaseItems.Remove(item);
                UpdateTotalInvoiceAmount();
            }
        }

        public void ExecuteEditCartItem(PurchaseItem? item)
        {
            if (item == null) return;
            item.PropertyChanged -= PurchaseItem_PropertyChanged;
            PurchaseManagementProberty.PurchaseItems.Remove(item);
            UpdateTotalInvoiceAmount();

            if (item.ProductId.HasValue)
            {
                PurchaseManagementProberty.IsAddonMode = false;
                var prod = PurchaseManagementProberty.Products.FirstOrDefault(p => p.Id == item.ProductId.Value) ?? item.Product;
                PurchaseManagementProberty.SelectedProduct = prod;
                PurchaseManagementProberty.Quantity = item.Quantity;
                PurchaseManagementProberty.Cost = item.UnitPrice;

                if (PurchaseService.TryDetectBagWeight(prod?.Name, item.Notes, out decimal parsedBagW) && parsedBagW > 0)
                {
                    PurchaseManagementProberty.IsBagOrPackageProduct = true;
                    PurchaseManagementProberty.PackageWeightPerUnit = parsedBagW;
                }
            }
            else if (item.AddonId.HasValue)
            {
                PurchaseManagementProberty.IsAddonMode = true;
                var addon = PurchaseManagementProberty.Addons.FirstOrDefault(a => a.Id == item.AddonId.Value) ?? item.Addon;
                PurchaseManagementProberty.SelectedAddon = addon;
                PurchaseManagementProberty.Quantity = item.Quantity;
                PurchaseManagementProberty.Cost = item.UnitPrice;
            }
        }

        public async Task ExecuteViewPurchaseDetailsAsync(Purchase? purchase)
        {
            if (purchase == null) return;
            try
            {
                var fullPurchase = await _purchaseService.GetPurchaseWithItemsAsync(purchase.Id) ?? purchase;
                var items = fullPurchase.PurchaseItems?.ToList() ?? new List<PurchaseItem>();

                foreach (var it in items)
                {
                    if (it.Product == null && it.ProductId.HasValue)
                    {
                        it.Product = PurchaseManagementProberty.Products.FirstOrDefault(p => p.Id == it.ProductId.Value);
                    }
                    if (it.Addon == null && it.AddonId.HasValue)
                    {
                        it.Addon = PurchaseManagementProberty.Addons.FirstOrDefault(a => a.Id == it.AddonId.Value);
                    }
                }

                string warehouseName = fullPurchase.Warehouse?.Name
                    ?? PurchaseManagementProberty.Warehouses.FirstOrDefault(w => w.Id == fullPurchase.WarehouseId)?.Name
                    ?? "المستودع الرئيسي";

                var win = new Window
                {
                    Title = $"تفاصيل فاتورة المشتريات رقم: {fullPurchase.PurchaseNumber}",
                    Width = 860,
                    Height = 620,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    FlowDirection = FlowDirection.RightToLeft,
                    Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#f8fafc"))
                };

                var mainGrid = new System.Windows.Controls.Grid { Margin = new Thickness(20) };
                mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
                mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
                mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });

                // Header Banner
                var headerBorder = new System.Windows.Controls.Border
                {
                    Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00358c")),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(20, 14, 20, 14),
                    Margin = new Thickness(0, 0, 0, 14)
                };
                var headerStack = new System.Windows.Controls.StackPanel();
                headerStack.Children.Add(new System.Windows.Controls.TextBlock
                {
                    Text = $"📄 استعراض فاتورة المشتريات رقم: ({fullPurchase.PurchaseNumber})",
                    FontSize = 19,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.White
                });
                headerStack.Children.Add(new System.Windows.Controls.TextBlock
                {
                    Text = $"المورد: {fullPurchase.SupplierName}   |   التاريخ: {fullPurchase.PurchaseDate:yyyy/MM/dd}   |   المستودع: {warehouseName}   |   طريقة الدفع: {fullPurchase.PaymentMethodDisplay}",
                    FontSize = 13,
                    Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#dbeafe")),
                    Margin = new Thickness(0, 5, 0, 0)
                });
                headerBorder.Child = headerStack;
                System.Windows.Controls.Grid.SetRow(headerBorder, 0);
                mainGrid.Children.Add(headerBorder);

                // Summary Strip
                var summaryBorder = new System.Windows.Controls.Border
                {
                    Background = System.Windows.Media.Brushes.White,
                    BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#e2e8f0")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(16, 10, 16, 10),
                    Margin = new Thickness(0, 0, 0, 12)
                };
                var summaryText = new System.Windows.Controls.TextBlock
                {
                    Text = $"إجمالي عدد الأصناف في الفاتورة: {items.Count} صنف   |   إجمالي الكميات: {items.Sum(x => x.Quantity)}   |   إجمالي مبلغ الفاتورة: {fullPurchase.TotalAmount:N0} ريال",
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1e293b"))
                };
                summaryBorder.Child = summaryText;
                System.Windows.Controls.Grid.SetRow(summaryBorder, 1);
                mainGrid.Children.Add(summaryBorder);

                // Items DataGrid
                var dg = new System.Windows.Controls.DataGrid
                {
                    ItemsSource = items.Select((item, idx) => new
                    {
                        Index = idx + 1,
                        ProductName = item.DisplayName,
                        UnitName = item.Product?.PortionUnit != null ? PortionUnitHelper.GetDisplayName(item.Product.PortionUnit.Value) : "وحدة",
                        Quantity = item.Quantity,
                        UnitPriceText = $"{item.UnitPrice:N0} ريال",
                        TotalPriceText = $"{item.TotalPrice:N0} ريال",
                        Notes = string.IsNullOrWhiteSpace(item.Notes) ? "—" : item.Notes
                    }).ToList(),
                    AutoGenerateColumns = false,
                    IsReadOnly = true,
                    CanUserAddRows = false,
                    FontSize = 13,
                    RowHeight = 38,
                    ColumnHeaderHeight = 40,
                    Background = System.Windows.Media.Brushes.White,
                    BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#cbd5e1")),
                    BorderThickness = new Thickness(1)
                };

                dg.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "#", Binding = new System.Windows.Data.Binding("Index"), Width = 45 });
                dg.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "اسم الصنف / المادة المشتراة", Binding = new System.Windows.Data.Binding("ProductName"), Width = new System.Windows.Controls.DataGridLength(2, System.Windows.Controls.DataGridLengthUnitType.Star) });
                dg.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "الوحدة", Binding = new System.Windows.Data.Binding("UnitName"), Width = 90 });
                dg.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "الكمية", Binding = new System.Windows.Data.Binding("Quantity"), Width = 90 });
                dg.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "سعر الوحدة", Binding = new System.Windows.Data.Binding("UnitPriceText"), Width = 120 });
                dg.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "الإجمالي", Binding = new System.Windows.Data.Binding("TotalPriceText"), Width = 130 });
                dg.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "ملاحظات", Binding = new System.Windows.Data.Binding("Notes"), Width = new System.Windows.Controls.DataGridLength(1.2, System.Windows.Controls.DataGridLengthUnitType.Star) });

                System.Windows.Controls.Grid.SetRow(dg, 2);
                mainGrid.Children.Add(dg);

                // Footer Buttons
                var footerPanel = new System.Windows.Controls.StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 14, 0, 0)
                };

                var editBtn = new System.Windows.Controls.Button
                {
                    Content = "✏️ تعديل هذه الفاتورة",
                    Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0284c7")),
                    Foreground = System.Windows.Media.Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    Padding = new Thickness(18, 8, 18, 8),
                    Height = 40,
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(0, 0, 10, 0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                editBtn.Click += async (s, e) =>
                {
                    win.Close();
                    await ExecuteEditPurchaseAsync(fullPurchase);
                };

                var printBtn = new System.Windows.Controls.Button
                {
                    Content = "📊 حفظ / طباعة الفاتورة",
                    Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#16a34a")),
                    Foreground = System.Windows.Media.Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    Padding = new Thickness(18, 8, 18, 8),
                    Height = 40,
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(0, 0, 10, 0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                printBtn.Click += (s, e) =>
                {
                    try
                    {
                        var headers = new[] { "اسم الصنف", "الوحدة", "الكمية", "سعر الوحدة", "الإجمالي", "ملاحظات" };
                        Services.PrintService.PrintReport(
                            $"فاتورة مشتريات رقم {fullPurchase.PurchaseNumber}",
                            $"المورد: {fullPurchase.SupplierName} | التاريخ: {fullPurchase.PurchaseDate:yyyy/MM/dd} | الدفع: {fullPurchase.PaymentMethodDisplay}",
                            items,
                            headers,
                            (object obj) =>
                            {
                                if (obj is not PurchaseItem pi) return new string[headers.Length];
                                string unitStr = pi.Product?.PortionUnit != null ? PortionUnitHelper.GetDisplayName(pi.Product.PortionUnit.Value) : "وحدة";
                                return new[]
                                {
                                    pi.DisplayName,
                                    unitStr,
                                    pi.Quantity.ToString(),
                                    $"{pi.UnitPrice:N0} ريال",
                                    $"{pi.TotalPrice:N0} ريال",
                                    pi.Notes ?? ""
                                };
                            },
                            $"إجمالي فاتورة المشتريات: {fullPurchase.TotalAmount:N0} ريال",
                            preferPdf: true);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"خطأ أثناء الطباعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                var closeBtn = new System.Windows.Controls.Button
                {
                    Content = "إغلاق",
                    Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#64748b")),
                    Foreground = System.Windows.Media.Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    Padding = new Thickness(22, 8, 22, 8),
                    Height = 40,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                closeBtn.Click += (s, e) => win.Close();

                footerPanel.Children.Add(editBtn);
                footerPanel.Children.Add(printBtn);
                footerPanel.Children.Add(closeBtn);
                System.Windows.Controls.Grid.SetRow(footerPanel, 3);
                mainGrid.Children.Add(footerPanel);

                win.Content = mainGrid;
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء استعراض الفاتورة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task ExecuteEditPurchaseAsync(Purchase? purchase)
        {
            if (purchase == null) return;
            try
            {
                var fullPurchase = await _purchaseService.GetPurchaseWithItemsAsync(purchase.Id) ?? purchase;

                foreach (var oldItem in PurchaseManagementProberty.PurchaseItems)
                {
                    oldItem.PropertyChanged -= PurchaseItem_PropertyChanged;
                }
                PurchaseManagementProberty.PurchaseItems.Clear();

                PurchaseManagementProberty.EditingPurchaseId = fullPurchase.Id;
                PurchaseManagementProberty.EditingPurchaseNumber = fullPurchase.PurchaseNumber ?? $"#{fullPurchase.Id}";
                PurchaseManagementProberty.PurchaseDate = fullPurchase.PurchaseDate;

                if (fullPurchase.WarehouseId.HasValue)
                {
                    var wh = PurchaseManagementProberty.Warehouses.FirstOrDefault(w => w.Id == fullPurchase.WarehouseId.Value);
                    if (wh != null)
                    {
                        PurchaseManagementProberty.SelectedWarehouse = wh;
                    }
                }
                if (PurchaseManagementProberty.SelectedWarehouse == null && PurchaseManagementProberty.Warehouses.Any())
                {
                    var firstProdWhId = fullPurchase.PurchaseItems?.FirstOrDefault(i => i.Product?.WarehouseId != null)?.Product?.WarehouseId;
                    PurchaseManagementProberty.SelectedWarehouse = firstProdWhId.HasValue
                        ? (PurchaseManagementProberty.Warehouses.FirstOrDefault(w => w.Id == firstProdWhId.Value) ?? PurchaseManagementProberty.Warehouses.First())
                        : PurchaseManagementProberty.Warehouses.First();
                }

                PurchaseManagementProberty.SupplierName = fullPurchase.SupplierName ?? string.Empty;
                if (fullPurchase.SupplierId.HasValue)
                {
                    PurchaseManagementProberty.SelectedSupplierId = fullPurchase.SupplierId.Value;
                }

                if (fullPurchase.PaymentMethod == PaymentMethod.OnCredit)
                {
                    PurchaseManagementProberty.PaymentMethod = "آجل (دين)";
                }
                else if (fullPurchase.PaymentMethod == PaymentMethod.MobilePayment)
                {
                    PurchaseManagementProberty.PaymentMethod = "إيداع / محفظة للمورد";
                    if (!string.IsNullOrWhiteSpace(fullPurchase.DepositInfo?.BankOrWallet))
                        PurchaseManagementProberty.DepositBankOrWallet = fullPurchase.DepositInfo!.BankOrWallet;
                }
                else if (fullPurchase.PaymentMethod == PaymentMethod.DebitCard)
                {
                    PurchaseManagementProberty.PaymentMethod = "بطاقة (شبكة)";
                }
                else if (fullPurchase.Notes != null && fullPurchase.Notes.Contains("سند صرف"))
                {
                    PurchaseManagementProberty.PaymentMethod = "سند صرف";
                }
                else
                {
                    PurchaseManagementProberty.PaymentMethod = "نقدي (كاش)";
                }

                if (fullPurchase.PurchaseItems != null)
                {
                    foreach (var item in fullPurchase.PurchaseItems)
                    {
                        var cloned = new PurchaseItem
                        {
                            Id = item.Id,
                            PurchaseId = fullPurchase.Id,
                            ProductId = item.ProductId,
                            Product = item.Product ?? (item.ProductId.HasValue ? PurchaseManagementProberty.Products.FirstOrDefault(p => p.Id == item.ProductId.Value) : null),
                            AddonId = item.AddonId,
                            Addon = item.Addon ?? (item.AddonId.HasValue ? PurchaseManagementProberty.Addons.FirstOrDefault(a => a.Id == item.AddonId.Value) : null),
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            TotalPrice = item.TotalPrice > 0 ? item.TotalPrice : (item.Quantity * item.UnitPrice),
                            Notes = item.Notes
                        };
                        cloned.PropertyChanged += PurchaseItem_PropertyChanged;
                        PurchaseManagementProberty.PurchaseItems.Add(cloned);
                    }
                }

                UpdateTotalInvoiceAmount();
                PurchaseManagementProberty.ActiveTabIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحميل الفاتورة للتعديل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool CanExecuteSave()
        {
            bool hasProducts = PurchaseManagementProberty.PurchaseItems.Any(pi => pi.ProductId.HasValue);
            bool isWarehouseValid = !hasProducts || PurchaseManagementProberty.SelectedWarehouse != null;
            bool isSupplierValid = !string.IsNullOrWhiteSpace(PurchaseManagementProberty.SupplierName);
            
            if (PurchaseManagementProberty.IsCreditPayment)
            {
                isSupplierValid = PurchaseManagementProberty.SelectedSupplierId.HasValue;
            }

            return isWarehouseValid &&
                    PurchaseManagementProberty.PurchaseItems.Any() &&
                    isSupplierValid;
        }

        public async Task RefreshWalletAndChannelBalancesAsync()
        {
            try
            {
                var accounts = await POSSystem.WPF.Business.ServiceLocator.FinancialAccountService.GetAllAccountsAsync(includeInactive: false);
                var walletAccounts = accounts.Where(a => a.AccountType != FinancialAccountType.MainCashBox).ToList();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    PurchaseManagementProberty.WalletBalancesList.Clear();
                    foreach (var w in walletAccounts)
                    {
                        PurchaseManagementProberty.WalletBalancesList.Add(w);
                    }

                    var activeNames = POSSystem.WPF.Helpers.FinancialChannelHelper.GetActiveWalletsList();
                    foreach (var wn in activeNames)
                    {
                        if (!PurchaseManagementProberty.DepositWalletsList.Contains(wn))
                            PurchaseManagementProberty.DepositWalletsList.Add(wn);
                    }
                });

                if (PurchaseManagementProberty.IsCreditPayment)
                {
                    PurchaseManagementProberty.IsChannelBalanceInsufficient = false;
                    return;
                }

                string targetChannel;
                string displayName;

                if (PurchaseManagementProberty.IsDepositPayment || PurchaseManagementProberty.IsVoucherPayment)
                {
                    targetChannel = string.IsNullOrWhiteSpace(PurchaseManagementProberty.DepositBankOrWallet)
                        ? "حاسب كريمي"
                        : PurchaseManagementProberty.DepositBankOrWallet.Trim();
                    displayName = targetChannel;
                }
                else if (PurchaseManagementProberty.PaymentMethod == "بطاقة (شبكة)")
                {
                    var settings = POSSystem.WPF.Services.SettingsService.LoadSettings();
                    targetChannel = !string.IsNullOrWhiteSpace(settings?.DefaultBankOrWallet) ? settings.DefaultBankOrWallet : "حاسب كريمي";
                    displayName = $"شبكة ({targetChannel})";
                }
                else
                {
                    targetChannel = POSSystem.WPF.Helpers.FinancialChannelHelper.ChannelCash;
                    displayName = "الصندوق الرئيسي (درج الكاشير)";
                }

                decimal available = await POSSystem.WPF.Business.ServiceLocator.FinancialChannelService.GetAvailableBalanceAsync(targetChannel);
                if (targetChannel == POSSystem.WPF.Helpers.FinancialChannelHelper.ChannelCash)
                {
                    var mainCashAcc = accounts.FirstOrDefault(a => a.AccountType == FinancialAccountType.MainCashBox);
                    if (mainCashAcc != null && mainCashAcc.CurrentBalance > available)
                        available = mainCashAcc.CurrentBalance;
                }
                else
                {
                    var matchedAcc = walletAccounts.FirstOrDefault(a =>
                        a.AccountName.Equals(targetChannel, StringComparison.OrdinalIgnoreCase) ||
                        a.BankOrWalletName.Equals(targetChannel, StringComparison.OrdinalIgnoreCase));
                    if (matchedAcc != null)
                        available = matchedAcc.CurrentBalance;
                }

                decimal req = PurchaseManagementProberty.TotalInvoiceAmount;
                bool insufficient = available <= 0 || (req > 0 && available < req);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    PurchaseManagementProberty.SelectedChannelBalance = available;
                    PurchaseManagementProberty.IsChannelBalanceInsufficient = insufficient;
                    PurchaseManagementProberty.SelectedChannelTitleText = insufficient
                        ? $"⚠️ رصيد ({displayName}) غير كافٍ للشراء! المتاح:"
                        : $"✅ الرصيد المتاح في ({displayName}):";
                    PurchaseManagementProberty.SelectedChannelBalanceText = $"{available:N0} ريال";
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing purchase wallet balances: {ex.Message}");
            }
        }

        public async Task ExecuteSaveAsync()
        {
            try
            {
                bool hasProducts = PurchaseManagementProberty.PurchaseItems.Any(pi => pi.ProductId.HasValue);
                if ((hasProducts && PurchaseManagementProberty.SelectedWarehouse == null) || !PurchaseManagementProberty.PurchaseItems.Any()) return;

                PaymentMethod paymentMethodEnum;
                string paymentNotes;

                if (PurchaseManagementProberty.IsCreditPayment)
                {
                    paymentMethodEnum = PaymentMethod.OnCredit;
                    paymentNotes = "طريقة الدفع: آجل (دين)";
                }
                else if (PurchaseManagementProberty.IsVoucherPayment)
                {
                    paymentMethodEnum = PaymentMethod.Cash;
                    string wallet = string.IsNullOrWhiteSpace(PurchaseManagementProberty.DepositBankOrWallet)
                        ? "سند صرف نقدي"
                        : PurchaseManagementProberty.DepositBankOrWallet.Trim();
                    string refNum = PurchaseManagementProberty.DepositReferenceNumber?.Trim() ?? string.Empty;
                    string currency = PurchaseManagementProberty.DepositCurrency ?? "ريال يمني";

                    string depositTag = OrderDepositHelper.FormatDepositTag(currency, PurchaseManagementProberty.TotalInvoiceAmount, wallet, refNum);
                    paymentNotes = $"طريقة الدفع: سند صرف {depositTag}";

                    if (!string.IsNullOrWhiteSpace(PurchaseManagementProberty.DepositNotes))
                    {
                        paymentNotes += $" - {PurchaseManagementProberty.DepositNotes.Trim()}";
                    }
                }
                else if (PurchaseManagementProberty.IsDepositPayment)
                {
                    paymentMethodEnum = PaymentMethod.MobilePayment;
                    string wallet = string.IsNullOrWhiteSpace(PurchaseManagementProberty.DepositBankOrWallet)
                        ? "محفظة إلكترونية"
                        : PurchaseManagementProberty.DepositBankOrWallet.Trim();
                    string refNum = PurchaseManagementProberty.DepositReferenceNumber?.Trim() ?? string.Empty;
                    string currency = PurchaseManagementProberty.DepositCurrency ?? "ريال يمني";

                    string depositTag = OrderDepositHelper.FormatDepositTag(currency, PurchaseManagementProberty.TotalInvoiceAmount, wallet, refNum);
                    paymentNotes = $"طريقة الدفع: إيداع / محفظة للمورد {depositTag}";

                    if (!string.IsNullOrWhiteSpace(PurchaseManagementProberty.DepositNotes))
                    {
                        paymentNotes += $" - {PurchaseManagementProberty.DepositNotes.Trim()}";
                    }
                }
                else if (PurchaseManagementProberty.PaymentMethod == "بطاقة (شبكة)")
                {
                    paymentMethodEnum = PaymentMethod.DebitCard;
                    paymentNotes = "طريقة الدفع: بطاقة (شبكة)";
                }
                else
                {
                    paymentMethodEnum = PaymentMethod.Cash;
                    paymentNotes = "طريقة الدفع: نقدي (كاش)";
                }

                int? validSupplierId = null;
                if (PurchaseManagementProberty.SelectedSupplierId.HasValue && PurchaseManagementProberty.SelectedSupplierId.Value > 0)
                {
                    if (PurchaseManagementProberty.AllSuppliers != null && PurchaseManagementProberty.AllSuppliers.Any(s => s.Id == PurchaseManagementProberty.SelectedSupplierId.Value))
                    {
                        validSupplierId = PurchaseManagementProberty.SelectedSupplierId.Value;
                    }
                }

                if (PurchaseManagementProberty.IsCreditPayment && !validSupplierId.HasValue)
                {
                    MessageBox.Show("عملية الشراء الآجل تتطلب اختيار مورد مسجل من القائمة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool isEditing = PurchaseManagementProberty.IsEditingPurchase && PurchaseManagementProberty.EditingPurchaseId.HasValue;
                decimal previousPaidAmount = 0m;
                if (isEditing)
                {
                    var oldPurchase = await _purchaseService.GetPurchaseByIdAsync(PurchaseManagementProberty.EditingPurchaseId!.Value);
                    if (oldPurchase != null && oldPurchase.PaymentMethod != PaymentMethod.OnCredit)
                    {
                        previousPaidAmount = oldPurchase.TotalAmount;
                    }
                }

                // التحقق الإلزامي من كفاية الرصيد في المحفظة أو الصندوق قبل السماح بالشراء غير الآجل
                if (!PurchaseManagementProberty.IsCreditPayment)
                {
                    await RefreshWalletAndChannelBalancesAsync();
                    decimal requiredAmount = PurchaseManagementProberty.TotalInvoiceAmount;
                    decimal availableBalance = PurchaseManagementProberty.SelectedChannelBalance + previousPaidAmount;
                    string channelLabel = (PurchaseManagementProberty.IsDepositPayment || PurchaseManagementProberty.IsVoucherPayment)
                        ? (string.IsNullOrWhiteSpace(PurchaseManagementProberty.DepositBankOrWallet) ? "حاسب كريمي" : PurchaseManagementProberty.DepositBankOrWallet.Trim())
                        : (PurchaseManagementProberty.PaymentMethod == "بطاقة (شبكة)" ? "حساب الشبكة" : "الصندوق الرئيسي (درج الكاشير)");

                    if (availableBalance <= 0 || availableBalance < requiredAmount)
                    {
                        decimal shortage = Math.Max(0, requiredAmount - availableBalance);
                        MessageBox.Show(
                            $"عفواً، لا يمكن إتمام عملية الشراء لعدم كفاية الرصيد في ({channelLabel})!\n\n" +
                            $"• الرصيد المتاح حالياً في {channelLabel}: {availableBalance:N0} ريال\n" +
                            $"• إجمالي فاتورة المشتريات المطلوب: {requiredAmount:N0} ريال\n" +
                            $"• مقدار العجز المالي: {shortage:N0} ريال\n\n" +
                            "يرجى تغذية رصيد المحفظة/الصندوق أولاً، أو اختيار محفظة أخرى بها رصيد كافٍ، أو تغيير طريقة الدفع إلى (آجل / دين).",
                            "الرصيد غير كافٍ لإتمام الشراء",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
                }

                var purchase = new Purchase
                {
                    Id = isEditing ? PurchaseManagementProberty.EditingPurchaseId!.Value : 0,
                    PurchaseNumber = isEditing && !string.IsNullOrWhiteSpace(PurchaseManagementProberty.EditingPurchaseNumber)
                        ? PurchaseManagementProberty.EditingPurchaseNumber
                        : await _purchaseService.GeneratePurchaseNumberAsync(),
                    PurchaseDate = PurchaseManagementProberty.PurchaseDate,
                    SupplierName = PurchaseManagementProberty.SupplierName,
                    SupplierId = validSupplierId,
                    TotalAmount = PurchaseManagementProberty.TotalInvoiceAmount,
                    WarehouseId = PurchaseManagementProberty.SelectedWarehouse?.Id,
                    PaymentMethod = paymentMethodEnum,
                    Notes = paymentNotes,
                    UserId = AdminDashboardWindow._currentUser?.Id,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Name = PurchaseManagementProberty.SupplierName,
                    PurchaseItems = PurchaseManagementProberty.PurchaseItems.ToList(),
                };

                var result = isEditing
                    ? await _purchaseService.UpdatePurchaseAsync(purchase)
                    : await _purchaseService.CreatePurchaseAsync(purchase);
                
                if (result.SupplierId.HasValue && PurchaseManagementProberty.SelectedSupplierId == result.SupplierId)
                {
                    var updatedSupplier = await _supplierService.GetSupplierByIdAsync(result.SupplierId.Value);
                    if (updatedSupplier != null)
                    {
                        PurchaseManagementProberty.SelectedSupplierBalance = updatedSupplier.TotalBalance;
                    }
                }

                await LoadDataAsync();
                ServiceLocator.NotifySuppliersChanged();
                ServiceLocator.NotifyInventoryChanged();

                string voucherMsg = result.VoucherNumber != null
                    ? $"\nتم توليد سند صرف بنجاح برقم: ({result.VoucherNumber})"
                    : "";
                MessageBox.Show(isEditing ? $"تم حفظ تعديلات الفاتورة رقم ({result.PurchaseNumber}) وتحديث المخزون والأرصدة بنجاح." : $"تم حفظ الشراء وتحديث المخزون بنجاح.{voucherMsg}", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                ExecuteCancel(); 
            }
            catch (Exception ex)
            {
                var fullError = ex.InnerException != null ? $"{ex.Message}\n{ex.InnerException.Message}" : ex.Message;
                MessageBox.Show($"حدث خطأ أثناء حفظ الشراء: {fullError}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task ExecuteViewVoucherAsync(Purchase? purchase)
        {
            if (purchase == null) return;
            try
            {
                var vchNum = purchase.VoucherNumber;
                var allVouchers = await _receiptVoucherService.GetAllVouchersAsync();
                var voucher = allVouchers.FirstOrDefault(v => 
                    (!string.IsNullOrEmpty(vchNum) && v.VoucherNumber == vchNum) ||
                    (v.Notes != null && v.Notes.Contains(purchase.PurchaseNumber)));

                if (voucher != null)
                {
                    var msg = $"📄 تفاصيل سند الصرف المرتبط بالفاتورة:\n\n" +
                              $"• رقم السند: {voucher.VoucherNumber}\n" +
                              $"• نوع السند: {voucher.TypeText}\n" +
                              $"• اسم المورد: {voucher.CustomerName}\n" +
                              $"• المبلغ: {voucher.Amount:N0} ريال\n" +
                              $"• التاريخ: {voucher.VoucherDate:yyyy/MM/dd}\n" +
                              $"• رقم فاتورة الشراء: {purchase.PurchaseNumber}\n" +
                              $"• البيان والملاحظات:\n  {voucher.Notes}\n\n" +
                              $"هل تريد الانتقال إلى شاشة سندات القبض والصرف؟";

                    var result = MessageBox.Show(msg, "بيانات سند الصرف", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                    {
                        NavigationService.Instance.ShowExpenseManagementView(showVouchersByDefault: true);
                    }
                }
                else
                {
                    MessageBox.Show("لم يتم العثور على سند صرف مسجل لهذه الفاتورة (قد تكون فاتورة شراء آجل).", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحميل بيانات سند الصرف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ExecuteCancel()
        {
            foreach (var item in PurchaseManagementProberty.PurchaseItems)
            {
                item.PropertyChanged -= PurchaseItem_PropertyChanged;
            }
            PurchaseManagementProberty.PurchaseItems.Clear();
            PurchaseManagementProberty.EditingPurchaseId = null;
            PurchaseManagementProberty.EditingPurchaseNumber = string.Empty;
            PurchaseManagementProberty.PurchaseDate = DateTime.Now;
            PurchaseManagementProberty.SupplierName = string.Empty;
            PurchaseManagementProberty.SelectedSupplierId = null;
            PurchaseManagementProberty.DepositBankOrWallet = string.Empty;
            PurchaseManagementProberty.DepositReferenceNumber = string.Empty;
            PurchaseManagementProberty.DepositNotes = string.Empty;
            PurchaseManagementProberty.DepositCurrency = "ريال يمني";
            PurchaseManagementProberty.PaymentMethod = "نقدي (كاش)";
            UpdateTotalInvoiceAmount();
            OnRequestClose?.Invoke();
        }
    }
}
