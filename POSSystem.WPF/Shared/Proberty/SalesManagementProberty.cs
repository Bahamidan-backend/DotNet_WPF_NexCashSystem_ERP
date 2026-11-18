using System;
using System.Collections.ObjectModel;
using System.Linq;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Shered.Proberty
{
    public class SalesManagementProberty : BaseModel
    {
        private DateTime _reportStartDate = DateTime.Today;
        private DateTime _reportEndDate = DateTime.Today;
        private string _customerSearchText = string.Empty;
        private string _statusFilter = "الكل";
        private decimal _totalSalesAmount;
        private decimal _totalReturnAmount;
        private string _paymentMethodFilter = "الكل";
        private bool _hasWalletSummaries;
        private string _totalDepositSummaryText = string.Empty;
        private string _selectedProductCategoryFilter = "الكل (جميع الأقسام)";
        private string _productSalesSearchText = string.Empty;
        private CategorySalesSummaryItem? _selectedCategorySummaryItem;

        public ObservableCollection<Order> SalesLog { get; } = new();
        public ObservableCollection<Order> SalesReport { get; } = new();
        public ObservableCollection<ReturnOrder> SalesReturns { get; } = new();
        public ObservableCollection<WalletDepositSummary> WalletSummaries { get; } = new();
        public ObservableCollection<ProductSalesReportItem> ProductSalesBreakdown { get; } = new();
        public ObservableCollection<CategorySalesSummaryItem> CategorySalesBreakdown { get; } = new();
        public ObservableCollection<string> ProductCategoriesFilterList { get; } = new() { "الكل (جميع الأقسام)" };

        public Action? OnProductCategoryOrSearchChanged { get; set; }

        public string SelectedProductCategoryFilter
        {
            get => _selectedProductCategoryFilter;
            set
            {
                if (SetProperty(ref _selectedProductCategoryFilter, value))
                {
                    OnProductCategoryOrSearchChanged?.Invoke();
                }
            }
        }

        public string ProductSalesSearchText
        {
            get => _productSalesSearchText;
            set
            {
                if (SetProperty(ref _productSalesSearchText, value))
                {
                    OnProductCategoryOrSearchChanged?.Invoke();
                }
            }
        }

        public CategorySalesSummaryItem? SelectedCategorySummaryItem
        {
            get => _selectedCategorySummaryItem;
            set
            {
                if (SetProperty(ref _selectedCategorySummaryItem, value) && value != null && !string.IsNullOrWhiteSpace(value.CategoryName))
                {
                    SelectedProductCategoryFilter = value.CategoryName;
                }
            }
        }

        public int TotalSoldCategoriesCount => CategorySalesBreakdown?.Count ?? 0;
        public int TotalSoldProductsTypesCount => ProductSalesBreakdown?.Count ?? 0;
        public decimal TotalSoldPortionsSum => ProductSalesBreakdown?.Sum(x => x.TotalSoldQuantity) ?? 0m;
        public decimal TotalProductsSalesRevenue => ProductSalesBreakdown?.Sum(x => x.TotalRevenue) ?? 0m;

        public void NotifyProductSalesChanged()
        {
            OnPropertyChanged(nameof(CategorySalesBreakdown));
            OnPropertyChanged(nameof(ProductSalesBreakdown));
            OnPropertyChanged(nameof(TotalSoldCategoriesCount));
            OnPropertyChanged(nameof(TotalSoldProductsTypesCount));
            OnPropertyChanged(nameof(TotalSoldPortionsSum));
            OnPropertyChanged(nameof(TotalProductsSalesRevenue));
        }

        public Pagination<Order> SalesLogPager { get; } = new();
        public Pagination<Order> SalesReportPager { get; } = new();
        public Pagination<ReturnOrder> ReturnsPager { get; } = new();

        public ObservableCollection<string> StatusFilters { get; } = new()
        {
            "الكل", "مكتمل", "ملغي", "مرتجع", "آجل (دين)", "خصم"
        };

        public ObservableCollection<string> PaymentMethods { get; } = new()
        {
            "الكل", "نقدي (كاش)", "بطاقة (شبكة/فيزا)", "تحويل بنكي", "آجل (دين)", "إيداع عام (جميع المحافظ)",
            "حاسب كريمي", "محفظة جيب", "محفظة فلوسك", "محفظة ون كاش", "محفظة جوالي", "محفظة موبايل موني", "محفظة يمن والت"
        };

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

        public string CustomerSearchText
        {
            get => _customerSearchText;
            set => SetProperty(ref _customerSearchText, value);
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set => SetProperty(ref _statusFilter, value);
        }

        public string PaymentMethodFilter
        {
            get => _paymentMethodFilter;
            set => SetProperty(ref _paymentMethodFilter, value);
        }

        public bool HasWalletSummaries
        {
            get => _hasWalletSummaries;
            set => SetProperty(ref _hasWalletSummaries, value);
        }

        public string TotalDepositSummaryText
        {
            get => _totalDepositSummaryText;
            set => SetProperty(ref _totalDepositSummaryText, value);
        }

        public decimal TotalSalesAmount
        {
            get => _totalSalesAmount;
            set
            {
                if (SetProperty(ref _totalSalesAmount, value))
                    OnPropertyChanged(nameof(NetSalesAmount));
            }
        }

        public decimal TotalReturnAmount
        {
            get => _totalReturnAmount;
            set
            {
                if (SetProperty(ref _totalReturnAmount, value))
                    OnPropertyChanged(nameof(NetSalesAmount));
            }
        }

        public decimal NetSalesAmount => TotalSalesAmount - TotalReturnAmount;
    }
}
