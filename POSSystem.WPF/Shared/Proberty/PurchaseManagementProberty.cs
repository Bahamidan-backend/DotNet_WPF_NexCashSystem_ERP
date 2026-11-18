using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using POSSystem.WPF.Business;
using POSSystem.WPF.Models;
using static POSSystem.WPF.Models.Recipe;

namespace POSSystem.WPF.Shered.Proberty
{
    public class PurchaseManagementProberty : BaseModel
    {
        private ObservableCollection<WarehouseFromProduct> _products = new();
        private ObservableCollection<Warehouse> _warehouses = new();
        private ObservableCollection<PurchaseItem> _purchaseItems = new();
        private ObservableCollection<Purchase> _purchaseReport = new();
        private ObservableCollection<PurchaseReturn> _purchaseReturnReport = new();
        private ObservableCollection<string> _suppliers = new();
        public Pagination<Purchase> PurchasePager { get; } = new();
        private ObservableCollection<Addon> _addons = new();
        private List<Addon> _allAddons = new();
        private ObservableCollection<Supplier> _allSuppliers = new();
        public ObservableCollection<string> PaymentMethods { get; } = new() { "نقدي (كاش)", "إيداع / محفظة للمورد", "سند صرف", "بطاقة (شبكة)", "آجل (دين)" };

        private decimal _subTotal;
        private decimal _totalInvoiceAmount;
        private decimal _totalReportAmount;
        private decimal _totalReturnReportAmount;
        private string _supplierName = string.Empty;
        private string? _reportSupplierName;
        private DateTime _purchaseDate = DateTime.Now;
        private string _paymentMethod = "نقدي (كاش)";
        private decimal _selectedSupplierBalance;
        private DateTime _reportStartDate = DateTime.Now.AddDays(-30);
        private DateTime _reportEndDate = DateTime.Now;

        public PurchaseManagementProberty()
        {
        }

        private int _activeTabIndex;
        public int ActiveTabIndex
        {
            get => _activeTabIndex;
            set => SetProperty(ref _activeTabIndex, value);
        }

        private int? _editingPurchaseId;
        public int? EditingPurchaseId
        {
            get => _editingPurchaseId;
            set
            {
                if (SetProperty(ref _editingPurchaseId, value))
                {
                    OnPropertyChanged(nameof(IsEditingPurchase));
                    OnPropertyChanged(nameof(FormHeaderTitle));
                    OnPropertyChanged(nameof(SaveButtonText));
                }
            }
        }

        private string _editingPurchaseNumber = string.Empty;
        public string EditingPurchaseNumber
        {
            get => _editingPurchaseNumber;
            set
            {
                if (SetProperty(ref _editingPurchaseNumber, value))
                {
                    OnPropertyChanged(nameof(FormHeaderTitle));
                }
            }
        }

        public bool IsEditingPurchase => EditingPurchaseId.HasValue && EditingPurchaseId.Value > 0;
        public string FormHeaderTitle => IsEditingPurchase ? $"✏️ تعديل فاتورة المشتريات رقم: ({EditingPurchaseNumber})" : "تسجيل مشتريات جديدة";
        public string SaveButtonText => IsEditingPurchase ? "💾 حفظ تعديلات الفاتورة وتحديث المخزون" : "حفظ وتحديث المخزون";

        private bool _isCarcassProduct;
        public bool IsCarcassProduct
        {
            get => _isCarcassProduct;
            set => SetProperty(ref _isCarcassProduct, value);
        }

        private int? _portionsPerCarcass;
        public int? PortionsPerCarcass
        {
            get => _portionsPerCarcass;
            set => SetProperty(ref _portionsPerCarcass, value);
        }

        private bool _isBagOrPackageProduct;
        public bool IsBagOrPackageProduct
        {
            get => _isBagOrPackageProduct;
            set
            {
                if (SetProperty(ref _isBagOrPackageProduct, value))
                {
                    OnPropertyChanged(nameof(QuantityFieldLabel));
                    OnPropertyChanged(nameof(CostFieldLabel));
                    OnPropertyChanged(nameof(TotalPackageKg));
                    OnPropertyChanged(nameof(PackageConversionPreviewText));
                }
            }
        }

        private decimal? _packageWeightPerUnit;
        public decimal? PackageWeightPerUnit
        {
            get => _packageWeightPerUnit;
            set
            {
                if (SetProperty(ref _packageWeightPerUnit, value))
                {
                    OnPropertyChanged(nameof(TotalPackageKg));
                    OnPropertyChanged(nameof(PackageConversionPreviewText));
                }
            }
        }

        public string QuantityFieldLabel => IsBagOrPackageProduct ? "الكمية (عدد الأكياس)" : "الكمية";
        public string CostFieldLabel => IsBagOrPackageProduct ? "سعر شراء الكيس" : "سعر الشراء";

        public decimal TotalPackageKg =>
            IsBagOrPackageProduct && PackageWeightPerUnit.HasValue && PackageWeightPerUnit.Value > 0
                ? (Quantity * PackageWeightPerUnit.Value)
                : Quantity;

        public string PackageConversionPreviewText =>
            IsBagOrPackageProduct && PackageWeightPerUnit.HasValue && PackageWeightPerUnit.Value > 0
                ? $"📦 يتخزن بالمخزون: {TotalPackageKg:0.##} كجم ({Quantity} كيس × {PackageWeightPerUnit.Value:0.##} كجم)"
                : "أدخل حجم الكيس بالكيلو ليتم ضربه في الكمية";

        private WarehouseFromProduct? _selectedProduct;
        public WarehouseFromProduct? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (SetProperty(ref _selectedProduct, value))
                {
                    if (_selectedProduct != null)
                    {
                        Cost = _selectedProduct.Price;
                        UpdateCarcassDetection(_selectedProduct);
                        UpdateBagOrPackageDetection(_selectedProduct);
                    }
                    else
                    {
                        IsCarcassProduct = false;
                        PortionsPerCarcass = null;
                        IsBagOrPackageProduct = false;
                        PackageWeightPerUnit = null;
                    }
                    UpdateSubTotal();
                }
            }
        }

        private void UpdateCarcassDetection(WarehouseFromProduct prod)
        {
            string name = (prod.Name ?? "").Trim().ToLower();
            bool isPart = name.Contains("لحم") || name.Contains("رأس") || name.Contains("راس") || 
                          name.Contains("كبدة") || name.Contains("كبده") || name.Contains("ظهر") || 
                          name.Contains("كرشة") || name.Contains("كرشه") || name.Contains("كوارع") || 
                          name.Contains("نفر") || name.Contains("جزء") || name.Contains("ثمن");

            if (isPart)
            {
                IsCarcassProduct = false;
                PortionsPerCarcass = null;
                return;
            }

            if (name.Contains("خروف") || name.Contains("غنم") || name.Contains("ضأن") || name.Contains("تيس") || name.Contains("sheep") || name.Contains("lamb"))
            {
                IsCarcassProduct = true;
                PortionsPerCarcass = 16;
            }
            else if (name.Contains("عجل") || name.Contains("بقر") || name.Contains("ثور") || name.Contains("cow") || name.Contains("beef"))
            {
                IsCarcassProduct = true;
                PortionsPerCarcass = 50;
            }
            else if (name.Contains("جمل") || name.Contains("حاشي") || name.Contains("قعود") || name.Contains("camel"))
            {
                IsCarcassProduct = true;
                PortionsPerCarcass = 60;
            }
            else if (prod.PortionUnit == PortionUnit.Carcass || name.Contains("ذبيحة") || name.Contains("ذبيحه"))
            {
                IsCarcassProduct = true;
                PortionsPerCarcass = 16;
            }
            else
            {
                IsCarcassProduct = false;
                PortionsPerCarcass = null;
            }
        }

        private void UpdateBagOrPackageDetection(WarehouseFromProduct prod)
        {
            if (IsCarcassProduct || prod == null)
            {
                IsBagOrPackageProduct = false;
                PackageWeightPerUnit = null;
                return;
            }

            string name = (prod.Name ?? "").Trim().ToLowerInvariant();
            string desc = (prod.Description ?? "").Trim();

            // Check if previously saved bag weight exists in Description: [شراء: كيس=40]
            decimal savedBagWeight = 0m;
            if (!string.IsNullOrWhiteSpace(desc))
            {
                var m = Regex.Match(desc, @"\[شراء:\s*كيس\s*=\s*(\d+(?:[\.,]\d+)?)\]");
                if (m.Success && decimal.TryParse(m.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedW) && parsedW > 0)
                {
                    savedBagWeight = parsedW;
                }
            }

            bool isBagUnit = prod.PortionUnit == PortionUnit.Bag ||
                             prod.PortionUnit == PortionUnit.Carton ||
                             prod.PortionUnit == PortionUnit.Box ||
                             prod.PortionUnit == PortionUnit.Bundle ||
                             prod.PortionUnit == PortionUnit.Gallon;

            bool isBulkByName = name.Contains("كيس") || name.Contains("رز") || name.Contains("أرز") ||
                                name.Contains("ارز") || name.Contains("سكر") || name.Contains("طحين") ||
                                name.Contains("دقيق") || name.Contains("كرتون") || name.Contains("صندوق");

            if (isBagUnit || savedBagWeight > 0 || isBulkByName)
            {
                IsBagOrPackageProduct = true;
                if (savedBagWeight > 0)
                {
                    PackageWeightPerUnit = savedBagWeight;
                    // إذا كان السعر المخزن بالمستودع للكيلو الواحد، نعيد حساب سعر الكيس تلقائياً لتسهيل الإدخال
                    if (prod.PortionUnit == PortionUnit.Kg && prod.Price > 0)
                    {
                        Cost = Math.Round(prod.Price * savedBagWeight, 0);
                    }
                }
                else
                {
                    PackageWeightPerUnit = 40m;
                }
            }
            else
            {
                IsBagOrPackageProduct = false;
                PackageWeightPerUnit = null;
            }
        }

        private Warehouse? _selectedWarehouse;
        public Warehouse? SelectedWarehouse
        {
            get => _selectedWarehouse;
            set
            {
                if (SetProperty(ref _selectedWarehouse, value))
                {
                    if (_selectedWarehouse != null)
                    {
                        LoadProductsForWarehouse(_selectedWarehouse.Id);
                    }
                    else
                    {
                        Products.Clear();
                    }
                }
            }
        }

        private int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    UpdateSubTotal();
                    OnPropertyChanged(nameof(TotalPackageKg));
                    OnPropertyChanged(nameof(PackageConversionPreviewText));
                }
            }
        }

        private decimal _cost;
        public decimal Cost
        {
            get => _cost;
            set
            {
                if (SetProperty(ref _cost, value))
                {
                    UpdateSubTotal();
                }
            }
        }

        public ObservableCollection<WarehouseFromProduct> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        public ObservableCollection<Warehouse> Warehouses
        {
            get => _warehouses;
            set => SetProperty(ref _warehouses, value);
        }

        public ObservableCollection<PurchaseItem> PurchaseItems
        {
            get => _purchaseItems;
            set => SetProperty(ref _purchaseItems, value);
        }

        public ObservableCollection<Purchase> PurchaseReport
        {
            get => _purchaseReport;
            set => SetProperty(ref _purchaseReport, value);
        }

        public ObservableCollection<PurchaseReturn> PurchaseReturnReport
        {
            get => _purchaseReturnReport;
            set => SetProperty(ref _purchaseReturnReport, value);
        }

        public DateTime ReportStartDate
        {
            get => _reportStartDate;
            set => SetProperty(ref _reportStartDate, value);
        }

        public DateTime ReportEndDate
        {
            get => _reportEndDate;
            set => SetProperty(ref _reportEndDate, value);
        }

        public string? ReportSupplierName
        {
            get => _reportSupplierName;
            set => SetProperty(ref _reportSupplierName, value);
        }

        public DateTime PurchaseDate
        {
            get => _purchaseDate;
            set => SetProperty(ref _purchaseDate, value);
        }

        public Action? OnPaymentChannelChanged { get; set; }

        public string PaymentMethod
        {
            get => _paymentMethod;
            set
            {
                if (SetProperty(ref _paymentMethod, value))
                {
                    OnPropertyChanged(nameof(IsCreditPayment));
                    OnPropertyChanged(nameof(IsDepositPayment));
                    OnPropertyChanged(nameof(IsVoucherPayment));
                    OnPropertyChanged(nameof(ShowChannelBalanceBanner));
                    OnPaymentChannelChanged?.Invoke();
                }
            }
        }

        public decimal SubTotal
        {
            get => _subTotal;
            set => SetProperty(ref _subTotal, value);
        }

        public decimal TotalInvoiceAmount
        {
            get => _totalInvoiceAmount;
            set
            {
                if (SetProperty(ref _totalInvoiceAmount, value))
                {
                    OnPaymentChannelChanged?.Invoke();
                }
            }
        }

        public decimal TotalReportAmount
        {
            get => _totalReportAmount;
            set
            {
                if (SetProperty(ref _totalReportAmount, value))
                    OnPropertyChanged(nameof(NetReportAmount));
            }
        }

        public decimal TotalReturnReportAmount
        {
            get => _totalReturnReportAmount;
            set
            {
                if (SetProperty(ref _totalReturnReportAmount, value))
                    OnPropertyChanged(nameof(NetReportAmount));
            }
        }

        public decimal NetReportAmount => TotalReportAmount - TotalReturnReportAmount;

        public string SupplierName
        {
            get => _supplierName;
            set
            {
                if (SetProperty(ref _supplierName, value))
                {
                    if (!string.IsNullOrWhiteSpace(value) && AllSuppliers != null && AllSuppliers.Any())
                    {
                        var matched = AllSuppliers.FirstOrDefault(s => s.Name.Trim().Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (matched != null)
                        {
                            _selectedSupplierId = matched.Id;
                            SelectedSupplierBalance = matched.TotalBalance;
                            OnPropertyChanged(nameof(SelectedSupplierId));
                        }
                        else
                        {
                            _selectedSupplierId = null;
                            SelectedSupplierBalance = 0;
                            OnPropertyChanged(nameof(SelectedSupplierId));
                        }
                    }
                    else
                    {
                        _selectedSupplierId = null;
                        SelectedSupplierBalance = 0;
                        OnPropertyChanged(nameof(SelectedSupplierId));
                    }
                }
            }
        }

        public ObservableCollection<string> Suppliers
        {
            get => _suppliers;
            set => SetProperty(ref _suppliers, value);
        }

        public ObservableCollection<Supplier> AllSuppliers
        {
            get => _allSuppliers;
            set => SetProperty(ref _allSuppliers, value);
        }

        private int? _selectedSupplierId;
        public int? SelectedSupplierId
        {
            get => _selectedSupplierId;
            set
            {
                int? sanitized = (value.HasValue && value.Value > 0) ? value : null;
                if (SetProperty(ref _selectedSupplierId, sanitized))
                {
                    if (sanitized.HasValue)
                    {
                        var supplier = AllSuppliers.FirstOrDefault(s => s.Id == sanitized.Value);
                        if (supplier != null)
                        {
                            SupplierName = supplier.Name;
                            SelectedSupplierBalance = supplier.TotalBalance;
                        }
                    }
                    else
                    {
                        SelectedSupplierBalance = 0;
                    }
                }
            }
        }

        public decimal SelectedSupplierBalance
        {
            get => _selectedSupplierBalance;
            set => SetProperty(ref _selectedSupplierBalance, value);
        }

        public bool IsCreditPayment => PaymentMethod == "آجل (دين)";
        public bool IsDepositPayment => PaymentMethod == "إيداع / محفظة للمورد" || PaymentMethod == "سند صرف";
        public bool IsVoucherPayment => PaymentMethod == "سند صرف";

        public bool ShowChannelBalanceBanner => !IsCreditPayment;

        private decimal _selectedChannelBalance;
        public decimal SelectedChannelBalance
        {
            get => _selectedChannelBalance;
            set => SetProperty(ref _selectedChannelBalance, value);
        }

        private string _selectedChannelTitleText = "الرصيد المتاح في الصندوق الرئيسي:";
        public string SelectedChannelTitleText
        {
            get => _selectedChannelTitleText;
            set => SetProperty(ref _selectedChannelTitleText, value);
        }

        private string _selectedChannelBalanceText = "0.00 ريال";
        public string SelectedChannelBalanceText
        {
            get => _selectedChannelBalanceText;
            set => SetProperty(ref _selectedChannelBalanceText, value);
        }

        private bool _isChannelBalanceInsufficient;
        public bool IsChannelBalanceInsufficient
        {
            get => _isChannelBalanceInsufficient;
            set => SetProperty(ref _isChannelBalanceInsufficient, value);
        }

        public ObservableCollection<FinancialAccount> WalletBalancesList { get; } = new();

        private string _depositBankOrWallet = "حاسب كريمي";
        public string DepositBankOrWallet
        {
            get => _depositBankOrWallet;
            set
            {
                if (SetProperty(ref _depositBankOrWallet, value))
                {
                    OnPaymentChannelChanged?.Invoke();
                }
            }
        }

        private string _depositReferenceNumber = string.Empty;
        public string DepositReferenceNumber
        {
            get => _depositReferenceNumber;
            set => SetProperty(ref _depositReferenceNumber, value);
        }

        private string _depositCurrency = "ريال يمني";
        public string DepositCurrency
        {
            get => _depositCurrency;
            set => SetProperty(ref _depositCurrency, value);
        }

        private string _depositNotes = string.Empty;
        public string DepositNotes
        {
            get => _depositNotes;
            set => SetProperty(ref _depositNotes, value);
        }

        public ObservableCollection<string> DepositWalletsList { get; } = new(POSSystem.WPF.Helpers.FinancialChannelHelper.GetActiveWalletsList());

        public ObservableCollection<string> DepositCurrenciesList { get; } = new()
        {
            "ريال يمني",
            "ريال سعودي",
            "دولار أمريكي"
        };

        public ObservableCollection<Addon> Addons
        {
            get => _addons;
            set
            {
                if (SetProperty(ref _addons, value))
                {
                    _allAddons = value.ToList();
                    FilterAddons();
                }
            }
        }

        private void FilterAddons()
        {
        }

        private Addon? _selectedAddon;
        public Addon? SelectedAddon
        {
            get => _selectedAddon;
            set
            {
                if (SetProperty(ref _selectedAddon, value))
                {
                    if (_selectedAddon != null)
                    {
                        Cost = 0;
                        IsInternalProduction = _selectedAddon.IsInHouse;
                    }
                    UpdateSubTotal();
                }
            }
        }

        private bool _isAddonMode;
        public bool IsAddonMode
        {
            get => _isAddonMode;
            set
            {
                if (SetProperty(ref _isAddonMode, value))
                {
                    OnPropertyChanged(nameof(IsProductMode));
                    SelectedProduct = null;
                    SelectedAddon = null;
                    FilterAddons();
                }
            }
        }

        private bool _isInternalProduction;
        public bool IsInternalProduction
        {
            get => _isInternalProduction;
            set
            {
                if (SetProperty(ref _isInternalProduction, value))
                {
                    if (_isInternalProduction)
                    {
                        Cost = 0;
                    }
                    FilterAddons();
                }
            }
        }

        public bool IsProductMode
        {
            get => !IsAddonMode;
            set
            {
                if (IsAddonMode == value)
                {
                    IsAddonMode = !value;
                }
            }
        }

        internal void LoadProductsForWarehouse(int warehouseId)
        {
            try
            {
                var allWarehouseProducts = ServiceLocator.UnitOfWork.warehouseFromProduct.GetWarehouseFromProductsAllProducts().ToList();
                foreach (var item in allWarehouseProducts)
                {
                    if (item.Name != null && item.Name.Contains("لحم") &&
                        (item.Name.Contains("نفر") || item.Name.Contains("جزء") || item.Name.Contains("ثمن")) &&
                        (item.PortionUnit == PortionUnit.Eighth || item.PortionUnit == null))
                    {
                        item.PortionUnit = PortionUnit.Portion;
                    }
                }
                var filteredProducts = allWarehouseProducts.Where(w => w.WarehouseId == warehouseId).ToList();
                Products = new ObservableCollection<WarehouseFromProduct>(filteredProducts);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading warehouse products: {ex.Message}");
            }
        }

        private void UpdateSubTotal()
        {
            SubTotal = Quantity * Cost;
        }
    }
}
