using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.Tests.TestInfrastructure.Assertions;
using POSSystem.Tests.TestInfrastructure.DatabaseFixtures;
using POSSystem.Tests.TestInfrastructure.Helpers;
using POSSystem.Tests.TestInfrastructure.TestDataBuilders;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace POSSystem.Tests.UnitTests.CashBox
{
    public class CashBoxTests
    {
        [Fact]
        public async Task CashBox_MandatoryBalanceScenario_ShouldMatchIndependentOracle()
        {
            using var db = new IsolatedSqliteTestDb();
            var user = FinancialEntityBuilders.BuildUser();
            db.Context.Users.Add(user);
            await db.Context.SaveChangesAsync();

            var shift = FinancialEntityBuilders.BuildActiveShift(user.Id, startingCash: 100_000m);
            db.Context.Shifts.Add(shift);
            await db.Context.SaveChangesAsync();

            var cashBoxService = db.ServiceProvider.GetRequiredService<ICashBoxTransactionService>();

            await cashBoxService.CreateTransactionAsync(new CashBoxTransaction
            {
                ShiftId = shift.Id,
                UserId = user.Id,
                Type = CashBoxTransactionType.Income,
                Amount = 20_000m,
                Name = "إيداع نقدي",
                Description = "إيداع"
            });

            await cashBoxService.CreateTransactionAsync(new CashBoxTransaction
            {
                ShiftId = shift.Id,
                UserId = user.Id,
                Type = CashBoxTransactionType.BankDeposit,
                Amount = 10_000m,
                Name = "سحب نقدي",
                Description = "سحب"
            });

            await cashBoxService.CreateTransactionAsync(new CashBoxTransaction
            {
                ShiftId = shift.Id,
                UserId = user.Id,
                Type = CashBoxTransactionType.Expense,
                Amount = 5_000m,
                Name = "مصروف تشغيل",
                Description = "مصروف"
            });

            var actualBalance = await cashBoxService.GetCashBoxBalanceAsync(shift.Id);
            var expectedBalance = AccountingReferenceEngine.ComputeExpectedCashBoxBalance(
                100_000m,
                new[]
                {
                    (true, 20_000m, false),
                    (false, 10_000m, false),
                    (false, 5_000m, false)
                });

            expectedBalance.Should().Be(105_000m);
            actualBalance.ShouldMatchAmount(expectedBalance, "100,000 + 20,000 - 10,000 - 5,000 = 105,000");
        }

        [Fact]
        public async Task GetCashBoxBalanceAsync_CalledMultipleTimes_MustBePureReadAndNeverDeleteTransactions()
        {
            using var db = new IsolatedSqliteTestDb();
            var user = FinancialEntityBuilders.BuildUser();
            db.Context.Users.Add(user);
            await db.Context.SaveChangesAsync();

            var shift = FinancialEntityBuilders.BuildActiveShift(user.Id, 50_000m);
            db.Context.Shifts.Add(shift);
            await db.Context.SaveChangesAsync();

            var cashBoxService = db.ServiceProvider.GetRequiredService<ICashBoxTransactionService>();
            await cashBoxService.CreateTransactionAsync(new CashBoxTransaction
            {
                ShiftId = shift.Id,
                UserId = user.Id,
                Type = CashBoxTransactionType.Income,
                Amount = 12_500m,
                Name = "مبيعات",
                Description = "طلب #1"
            });
            await cashBoxService.CreateTransactionAsync(new CashBoxTransaction
            {
                ShiftId = shift.Id,
                UserId = user.Id,
                Type = CashBoxTransactionType.Expense,
                Amount = 2_500m,
                Name = "مصروف",
                Description = "قرطاسية"
            });

            var beforeSnapshot = await db.Context.CashBoxTransactions.AsNoTracking().OrderBy(t => t.Id).ToListAsync();

            var b1 = await cashBoxService.GetCashBoxBalanceAsync(shift.Id);
            var b2 = await cashBoxService.GetCashBoxBalanceAsync(shift.Id);
            var b3 = await cashBoxService.GetCashBoxBalanceAsync(shift.Id);

            var afterSnapshot = await db.Context.CashBoxTransactions.AsNoTracking().OrderBy(t => t.Id).ToListAsync();

            b1.Should().Be(60_000m);
            b2.Should().Be(b1);
            b3.Should().Be(b1);
            afterSnapshot.Should().HaveCount(beforeSnapshot.Count);
            afterSnapshot.Select(x => (x.Id, x.Amount, x.Type)).Should().Equal(beforeSnapshot.Select(x => (x.Id, x.Amount, x.Type)));
        }

        [Fact]
        public async Task CleanupOrphanedCashBoxTransactionsAsync_ShouldRemoveOrphanedAdvanceButKeepValidRepayments()
        {
            using var db = new IsolatedSqliteTestDb();
            var user = FinancialEntityBuilders.BuildUser();
            db.Context.Users.Add(user);
            await db.Context.SaveChangesAsync();

            var shift = FinancialEntityBuilders.BuildActiveShift(user.Id, 50_000m);
            db.Context.Shifts.Add(shift);
            await db.Context.SaveChangesAsync();

            // Valid income repayment
            db.Context.CashBoxTransactions.Add(new CashBoxTransaction
            {
                ShiftId = shift.Id,
                UserId = user.Id,
                Type = CashBoxTransactionType.Income,
                Amount = 5_000m,
                Name = "سداد سلفة",
                Description = "سداد سلفة نقدي",
                TransactionDate = DateTime.Now,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });

            // Orphaned advance without matching EmployeeAdvance or ReceiptVoucher
            db.Context.CashBoxTransactions.Add(new CashBoxTransaction
            {
                ShiftId = shift.Id,
                UserId = user.Id,
                Type = CashBoxTransactionType.Advance,
                Amount = 9_999m,
                Name = "سلفة يتيمة",
                Description = "سلفة يتيمة",
                Notes = "[سلفة: 999999]",
                TransactionDate = DateTime.Now,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
            await db.Context.SaveChangesAsync();

            var cashBoxService = db.ServiceProvider.GetRequiredService<ICashBoxTransactionService>();
            int removed = await cashBoxService.CleanupOrphanedCashBoxTransactionsAsync(shift.Id);

            removed.Should().Be(1);
            var remaining = await db.Context.CashBoxTransactions.AsNoTracking().ToListAsync();
            remaining.Should().ContainSingle(t => t.Type == CashBoxTransactionType.Income && t.Amount == 5_000m);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public async Task CreateTransactionAsync_WithZeroOrNegativeAmount_ShouldThrow(decimal invalidAmount)
        {
            using var db = new IsolatedSqliteTestDb();
            var cashBoxService = db.ServiceProvider.GetRequiredService<ICashBoxTransactionService>();

            Func<Task> act = () => cashBoxService.CreateTransactionAsync(new CashBoxTransaction
            {
                ShiftId = 1,
                Type = CashBoxTransactionType.Income,
                Amount = invalidAmount,
                Name = "حركة غير صالحة"
            });

            await act.Should().ThrowAsync<ArgumentException>();
        }
    }
}
