using System.Collections.Generic;
using System.Collections.ObjectModel;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Models;
using POSSystem.WPF.Services;
using POSSystem.WPF.ViewModels.Orders;
using POSSystem.WPF.Shered.Proberty;
using POSSystem.WPF.Helpers;

namespace POSSystem.WPF.ViewModels.Cashier
{
    public partial class CashierViewModel
    {
        private readonly ICategoryService _categoryService;
        private readonly IProductService _productService;
        private readonly IOrderService _orderService;
        private readonly IShiftService _shiftService;
        private readonly IEmployeeService _employeeService;
        private readonly IAddonService _addonService;
        private readonly NavigationService _navigationService;
        private readonly User _currentUser;
        private readonly OrdersViewModel _ordersViewModel;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDiscountsServices _discountsServices;
        private readonly IReturnOrderService _returnOrderService;
        private readonly IHeldOrderService _heldOrderService;
        private readonly ObservableCollection<Category> _categories;
        private Dictionary<string, bool> _selectedCategories;
        private ObservableCollection<Product> _filteredProducts;
        private List<Product> _allProducts;
        private Shift? _currentShift;
        private bool _hasActiveShift;
        private string _searchText = "";
        private Category _addonDummyCategory;
        private ObservableCollection<HeldOrder> _heldOrders = new ObservableCollection<HeldOrder>();
        private SettingsProberty _settingsProberty;

        public CashierProberty cashierProberty { get; }
        
        public SettingsProberty settingsProberty
        {
            get => _settingsProberty;
            set
            {
                if (_settingsProberty != null) _settingsProberty.PropertyChanged -= Settings_PropertyChanged;
                _settingsProberty = value;
                if (_settingsProberty != null) _settingsProberty.PropertyChanged += Settings_PropertyChanged;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Product> Products
        {
            get => _filteredProducts;
            set { if (_filteredProducts != value) { _filteredProducts = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<Category> Categories => _categories;

        public bool HasActiveShift
        {
            get => _hasActiveShift;
            private set
            {
                if (_hasActiveShift != value)
                {
                    _hasActiveShift = value;
                    OnPropertyChanged(nameof(HasActiveShift));
                    OnPropertyChanged(nameof(ShiftButtonText));
                    OnPropertyChanged(nameof(ShiftButtonColor));
                }
            }
        }

        public string ShiftButtonText => HasActiveShift ? "🕐 المناوبة نشطة" : "🕐 المناوبة";
        public string ShiftButtonColor => HasActiveShift ? "#01922a" : "#6c757d";
        public string WelcomeMessage => $"مرحباً، {_currentUser.Name}";
        public User CurrentUser => _currentUser;
        public bool CanAccessAdminPanel => PermissionGuard.CanEnterAdminPanelFromCashier(_currentUser);
        public bool CanEditCartItemPrice => PermissionGuard.HasPermission(_currentUser, SystemPermissions.SalesEditItemPrice) || PermissionGuard.HasPermission(_currentUser, SystemPermissions.ProductsChangePrice);
        public bool IsCartPriceReadOnly => !CanEditCartItemPrice;
        public string RoleBadgeDisplayText => !string.IsNullOrWhiteSpace(_currentUser.RoleTemplateName)
            ? _currentUser.RoleTemplateName
            : (_currentUser.Role == UserRole.Admin ? "مدير النظام" : "كاشير");

        private readonly POSSystem.WPF.Helpers.Debouncer _searchDebouncer = new POSSystem.WPF.Helpers.Debouncer(100);

        public string SearchText
        {
            get => _searchText;
            set 
            { 
                if (_searchText != value) 
                { 
                    _searchText = value; 
                    OnPropertyChanged(); 
                    _searchDebouncer.RunActionAsync(() => PerformSearch()).SafeFireAndForget(); 
                } 
            }
        }

        public ObservableCollection<HeldOrder> HeldOrders
        {
            get => _heldOrders;
            set { if (_heldOrders != value) { _heldOrders = value; OnPropertyChanged(); } }
        }
    }
}
