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

namespace POSSystem.Tests.UnitTests.Salaries
{
    public class SalaryTests
    {
        [Fact]
        public async Task SalaryCreationAndPayment_ShouldDeductMonthlyAdvanceAndPreventDoublePayment()
        {
            using var db = new IsolatedSqliteTestDb();
            var user = FinancialEntityBuilders.BuildUser(name: "عمر الطباخ", baseSalary: 150_000m);
            db.Context.Users.Add(user);
            await db.Context.SaveChangesAsync();

            var shift = FinancialEntityBuilders.BuildActiveShift(user.Id, startingCash: 300_000m);
            db.Context.Shifts.Add(shift);
            await db.Context.SaveChangesAsync();

            var employeeService = db.ServiceProvider.GetRequiredService<IEmployeeService>();
            var cashBoxService = db.ServiceProvider.GetRequiredService<ICashBoxTransactionService>();

            // Advance 25,000 from CashBox -> CashBox becomes 275,000
            await employeeService.CreateAdvanceAsync(new EmployeeAdvance
            {
                EmployeeId = user.Id,
                Amount = 25_000m,
                AdvanceDate = DateTime.Now,
                PaymentMethod = PaymentMethod.Cash,
                RepaymentMethod = AdvanceRepaymentMethod.MonthlyDeduction,
                TargetShiftId = shift.Id
            });

            // Create Salary -> Base=150,000, AdvanceDeduction=25,000, NetSalary=125,000
            var salary = await employeeService.CreateSalaryAsync(new EmployeeSalary
            {
                EmployeeId = user.Id,
                SalaryDate = new DateTime(2026, 11, 1)
            });

            salary.BaseSalary.ShouldMatchAmount(150_000m);
            salary.AdvanceDeduction.ShouldMatchAmount(25_000m);
            salary.NetSalary.ShouldMatchAmount(125_000m);
            salary.IsPaid.Should().BeFalse();

            // Pay salary in Cash -> CashBox becomes 275,000 - 125,000 = 150,000
            await employeeService.MarkSalaryAsPaidAsync(salary.Id, PaymentMethod.Cash, shift.Id);

            // Second call must be idempotent and NOT pay twice
            await employeeService.MarkSalaryAsPaidAsync(salary.Id, PaymentMethod.Cash, shift.Id);

            var cashAfter = await cashBoxService.GetCashBoxBalanceAsync(shift.Id);
            cashAfter.ShouldMatchAmount(150_000m);

            var unpaidAdv = await employeeService.GetTotalUnpaidAdvancesAsync(user.Id);
            unpaidAdv.ShouldMatchAmount(0m);

            var salaryVouchers = await db.Context.ReceiptVouchers
                .AsNoTracking()
                .Where(v => v.VoucherNumber != null && v.VoucherNumber.StartsWith("PV-SAL-"))
                .ToListAsync();
            salaryVouchers.Should().ContainSingle();
        }
    }
}
