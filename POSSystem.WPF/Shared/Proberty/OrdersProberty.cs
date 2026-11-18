using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSSystem.WPF.Shered.Proberty
{
    public class OrdersProberty:BaseModel
    {






        private readonly User _currentUser;

        private ObservableCollection<Order> _orders = new ObservableCollection<Order>();
        private ObservableCollection<Order> _filteredOrders = new ObservableCollection<Order>();
        private ObservableCollection<Order> _pagedFilteredOrders = new ObservableCollection<Order>();
        private int _pageSize = 8;
        private int _currentPage = 1;
        private string _searchText = string.Empty;
        private string _selectedDateFilter = "اليوم";
        private string _selectedStatusFilter = "الكافة";
        private string _selectedPaymentFilter = "الكافة";
        private DateTime? _activeShiftStart;

        public DateTime? ActiveShiftStart
        {
            get => _activeShiftStart;
            set
            {
                if (SetProperty(ref _activeShiftStart, value))
                {
                    FilterOrders();
                }
            }
        }

        // Statistics
        private int _totalOrdersToday = 0;
        private int _completedOrdersToday = 0;
        private int _preparingOrdersToday = 0;
        private decimal _totalSalesToday = 0;
        private string _orderType;
        private int _countProduct = 0;




        public OrdersProberty()
        {
            DateFilterOptions = new List<string> { "اليوم", "أمس", "هذا الأسبوع", "هذا الشهر", "مخصص" };
            StatusFilterOptions = new List<string> { "الكافة", "مكتمل", "قيد التحضير", "جاهز للتسليم", "قيد التوصيل", "ملغي", "مدفوع", "مرتجع", "مرتجع جزئي" };
            PaymentFilterOptions = new List<string> { "الكافة", "نقدي", "بطاقة", "محفظة" };

            // Pagination defaults
            PageSizeOptions = new[] { 5, 8, 12, 20 };
            PageSize = _pageSize;
            CurrentPage = _currentPage;

            PrevPageCommand = new RelayCommand(() =>
            {
                if (HasPreviousPage)
                {
                    CurrentPage--;
                    UpdatePagedOrders();
                }
            }, () => HasPreviousPage);

            NextPageCommand = new RelayCommand(() =>
            {
                if (HasNextPage)
                {
                    CurrentPage++;
                    UpdatePagedOrders();
                }
            }, () => HasNextPage);

            FirstPageCommand = new RelayCommand(() =>
            {
                if (CurrentPage != 1)
                {
                    CurrentPage = 1;
                    UpdatePagedOrders();
                }
            }, () => CurrentPage != 1);

            LastPageCommand = new RelayCommand(() =>
            {
                if (CurrentPage != TotalPages)
                {
                    CurrentPage = TotalPages;
                    UpdatePagedOrders();
                }
            }, () => CurrentPage != TotalPages);
        }


        public ObservableCollection<Order> Orders
        {
            get => _orders;
            set => SetProperty(ref _orders, value);
        }

        public ObservableCollection<Order> FilteredOrders
        {
            get => _filteredOrders;
            set => SetProperty(ref _filteredOrders, value);
        }

        public ObservableCollection<Order> PagedFilteredOrders
        {
            get => _pagedFilteredOrders;
            set => SetProperty(ref _pagedFilteredOrders, value);
        }

        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (SetProperty(ref _pageSize, value))
                {
                    CurrentPage = 1;
                    UpdatePagedOrders();
                }
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    UpdatePagedOrders();
                    OnPropertyChanged(nameof(HasPreviousPage));
                    OnPropertyChanged(nameof(HasNextPage));
                    OnPropertyChanged(nameof(TotalPages));
                    OnPropertyChanged(nameof(PageInfo));
                }
            }
        }

        public int TotalPages => FilteredOrders.Count == 0 ? 1 : (int)Math.Ceiling((double)FilteredOrders.Count / PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public IEnumerable<int> PageSizeOptions { get; private set; }
        public string PageInfo => $"الصفحة {CurrentPage} من {TotalPages}";

        public ICommand PrevPageCommand { get; private set; }
        public ICommand NextPageCommand { get; private set; }
        public ICommand FirstPageCommand { get; private set; }
        public ICommand LastPageCommand { get; private set; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    FilterOrders();
                }
            }
        }

        public string SelectedDateFilter
        {
            get => _selectedDateFilter;
            set
            {
                if (_selectedDateFilter != value)
                {
                    _selectedDateFilter = value;
                    OnPropertyChanged(nameof(SelectedDateFilter));
                    FilterOrders();
                }
            }
        }

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (_selectedStatusFilter != value)
                {
                    _selectedStatusFilter = value;
                    OnPropertyChanged(nameof(SelectedStatusFilter));
                    FilterOrders();
                }
            }
        }

        public string SelectedPaymentFilter
        {
            get => _selectedPaymentFilter;
            set
            {
                if (_selectedPaymentFilter != value)
                {
                    _selectedPaymentFilter = value;
                    OnPropertyChanged(nameof(SelectedPaymentFilter));
                    FilterOrders();
                }
            }
        }

        public List<string> DateFilterOptions { get; }
        public List<string> StatusFilterOptions { get; }
        public List<string> PaymentFilterOptions { get; }

        // Statistics Properties
        public int TotalOrdersToday
        {
            get => _totalOrdersToday;
            set => SetProperty(ref _totalOrdersToday, value);
        }

        public int CompletedOrdersToday
        {
            get => _completedOrdersToday;
            set => SetProperty(ref _completedOrdersToday, value);
        }

        public int PreparingOrdersToday
        {
            get => _preparingOrdersToday;
            set => SetProperty(ref _preparingOrdersToday, value);
        }

        public decimal TotalSalesToday
        {
            get => _totalSalesToday;
            set => SetProperty(ref _totalSalesToday, value);
        }

        public bool HasOrders => FilteredOrders.Any();

        public string OrderType
        {
            get => _orderType;
            set
            {
                if (_orderType != value)
                {
                    _orderType = value;
                    OnPropertyChanged(nameof(_orderType));
                }
            }
        }


        public int CountProduct
        {
            get => _countProduct;
            set
            {
               
                    _countProduct = value;
                    OnPropertyChanged(nameof(CountProduct));
               
            }
        }

        public void FilterOrders()
        {
            var filtered = Orders.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(o =>
                    o.OrderNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    o.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true);
            }

            // Apply date filter
            var today = DateTime.Today;
            filtered = SelectedDateFilter switch
            {
                "اليوم" => filtered.Where(o => o.CreatedAt.Date == today || (ActiveShiftStart != null && o.CreatedAt >= ActiveShiftStart)),
                "أمس" => filtered.Where(o => o.CreatedAt.Date == today.AddDays(-1)),
                "هذا الأسبوع" => filtered.Where(o => o.CreatedAt >= today.AddDays(-7)),
                "هذا الشهر" => filtered.Where(o => o.CreatedAt >= today.AddDays(-30)),
                _ => filtered
            };

            // Apply status filter
            if (SelectedStatusFilter != "الكافة")
            {
                var status = SelectedStatusFilter switch
                {
                    "مكتمل" => OrderStatus.Completed,
                    "قيد التحضير" => OrderStatus.Preparing,
                    "جاهز للتسليم" => OrderStatus.Ready,
                    "قيد التوصيل" => OrderStatus.Served,
                    "ملغي" => OrderStatus.Cancelled,
                    "مدفوع" => OrderStatus.Pending,
                    "مرتجع" => OrderStatus.Returned,
                    "مرتجع جزئي" => OrderStatus.PartiallyReturned,
                    _ => (OrderStatus?)null
                };

                if (status.HasValue)
                {
                    filtered = filtered.Where(o => o.Status == status.Value);
                }
            }

            // Apply payment method filter (this would need to be added to Order model)
            // For now, we'll skip this filter

            // Sort by creation date (newest first)
            filtered = filtered.OrderByDescending(o => o.CreatedAt);

            FilteredOrders.Clear();
            foreach (var order in filtered)
            {
                FilteredOrders.Add(order);
            }

            // Update paging
            CurrentPage = 1;
            UpdatePagedOrders();
        }

        private void UpdatePagedOrders()
        {
            if (PagedFilteredOrders == null) PagedFilteredOrders = new ObservableCollection<Order>();
            PagedFilteredOrders.Clear();

            if (FilteredOrders == null || FilteredOrders.Count == 0)
            {
                OnPropertyChanged(nameof(PagedFilteredOrders));
                OnPropertyChanged(nameof(PageInfo));
                OnPropertyChanged(nameof(TotalPages));
                CommandManager.InvalidateRequerySuggested();
                return;
            }

            var skip = (CurrentPage - 1) * PageSize;
            var pageItems = FilteredOrders.Skip(skip).Take(PageSize).ToList();
            foreach (var item in pageItems) PagedFilteredOrders.Add(item);

            OnPropertyChanged(nameof(PagedFilteredOrders));
            OnPropertyChanged(nameof(PageInfo));
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(HasPreviousPage));
            OnPropertyChanged(nameof(HasNextPage));

            CommandManager.InvalidateRequerySuggested();
        }




    }
}
