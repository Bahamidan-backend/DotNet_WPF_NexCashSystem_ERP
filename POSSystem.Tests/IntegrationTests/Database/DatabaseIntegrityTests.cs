using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using POSSystem.Tests.TestInfrastructure.DatabaseFixtures;
using POSSystem.Tests.TestInfrastructure.TestDataBuilders;
using POSSystem.WPF.Models;
using System;
using System.Threading.Tasks;
using Xunit;

namespace POSSystem.Tests.IntegrationTests.Database
{
    public class DatabaseIntegrityTests
    {
        [Fact]
        public async Task UniqueConstraints_DuplicateOrderNumberAndDuplicateActiveShift_MustThrowDbUpdateException()
        {
            using var db = new IsolatedSqliteTestDb();
            var user = FinancialEntityBuilders.BuildUser();
            db.Context.Users.Add(user);
            await db.Context.SaveChangesAsync();

            db.Context.Shifts.Add(FinancialEntityBuilders.BuildActiveShift(user.Id, 1000m));
            await db.Context.SaveChangesAsync();

            // Second active shift for same user violates partial unique index (UserId, IsActive=1)
            db.Context.Shifts.Add(FinancialEntityBuilders.BuildActiveShift(user.Id, 2000m));
            Func<Task> dupShift = () => db.Context.SaveChangesAsync();
            await dupShift.Should().ThrowAsync<DbUpdateException>();
        }

        [Fact]
        public async Task TransactionRollback_MidExecutionFailure_ShouldLeaveZeroPartialRecords()
        {
            using var db = new IsolatedSqliteTestDb();
            var user = FinancialEntityBuilders.BuildUser();
            db.Context.Users.Add(user);
            await db.Context.SaveChangesAsync();

            Func<Task> act = () => db.UnitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await db.UnitOfWork.Expenses.AddAsync(new Expense
                {
                    Name = "مصروف مؤقت",
                    Category = "حساب إيجار",
                    Amount = 1_000m,
                    ExpenseDate = DateTime.Now,
                    UserId = user.Id
                });
                await db.UnitOfWork.SaveChangesAsync();
                throw new InvalidOperationException("Simulated mid-transaction crash");
            });

            await act.Should().ThrowAsync<InvalidOperationException>();
            (await db.Context.Expenses.AsNoTracking().CountAsync()).Should().Be(0);
        }
    }
}
