using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.Tests.TestInfrastructure.DatabaseFixtures;
using POSSystem.Tests.TestInfrastructure.TestDataBuilders;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Models;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace POSSystem.Tests.PerformanceTests.Database
{
    [MemoryDiagnoser]
    public class CashBoxAndFinancialBenchmarks
    {
        private IsolatedSqliteTestDb? _db;
        private int _shiftId;

        [GlobalSetup]
        public void Setup()
        {
            _db = new IsolatedSqliteTestDb();
            var user = FinancialEntityBuilders.BuildUser();
            _db.Context.Users.Add(user);
            _db.Context.SaveChanges();

            var shift = FinancialEntityBuilders.BuildActiveShift(user.Id, 100_000m);
            _db.Context.Shifts.Add(shift);
            _db.Context.SaveChanges();
            _shiftId = shift.Id;

            for (int i = 0; i < 1000; i++)
            {
                _db.Context.CashBoxTransactions.Add(new CashBoxTransaction
                {
                    ShiftId = _shiftId,
                    UserId = user.Id,
                    Type = i % 3 == 0 ? CashBoxTransactionType.Expense : CashBoxTransactionType.Income,
                    Amount = 100m + i,
                    Name = $"TX-{i}",
                    TransactionDate = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
            }
            _db.Context.SaveChanges();
        }

        [Benchmark]
        public async Task<decimal> GetCashBoxBalance_1000Transactions()
        {
            var svc = _db!.ServiceProvider.GetRequiredService<ICashBoxTransactionService>();
            return await svc.GetCashBoxBalanceAsync(_shiftId);
        }

        [GlobalCleanup]
        public void Cleanup() => _db?.Dispose();
    }

    public class DatabaseScaleTests
    {
        [Theory]
        [InlineData(100)]
        [InlineData(1000)]
        public async Task ProductSearchAndFinancialReport_ScaleTest(int productCount)
        {
            using var db = new IsolatedSqliteTestDb();
            var user = FinancialEntityBuilders.BuildUser();
            db.Context.Users.Add(user);

            for (int i = 0; i < productCount; i++)
            {
                db.Context.Products.Add(FinancialEntityBuilders.BuildProduct($"منتج-{i}", 1000m + i, 500m, 50m));
            }
            await db.Context.SaveChangesAsync();

            var sw = Stopwatch.StartNew();
            var engine = db.ServiceProvider.GetRequiredService<IFinancialEngineService>();
            var report = await engine.GenerateFinancialStatementAsync(DateTime.Today.AddDays(-30), DateTime.Now);
            sw.Stop();

            Assert.NotNull(report);
        }
    }
}
