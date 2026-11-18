using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.Tests.TestInfrastructure.Assertions;
using POSSystem.Tests.TestInfrastructure.DatabaseFixtures;
using POSSystem.Tests.TestInfrastructure.TestDataBuilders;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace POSSystem.Tests.UnitTests.EmployeeAdvances
{
    public class EmployeeAdvanceTests
    {
        [Fact]
        public async Task AdvanceLifecycle_CreateAndDelete_ShouldDeductAndRestoreCashBoxAndVoucher()
        {
            using var db = new IsolatedSqliteTestDb();
            var user = FinancialEntityBuilders.BuildUser(name: "أحمد الكاشير", baseSalary: 120_000m);
            db.Context.Users.Add(user);
            await db.Context.SaveChangesAsync();

            var shift = FinancialEntityBuilders.BuildActiveShift(user.Id, startingCash: 100_000m);
            db.Context.Shifts.Add(shift);
            await db.Context.SaveChangesAsync();

            var employeeService = db.ServiceProvider.GetRequiredService<IEmployeeService>();
            var cashBoxService = db.ServiceProvider.GetRequiredService<ICashBoxTransactionService>();

            // 1. Create advance of 20,000
            var advance = await employeeService.CreateAdvanceAsync(new EmployeeAdvance
            {
                EmployeeId = user.Id,
                Amount = 20_000m,
                AdvanceDate = DateTime.Now,
                PaymentMethod = PaymentMethod.Cash,
                RepaymentMethod = AdvanceRepaymentMethod.MonthlyDeduction,
                TargetShiftId = shift.Id,
                Notes = "سلفة طارئة"
            });

            var balanceAfterAdvance = await cashBoxService.GetCashBoxBalanceAsync(shift.Id);
            var unpaidAfterAdvance = await employeeService.GetTotalUnpaidAdvancesAsync(user.Id);

            balanceAfterAdvance.ShouldMatchAmount(80_000m);
            unpaidAfterAdvance.ShouldMatchAmount(20_000m);
            advance.ReceiptVoucherId.Should().NotBeNull();

            // 2. Delete advance -> CashBox returns to 100,000 and unpaid advances return to 0
            await employeeService.DeleteAdvanceAsync(advance.Id);

            var balanceAfterDelete = await cashBoxService.GetCashBoxBalanceAsync(shift.Id);
            var unpaidAfterDelete = await employeeService.GetTotalUnpaidAdvancesAsync(user.Id);
            var remainingVouchers = await db.Context.ReceiptVouchers.AsNoTracking().Where(v => v.EmployeeAdvanceId == advance.Id).ToListAsync();

            balanceAfterDelete.ShouldMatchAmount(100_000m);
            unpaidAfterDelete.ShouldMatchAmount(0m);
            remainingVouchers.Should().BeEmpty();
        }

        [Fact]
        public async Task RepayAdvance_PartialThenFull_ShouldUpdateRemainingAndCashBox()
        {
            using var db = new IsolatedSqliteTestDb();
            var user = FinancialEntityBuilders.BuildUser(name: "سالم", baseSalary: 100_000m);
            db.Context.Users.Add(user);
            await db.Context.SaveChangesAsync();

            var shift = FinancialEntityBuilders.BuildActiveShift(user.Id, startingCash: 100_000m);
            db.Context.Shifts.Add(shift);
            await db.Context.SaveChangesAsync();

            var employeeService = db.ServiceProvider.GetRequiredService<IEmployeeService>();
            var cashBoxService = db.ServiceProvider.GetRequiredService<ICashBoxTransactionService>();

            var advance = await employeeService.CreateAdvanceAsync(new EmployeeAdvance
            {
                EmployeeId = user.Id,
                Amount = 30_000m,
                AdvanceDate = DateTime.Now,
                PaymentMethod = PaymentMethod.Cash,
                RepaymentMethod = AdvanceRepaymentMethod.Installment,
                TargetShiftId = shift.Id
            });

            await employeeService.RepayAdvanceAsync(advance.Id, 10_000m, "سداد دفعة أولى");
            (await employeeService.GetTotalUnpaidAdvancesAsync(user.Id)).ShouldMatchAmount(20_000m);

            await employeeService.RepayAdvanceAsync(advance.Id, 20_000m, "سداد باقي السلفة");
            (await employeeService.GetTotalUnpaidAdvancesAsync(user.Id)).ShouldMatchAmount(0m);
            (await cashBoxService.GetCashBoxBalanceAsync(shift.Id)).ShouldMatchAmount(100_000m);

            // Cannot delete a repaid advance
            Func<Task> delAct = () => employeeService.DeleteAdvanceAsync(advance.Id);
            await delAct.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
