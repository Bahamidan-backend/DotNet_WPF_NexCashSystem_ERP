using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.Tests.TestInfrastructure.Assertions;
using POSSystem.Tests.TestInfrastructure.DatabaseFixtures;
using POSSystem.Tests.TestInfrastructure.TestDataBuilders;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Models;
using System;
using System.Threading.Tasks;
using Xunit;

namespace POSSystem.Tests.UnitTests.Expenses
{
    public class ExpenseTests
    {
        [Fact]
        public async Task CreateExpense_Cash_ShouldDeductCashBoxAndRejectWhenInsufficientFunds()
        {
            using var db = new IsolatedSqliteTestDb();
            var user = FinancialEntityBuilders.BuildUser();
            db.Context.Users.Add(user);
            await db.Context.SaveChangesAsync();

            var shift = FinancialEntityBuilders.BuildActiveShift(user.Id, 15_000m);
            db.Context.Shifts.Add(shift);
            await db.Context.SaveChangesAsync();

            var expenseService = db.ServiceProvider.GetRequiredService<IExpenseService>();
            var cashBoxService = db.ServiceProvider.GetRequiredService<ICashBoxTransactionService>();

            var exp = await expenseService.CreateExpenseAsync(new Expense
            {
                Name = "فاتورة كهرباء",
                Category = "حساب مصاريف الكهرباء",
                Amount = 10_000m,
                PaymentMethod = PaymentMethod.Cash,
                Type = ExpenseType.Monthly,
                UserId = user.Id
            });

            exp.Id.Should().BeGreaterThan(0);
            (await cashBoxService.GetCashBoxBalanceAsync(shift.Id)).ShouldMatchAmount(5_000m);

            // Attempt 8,000 when only 5,000 left -> must throw
            Func<Task> overAct = () => expenseService.CreateExpenseAsync(new Expense
            {
                Name = "مصروف زائد",
                Category = "حساب نثريات وأخرى",
                Amount = 8_000m,
                PaymentMethod = PaymentMethod.Cash,
                Type = ExpenseType.OneTime,
                UserId = user.Id
            });
            await overAct.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
