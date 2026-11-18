using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.WPF.Business;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Data;
using POSSystem.WPF.Data.UnitOfWork;
using System;
using System.IO;
using System.Threading.Tasks;

namespace POSSystem.Tests.TestInfrastructure.DatabaseFixtures
{
    /// <summary>
    /// Isolated SQLite database fixture for unit, integration, concurrency, and backup/restore tests.
    /// Never touches production poss.db.
    /// </summary>
    public sealed class IsolatedSqliteTestDb : IDisposable, IAsyncDisposable
    {
        private readonly SqliteConnection? _inMemoryConnection;
        public string? TempDbFilePath { get; }
        public POSDbContext Context { get; }
        public IUnitOfWork UnitOfWork { get; }
        public ServiceProvider ServiceProvider { get; }

        public IsolatedSqliteTestDb(bool useTempFile = false)
        {
            DbContextOptions<POSDbContext> options;

            if (useTempFile)
            {
                var dir = Path.Combine(Path.GetTempPath(), "NexCash_Tests", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(dir);
                TempDbFilePath = Path.Combine(dir, "test_poss.db");
                options = new DbContextOptionsBuilder<POSDbContext>()
                    .UseSqlite($"Data Source={TempDbFilePath};Pooling=False;")
                    .Options;
            }
            else
            {
                _inMemoryConnection = new SqliteConnection("DataSource=:memory:");
                _inMemoryConnection.Open();
                options = new DbContextOptionsBuilder<POSDbContext>()
                    .UseSqlite(_inMemoryConnection)
                    .Options;
            }

            Context = new POSDbContext(options);
            Context.Database.EnsureCreated();
            Context.Database.ExecuteSqlRaw("PRAGMA foreign_keys=ON;");
            if (!System.Linq.Enumerable.Any(Context.Categories))
            {
                Context.Categories.Add(new POSSystem.WPF.Models.Category
                {
                    Id = 1,
                    Name = "عام",
                    Description = "تصنيف افتراضي للاختبارات",
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
                Context.SaveChanges();
            }

            UnitOfWork = new UnitOfWork(Context);

            var services = new ServiceCollection();
            services.AddSingleton(Context);
            services.AddSingleton(UnitOfWork);
            services.AddSingleton<ICashBoxTransactionService, CashBoxTransactionService>();
            services.AddSingleton<IFinancialChannelService, FinancialChannelService>();
            services.AddSingleton<IFinancialAccountService, FinancialAccountService>();
            services.AddSingleton<ICustomerDebtService, CustomerDebtService>();
            services.AddSingleton<IOrderService, OrderService>();
            services.AddSingleton<IReturnOrderService, ReturnOrderService>();
            services.AddSingleton<IEmployeeService, EmployeeService>();
            services.AddSingleton<ISupplierService, SupplierService>();
            services.AddSingleton<IExpenseService, ExpenseService>();
            services.AddSingleton<IPurchaseService, PurchaseService>();
            services.AddSingleton<IPurchaseReturnService, PurchaseReturnService>();
            services.AddSingleton<IWarehouseService, WarehouseService>();
            services.AddSingleton<IAnimalBatchService, AnimalBatchService>();
            services.AddSingleton<IShiftService, ShiftService>();
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<IFinancialEngineService, FinancialEngineService>();
            services.AddSingleton<IBackupService>(_ => new BackupService(TempDbFilePath));

            ServiceProvider = services.BuildServiceProvider();
            ServiceLocator.Initialize(ServiceProvider);
        }

        public POSDbContext CreateIndependentContext()
        {
            if (string.IsNullOrWhiteSpace(TempDbFilePath))
                throw new InvalidOperationException("Independent contexts require a file-backed test database.");

            var options = new DbContextOptionsBuilder<POSDbContext>()
                .UseSqlite($"Data Source={TempDbFilePath};Pooling=False;")
                .Options;
            var ctx = new POSDbContext(options);
            ctx.Database.ExecuteSqlRaw("PRAGMA foreign_keys=ON;");
            return ctx;
        }

        public void Dispose()
        {
            ServiceProvider.Dispose();
            Context.Dispose();
            _inMemoryConnection?.Dispose();

            if (!string.IsNullOrWhiteSpace(TempDbFilePath))
            {
                try
                {
                    var dir = Path.GetDirectoryName(TempDbFilePath);
                    if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                        Directory.Delete(dir, true);
                }
                catch { }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await ServiceProvider.DisposeAsync();
            await Context.DisposeAsync();
            if (_inMemoryConnection != null)
                await _inMemoryConnection.DisposeAsync();

            if (!string.IsNullOrWhiteSpace(TempDbFilePath))
            {
                try
                {
                    var dir = Path.GetDirectoryName(TempDbFilePath);
                    if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                        Directory.Delete(dir, true);
                }
                catch { }
            }
        }
    }
}
