namespace POSSystem.WPF.Data.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IProductRepository Products { get; }
        IOrderRepository Orders { get; }
        IOrderItemRepository OrdersItem { get; }
        IUserRepository Users { get; }
        ISaleRepository Sales { get; }
        ICategoryRepository Categories { get; }
        IAddonRepository Addons { get; }
        IKitchenRepository Kitchens { get; }
        IWarehouseRepository Warehouses { get; }
        IDiscountRepositories Discounts {  get; }
        IAddonAndOrderItemRepository AddonsOrderItem {  get; }
        IWarehouseFromProductRepostry  warehouseFromProduct {  get; }
        IRecipesRepostries Recipes {  get; }
        IEmployeeAdvanceRepository EmployeeAdvances { get; }
        IEmployeeSalaryRepository EmployeeSalaries { get; }
        ICustomerDebtRepository CustomerDebts { get; }
        ICustomerRepository Customers { get; }
        IExpenseRepository Expenses { get; }
        IReceiptVoucherRepository ReceiptVouchers { get; }
        IPurchaseRepository Purchases { get; }
        IPurchaseItemRepository PurchaseItems { get; }
        IShiftRepository Shifts { get; }
        ICashBoxTransactionRepository CashBoxTransactions { get; }
        IDeliveryRepository Deliveries { get; }
        IReturnOrderRepository ReturnOrders { get; }
        ISupplierRepository Suppliers { get; }
        ISupplierPaymentRepository SupplierPayments { get; }
        IPurchaseReturnRepository PurchaseReturns { get; }
        IPurchaseReturnItemRepository PurchaseReturnItems { get; }
        IAnimalBatchRepository AnimalBatches { get; }
        IRepository<BankDeposit> BankDeposits { get; }
        IRepository<HeldOrder> HeldOrders { get; }
        IRepository<StockMovement> StockMovements { get; }
        IRepository<StockWaste> StockWastes { get; }
        IRepository<KitchenCookingBatch> KitchenCookingBatches { get; }
        IRepository<KitchenCookingBatchItem> KitchenCookingBatchItems { get; }
        IRepository<FinancialAccount> FinancialAccounts { get; }
        IRepository<FinancialAccountTransaction> FinancialAccountTransactions { get; }
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task ExecuteInTransactionAsync(Func<Task> action);
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action);
        bool HasActiveTransaction { get; }
        void ClearEntityTracking();
        Task ExecuteWithConcurrencyRetryAsync(Func<Task> action, int maxRetries = 3);
        POSDbContext GetContext();
    }
}
