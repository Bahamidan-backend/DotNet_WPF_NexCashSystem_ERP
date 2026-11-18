using POSSystem.WPF.Services;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.WPF.Data;
using System;
using System.Threading.Tasks;

namespace POSSystem.WPF.Business
{
    public static class ServiceLocator
    {
        public static event Action? InventoryChanged;
        public static void NotifyInventoryChanged() => InventoryChanged?.Invoke();

        public static event Action? SuppliersChanged;
        public static void NotifySuppliersChanged() => SuppliersChanged?.Invoke();

        public static event Action? AddonsChanged;
        public static void NotifyAddonsChanged() => AddonsChanged?.Invoke();

        public static event Action? ProductsChanged;
        public static void NotifyProductsChanged() => ProductsChanged?.Invoke();

        public static event Action? CategoriesChanged;
        public static void NotifyCategoriesChanged() => CategoriesChanged?.Invoke();

        public static event Action? DiscountsChanged;
        public static void NotifyDiscountsChanged() => DiscountsChanged?.Invoke();

        public static event Action? CashBoxChanged;
        public static void NotifyCashBoxChanged() => CashBoxChanged?.Invoke();

        public static event Action<POSSystem.WPF.Models.User>? UserPermissionsChanged;
        public static void NotifyUserPermissionsChanged(POSSystem.WPF.Models.User user) => UserPermissionsChanged?.Invoke(user);

        private static IServiceProvider? _serviceProvider;

        public static NavigationService NavigationService => NavigationService.Instance;

        public static void Initialize(IServiceProvider? provider = null)
        {
            if (provider != null)
            {
                _serviceProvider = provider;
            }
            else if (_serviceProvider == null)
            {
                _serviceProvider = App.ServiceProvider;
            }
        }

        public static async Task InitializeAsync(IServiceProvider? provider = null)
        {
            Initialize(provider);

            // No longer caching Context - get fresh scoped instance each time
            var initializer = new DatabaseInitializer(Context);
            await initializer.InitializeAsync();
        }

        // Pass-through properties - NO CACHING, respects Scoped lifetime
        public static POSDbContext Context => _serviceProvider!.GetRequiredService<POSDbContext>();
        public static IUnitOfWork UnitOfWork => _serviceProvider!.GetRequiredService<IUnitOfWork>();
        public static IProductService ProductService => _serviceProvider!.GetRequiredService<IProductService>();
        public static IDialogService DialogService => _serviceProvider!.GetRequiredService<IDialogService>();
        public static IOrderService OrderService => _serviceProvider!.GetRequiredService<IOrderService>();
        public static IUserService UserService => _serviceProvider!.GetRequiredService<IUserService>();
        public static ICustomerDebtService CustomerDebtService => _serviceProvider!.GetRequiredService<ICustomerDebtService>();
        public static IKitchenService KitchenService => _serviceProvider!.GetRequiredService<IKitchenService>();
        public static ISaleService SaleService => _serviceProvider!.GetRequiredService<ISaleService>();
        public static ICategoryService CategoryService => _serviceProvider!.GetRequiredService<ICategoryService>();
        public static IEmployeeService EmployeeService => _serviceProvider!.GetRequiredService<IEmployeeService>();
        public static IAddonService AddonService => _serviceProvider!.GetRequiredService<IAddonService>();
        public static IReturnOrderService ReturnOrderService => _serviceProvider!.GetRequiredService<IReturnOrderService>();
        public static ISupplierService SupplierService => _serviceProvider!.GetRequiredService<ISupplierService>();
        public static IPurchaseReturnService PurchaseReturnService => _serviceProvider!.GetRequiredService<IPurchaseReturnService>();
        public static IAnimalBatchService AnimalBatchService => _serviceProvider!.GetRequiredService<IAnimalBatchService>();
        public static IBankDepositService BankDepositService => _serviceProvider!.GetRequiredService<IBankDepositService>();
        public static IReportService ReportService => _serviceProvider!.GetRequiredService<IReportService>();
        public static IFinancialEngineService FinancialEngineService => _serviceProvider!.GetRequiredService<IFinancialEngineService>();
        public static IBackupService BackupService => _serviceProvider!.GetRequiredService<IBackupService>();
        public static IHeldOrderService HeldOrderService => _serviceProvider!.GetRequiredService<IHeldOrderService>();
        public static IStockMovementService StockMovementService => _serviceProvider!.GetRequiredService<IStockMovementService>();
        public static IStockWasteService StockWasteService => _serviceProvider!.GetRequiredService<IStockWasteService>();
        public static IShiftService ShiftService => _serviceProvider!.GetRequiredService<IShiftService>();
        public static ICashBoxTransactionService CashBoxTransactionService => _serviceProvider!.GetRequiredService<ICashBoxTransactionService>();
        public static IKitchenCookingBatchService KitchenCookingBatchService => _serviceProvider!.GetRequiredService<IKitchenCookingBatchService>();
        public static IFinancialChannelService FinancialChannelService => _serviceProvider!.GetRequiredService<IFinancialChannelService>();
        public static IFinancialAccountService FinancialAccountService => _serviceProvider!.GetRequiredService<IFinancialAccountService>();

        // Clear cached reference to ServiceProvider (for app shutdown)
        public static void Dispose()
        {
            _serviceProvider = null;
        }
    }
}
