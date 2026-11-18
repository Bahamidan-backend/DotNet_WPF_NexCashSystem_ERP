using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.Tests.TestInfrastructure.Assertions;
using POSSystem.Tests.TestInfrastructure.DatabaseFixtures;
using POSSystem.Tests.TestInfrastructure.Helpers;
using POSSystem.Tests.TestInfrastructure.TestDataBuilders;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace POSSystem.Tests.UnitTests.FinancialStatements
{
    public class FinancialReconciliationTests
    {
        [Fact]
        public async Task FinancialEngine_MandatoryReferencePnLScenario_ShouldMatchIndependentOracle()
        {
            using var db = new IsolatedSqliteTestDb();
            var user = FinancialEntityBuilders.BuildUser(name: "موظف رواتب", baseSalary: 1_000_000m);
            var product = FinancialEntityBuilders.BuildProduct("وجبة رئيسية", price: 10_000m, basePrice: 4_000m, stockQty: 2_000m);
            var packaging = FinancialEntityBuilders.BuildProduct("تغليف", price: 0m, basePrice: 300m, stockQty: 2_000m);
            db.Context.Users.Add(user);
            db.Context.Products.AddRange(product, packaging);
            await db.Context.SaveChangesAsync();

            var periodStart = new DateTime(2026, 11, 1);
            var periodEnd = new DateTime(2026, 11, 30, 23, 59, 59);

            // Seed 1,000 sales * 10,000 = 10,000,000; COGS = 4,000,000 + Packaging 300,000 = 4,300,000
            var order = new Order
            {
                OrderNumber = "ORD-PNL-1",
                OrderType = "dinein",
                UserId = user.Id,
                SubTotal = 10_000_000m,
                Discount = 0m,
                Total = 10_000_000m,
                Status = OrderStatus.Completed,
                PaymentMethod = PaymentMethod.Cash,
                CreatedAt = new DateTime(2026, 11, 15, 12, 0, 0),
                UpdatedAt = new DateTime(2026, 11, 15, 12, 0, 0),
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { ProductId = product.Id, Quantity = 1000, UnitPrice = 10_000m, TotalPrice = 10_000_000m, UnitCost = 4_000m, TotalCost = 4_000_000m },
                    new OrderItem { ProductId = packaging.Id, Quantity = 1000, UnitPrice = 0m, TotalPrice = 0m, UnitCost = 300m, TotalCost = 300_000m }
                }
            };
            db.Context.Orders.Add(order);

            // Operating Expenses: Rent 500,000 + Utilities 200,000 + Other 300,000 = 1,000,000
            db.Context.Expenses.AddRange(
                new Expense { Name = "إيجار", Category = "حساب إيجار", Amount = 500_000m, ExpenseDate = new DateTime(2026, 11, 5), PaymentMethod = PaymentMethod.Cash, Type = ExpenseType.Monthly },
                new Expense { Name = "كهرباء وماء", Category = "حساب مصاريف الكهرباء", Amount = 200_000m, ExpenseDate = new DateTime(2026, 11, 6), PaymentMethod = PaymentMethod.Cash, Type = ExpenseType.Monthly },
                new Expense { Name = "مصروفات أخرى", Category = "حساب نثريات وأخرى", Amount = 300_000m, ExpenseDate = new DateTime(2026, 11, 7), PaymentMethod = PaymentMethod.Cash, Type = ExpenseType.OneTime }
            );

            // Employee Advance of 100,000 (must NOT be counted as an expense in P&L!)
            db.Context.EmployeeAdvances.Add(new EmployeeAdvance
            {
                EmployeeId = user.Id,
                Amount = 100_000m,
                AdvanceDate = new DateTime(2026, 11, 10),
                RepaymentMethod = AdvanceRepaymentMethod.MonthlyDeduction,
                IsRepaid = false
            });

            await db.Context.SaveChangesAsync();

            var oracle = AccountingReferenceEngine.ComputeReferencePnL(
                grossSales: 10_000_000m,
                discounts: 0m,
                returns: 0m,
                cogs: 4_000_000m,
                packagingCost: 300_000m,
                salaries: 1_000_000m,
                rent: 500_000m,
                utilities: 200_000m,
                otherExpenses: 300_000m,
                stockWasteAndShortage: 0m);

            oracle.GrossProfit.Should().Be(5_700_000m);
            oracle.OperatingExpenses.Should().Be(2_000_000m);
            oracle.NetOperatingProfit.Should().Be(3_700_000m);

            var engine = db.ServiceProvider.GetRequiredService<IFinancialEngineService>();
            var report = await engine.GenerateFinancialStatementAsync(periodStart, periodEnd);

            report.NetSales.ShouldMatchAmount(oracle.NetSales);
            report.TotalCOGS.ShouldMatchAmount(oracle.TotalDirectCost);
            report.GrossProfit.ShouldMatchAmount(oracle.GrossProfit);
            report.OperatingExpenses.ShouldMatchAmount(oracle.OperatingExpenses);
            report.NetProfit.ShouldMatchAmount(oracle.NetOperatingProfit);
        }
    }
}
