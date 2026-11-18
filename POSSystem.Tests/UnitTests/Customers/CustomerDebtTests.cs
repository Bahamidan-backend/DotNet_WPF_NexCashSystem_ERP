using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.Tests.TestInfrastructure.Assertions;
using POSSystem.Tests.TestInfrastructure.DatabaseFixtures;
using POSSystem.Tests.TestInfrastructure.Helpers;
using POSSystem.Tests.TestInfrastructure.TestDataBuilders;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Models;
using System;
using System.Threading.Tasks;
using Xunit;

namespace POSSystem.Tests.UnitTests.Customers
{
    public class CustomerDebtTests
    {
        [Fact]
        public async Task CustomerDebt_CreatePartialRepayAndFullSettle_ShouldMatchOracleAndCashBox()
        {
            using var db = new IsolatedSqliteTestDb();
            var user = FinancialEntityBuilders.BuildUser();
            db.Context.Users.Add(user);
            await db.Context.SaveChangesAsync();

            var shift = FinancialEntityBuilders.BuildActiveShift(user.Id, 10_000m);
            db.Context.Shifts.Add(shift);
            await db.Context.SaveChangesAsync();

            var debtService = db.ServiceProvider.GetRequiredService<ICustomerDebtService>();
            var cashBoxService = db.ServiceProvider.GetRequiredService<ICashBoxTransactionService>();

            var debt = await debtService.CreateDebtAsync(new CustomerDebt
            {
                CustomerName = "عميل آجل",
                PhoneNumber = "777123456",
                Amount = 25_000m,
                CreditLimit = 30_000m,
                UserId = user.Id,
                ShiftId = shift.Id
            });

            // Exceeding credit limit (25,000 + 10,000 > 30,000) must throw
            Func<Task> exceedAct = () => debtService.CreateDebtAsync(new CustomerDebt
            {
                CustomerName = "عميل آجل",
                PhoneNumber = "777123456",
                Amount = 10_000m,
                UserId = user.Id,
                ShiftId = shift.Id
            });
            await exceedAct.Should().ThrowAsync<InvalidOperationException>();

            // Partial repayment 10,000
            await debtService.RepayDebtAsync(debt.Id, 10_000m, user.Id, shift.Id, "دفعة 1", PaymentMethod.Cash);
            var afterPartial = await debtService.GetDebtByIdAsync(debt.Id);
            var expectedRemaining = AccountingReferenceEngine.ComputeExpectedCustomerDebtRemaining(25_000m, new[] { 10_000m }, Array.Empty<decimal>());
            afterPartial!.RemainingAmount.ShouldMatchAmount(expectedRemaining);

            // Full settlement of remaining 15,000
            await debtService.MarkDebtAsPaidAsync(debt.Id, user.Id, shift.Id, PaymentMethod.Cash);
            var afterFull = await debtService.GetDebtByIdAsync(debt.Id);
            afterFull!.IsPaid.Should().BeTrue();
            afterFull.RemainingAmount.ShouldMatchAmount(0m);

            (await cashBoxService.GetCashBoxBalanceAsync(shift.Id)).ShouldMatchAmount(35_000m);
        }
    }
}
