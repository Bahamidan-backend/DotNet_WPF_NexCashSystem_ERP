using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Data.Repositories;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Data.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly POSDbContext _context;
        private IDbContextTransaction? _transaction;
        private bool _disposed = false;

        // FIX ISSUE-DATA-001 & ISSUE-ARCH-001: Serialize write transactions to prevent concurrent DbContext access and SQLite DEFERRED lock races
        private static readonly SemaphoreSlim _writeTransactionGate = new(1, 1);
        private static readonly AsyncLocal<bool> _holdsWriteGate = new();

        private IProductRepository? _products;
        private IOrderRepository? _orders;
        private IUserRepository? _users;
        private ISaleRepository? _sales;
        private IOrderItemRepository? _ordersItem;
        private ICategoryRepository? _categories;
        private IAddonRepository? _addons;
        private IKitchenRepository? _kitchens;
        private IWarehouseRepository? _warehouses;
        private IDiscountRepositories? _discount;
        private IAddonAndOrderItemRepository? _addonsOrderItem;
        private IWarehouseFromProductRepostry? _warehouseFromProduct;
        private IRecipesRepostries? _recipes;
        private IEmployeeAdvanceRepository? _employeeAdvances;
        private IEmployeeSalaryRepository? _employeeSalaries;
        private ICustomerDebtRepository? _customerDebts;
        private ICustomerRepository? _customers;
        private IExpenseRepository? _expenses;
        private IReceiptVoucherRepository? _receiptVouchers;
        private IPurchaseRepository? _purchases;
        private IPurchaseItemRepository? _purchaseItems;
        private IShiftRepository? _shifts;
        private ICashBoxTransactionRepository? _cashBoxTransactions;
        private IDeliveryRepository? _deliveries;
        private IReturnOrderRepository? _returnOrders;
        private ISupplierRepository? _suppliers;
        private ISupplierPaymentRepository? _supplierPayments;
        private IPurchaseReturnRepository? _purchaseReturns;
        private IPurchaseReturnItemRepository? _purchaseReturnItems;
        private IAnimalBatchRepository? _animalBatches;
        private IRepository<BankDeposit>? _bankDeposits;
        private IRepository<HeldOrder>? _heldOrders;
        private IRepository<StockMovement>? _stockMovements;
        private IRepository<StockWaste>? _stockWastes;
        private IRepository<KitchenCookingBatch>? _kitchenCookingBatches;
        private IRepository<KitchenCookingBatchItem>? _kitchenCookingBatchItems;
        private IRepository<FinancialAccount>? _financialAccounts;
        private IRepository<FinancialAccountTransaction>? _financialAccountTransactions;

        public UnitOfWork(POSDbContext context)
        {
            _context = context;
        }

        public bool HasActiveTransaction => _transaction != null;

        public IProductRepository Products => _products ??= new ProductRepository(_context);
        public IOrderRepository Orders => _orders ??= new OrderRepository(_context);
        public IOrderItemRepository OrdersItem => _ordersItem ??= new OrderItemRepository(_context);
        public IUserRepository Users => _users ??= new UserRepository(_context);
        public ISaleRepository Sales => _sales ??= new SaleRepository(_context);
        public ICategoryRepository Categories => _categories ??= new CategoryRepository(_context);
        public IAddonRepository Addons => _addons ??= new AddonRepository(_context);
        public IKitchenRepository Kitchens => _kitchens ??= new KitchenRepository(_context);
        public IWarehouseRepository Warehouses => _warehouses ??= new WarehouseRepository(_context);
        public IDiscountRepositories Discounts => _discount ??= new DiscountRepositories(_context);
        public IAddonAndOrderItemRepository AddonsOrderItem => _addonsOrderItem ??= new AddonAndOrderItemRepository(_context);
        public IWarehouseFromProductRepostry warehouseFromProduct => _warehouseFromProduct ??= new WarehouseFromProductRepostry(_context);
        public IRecipesRepostries Recipes => _recipes ??= new RecipesRepostries(_context);
        public IEmployeeAdvanceRepository EmployeeAdvances => _employeeAdvances ??= new EmployeeAdvanceRepository(_context);
        public IEmployeeSalaryRepository EmployeeSalaries => _employeeSalaries ??= new EmployeeSalaryRepository(_context);
        public ICustomerDebtRepository CustomerDebts => _customerDebts ??= new CustomerDebtRepository(_context);
        public ICustomerRepository Customers => _customers ??= new CustomerRepository(_context);
        public IExpenseRepository Expenses => _expenses ??= new ExpenseRepository(_context);
        public IReceiptVoucherRepository ReceiptVouchers => _receiptVouchers ??= new ReceiptVoucherRepository(_context);
        public IPurchaseRepository Purchases => _purchases ??= new PurchaseRepository(_context);
        public IPurchaseItemRepository PurchaseItems => _purchaseItems ??= new PurchaseItemRepository(_context);
        public IShiftRepository Shifts => _shifts ??= new ShiftRepository(_context);
        public ICashBoxTransactionRepository CashBoxTransactions => _cashBoxTransactions ??= new CashBoxTransactionRepository(_context);
        public IDeliveryRepository Deliveries => _deliveries ??= new DeliveryRepository(_context);
        public IReturnOrderRepository ReturnOrders => _returnOrders ??= new ReturnOrderRepository(_context);
        public ISupplierRepository Suppliers => _suppliers ??= new SupplierRepository(_context);
        public ISupplierPaymentRepository SupplierPayments => _supplierPayments ??= new SupplierPaymentRepository(_context);
        public IPurchaseReturnRepository PurchaseReturns => _purchaseReturns ??= new PurchaseReturnRepository(_context);
        public IPurchaseReturnItemRepository PurchaseReturnItems => _purchaseReturnItems ??= new PurchaseReturnItemRepository(_context);
        public IAnimalBatchRepository AnimalBatches => _animalBatches ??= new AnimalBatchRepository(_context);
        public IRepository<BankDeposit> BankDeposits => _bankDeposits ??= new Repository<BankDeposit>(_context);
        public IRepository<HeldOrder> HeldOrders => _heldOrders ??= new Repository<HeldOrder>(_context);
        public IRepository<StockMovement> StockMovements => _stockMovements ??= new Repository<StockMovement>(_context);
        public IRepository<StockWaste> StockWastes => _stockWastes ??= new Repository<StockWaste>(_context);
        public IRepository<KitchenCookingBatch> KitchenCookingBatches => _kitchenCookingBatches ??= new Repository<KitchenCookingBatch>(_context);
        public IRepository<KitchenCookingBatchItem> KitchenCookingBatchItems => _kitchenCookingBatchItems ??= new Repository<KitchenCookingBatchItem>(_context);
        public IRepository<FinancialAccount> FinancialAccounts => _financialAccounts ??= new Repository<FinancialAccount>(_context);
        public IRepository<FinancialAccountTransaction> FinancialAccountTransactions => _financialAccountTransactions ??= new Repository<FinancialAccountTransaction>(_context);

        public async Task<int> SaveChangesAsync()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(UnitOfWork));
            int affected = await _context.SaveChangesAsync();
            FinancialChannelService.InvalidateCache();
            return affected;
        }

        public async Task BeginTransactionAsync()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(UnitOfWork));
            if (_transaction != null)
                throw new InvalidOperationException("Transaction already started");

            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(UnitOfWork));
            if (_transaction == null)
                throw new InvalidOperationException("No transaction to commit");

            try
            {
                await _context.SaveChangesAsync();
                await _transaction.CommitAsync();
                FinancialChannelService.InvalidateCache();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(UnitOfWork));
            if (_transaction == null)
                throw new InvalidOperationException("No transaction to rollback");

            try
            {
                await _transaction.RollbackAsync();
                FinancialChannelService.InvalidateCache();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task ExecuteInTransactionAsync(Func<Task> action)
        {
            bool acquiredGate = false;
            if (!_holdsWriteGate.Value)
            {
                await _writeTransactionGate.WaitAsync();
                _holdsWriteGate.Value = true;
                acquiredGate = true;
            }

            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    bool startedHere = false;
                    if (!HasActiveTransaction)
                    {
                        await BeginTransactionAsync();
                        startedHere = true;
                    }

                    try
                    {
                        await action();
                        if (startedHere && HasActiveTransaction)
                        {
                            await CommitTransactionAsync();
                        }
                    }
                    catch
                    {
                        if (startedHere && HasActiveTransaction)
                        {
                            await RollbackTransactionAsync();
                        }
                        throw;
                    }
                });
            }
            finally
            {
                if (acquiredGate)
                {
                    _holdsWriteGate.Value = false;
                    _writeTransactionGate.Release();
                }
            }
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action)
        {
            bool acquiredGate = false;
            if (!_holdsWriteGate.Value)
            {
                await _writeTransactionGate.WaitAsync();
                _holdsWriteGate.Value = true;
                acquiredGate = true;
            }

            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    bool startedHere = false;
                    if (!HasActiveTransaction)
                    {
                        await BeginTransactionAsync();
                        startedHere = true;
                    }

                    try
                    {
                        var result = await action();
                        if (startedHere && HasActiveTransaction)
                        {
                            await CommitTransactionAsync();
                        }
                        return result;
                    }
                    catch
                    {
                        if (startedHere && HasActiveTransaction)
                        {
                            await RollbackTransactionAsync();
                        }
                        throw;
                    }
                });
            }
            finally
            {
                if (acquiredGate)
                {
                    _holdsWriteGate.Value = false;
                    _writeTransactionGate.Release();
                }
            }
        }

        public void ClearEntityTracking()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(UnitOfWork));

            var entries = _context.ChangeTracker.Entries()
                .Where(e => e.State != EntityState.Detached)
                .ToList();

            foreach (var entry in entries)
            {
                entry.State = EntityState.Detached;
            }
        }

        public async Task ExecuteWithConcurrencyRetryAsync(Func<Task> action, int maxRetries = 3)
        {
            bool acquiredGate = false;
            if (!_holdsWriteGate.Value)
            {
                await _writeTransactionGate.WaitAsync();
                _holdsWriteGate.Value = true;
                acquiredGate = true;
            }

            try
            {
                int retryCount = 0;
                while (true)
                {
                    try
                    {
                        await action();
                        FinancialChannelService.InvalidateCache();
                        return;
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        retryCount++;
                        if (retryCount > maxRetries)
                        {
                            throw;
                        }

                        foreach (var entry in ex.Entries)
                        {
                            await entry.ReloadAsync();
                        }

                        await Task.Delay(100 * retryCount);
                    }
                }
            }
            finally
            {
                if (acquiredGate)
                {
                    _holdsWriteGate.Value = false;
                    _writeTransactionGate.Release();
                }
            }
        }

        public POSDbContext GetContext()
        {
            return _context;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transaction?.Dispose();
                    _context?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
