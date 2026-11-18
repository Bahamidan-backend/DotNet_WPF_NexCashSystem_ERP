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

namespace POSSystem.Tests.UnitTests.Orders
{
    public class OrderTests
    {
        [Fact]
        public async Task PlaceOrder_CashPayment_ShouldDeductStockAndAddCashBoxIncome()
        {
            using var db = new IsolatedSqliteTestDb();
            var user = FinancialEntityBuilders.BuildUser();
            var product = FinancialEntityBuilders.BuildProduct("كبسة دجاج", price: 5_000m, basePrice: 2_000m, stockQty: 20m);
            db.Context.Users.Add(user);
            db.Context.Products.Add(product);
            await db.Context.SaveChangesAsync();

            var shift = FinancialEntityBuilders.BuildActiveShift(user.Id, 50_000m);
            db.Context.Shifts.Add(shift);
            await db.Context.SaveChangesAsync();

            var orderService = db.ServiceProvider.GetRequiredService<IOrderService>();
            var cashBoxService = db.ServiceProvider.GetRequiredService<ICashBoxTransactionService>();

            var order = new Order
            {
                OrderNumber = "ORD-T1",
                OrderType = "dinein",
                UserId = user.Id,
                SubTotal = 10_000m,
                Discount = 1_000m,
                Total = 9_000m,
                Status = OrderStatus.Completed,
                PaymentMethod = PaymentMethod.Cash,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { ProductId = product.Id, Quantity = 2, UnitPrice = 5_000m, TotalPrice = 10_000m, Size = "نفر" }
                }
            };

            var saved = await orderService.PlaceOrderTransactionAsync(order, PaymentMethod.Cash, user, shift);
            saved.Id.Should().BeGreaterThan(0);

            var expectedTotal = AccountingReferenceEngine.ComputeExpectedOrderTotal(new[] { (5_000m, 2) }, 1_000m, 0m, 0m);
            saved.Total.ShouldMatchAmount(expectedTotal);

            var cashBalance = await cashBoxService.GetCashBoxBalanceAsync(shift.Id);
            cashBalance.ShouldMatchAmount(59_000m);

            var updatedProduct = await db.Context.Products.FindAsync(product.Id);
            updatedProduct!.StockQuantity.Should().Be(18m);
        }

        [Fact]
        public async Task CreateOrderAsync_PriceManipulationAttempt_ShouldNotAcceptArbitraryLowPrice()
        {
            using var db = new IsolatedSqliteTestDb();
            var user = FinancialEntityBuilders.BuildUser();
            var product = FinancialEntityBuilders.BuildProduct("مندي لحم", price: 5_000m, basePrice: 3_000m, stockQty: 10m);
            db.Context.Users.Add(user);
            db.Context.Products.Add(product);
            await db.Context.SaveChangesAsync();

            var orderService = db.ServiceProvider.GetRequiredService<IOrderService>();

            // Attacker sends UnitPrice = 100 instead of DB price 5,000
            var tamperedOrder = new Order
            {
                OrderNumber = "ORD-HACK-1",
                OrderType = "dinein",
                UserId = user.Id,
                SubTotal = 100m,
                Discount = 0m,
                Total = 100m,
                Status = OrderStatus.Completed,
                PaymentMethod = PaymentMethod.Cash,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { ProductId = product.Id, Quantity = 1, UnitPrice = 100m, TotalPrice = 100m, Size = "نفر" }
                }
            };

            // Expected business rule: OrderService should either reject tampered price or recalculate from DB price (5,000).
            var result = await orderService.CreateOrderAsync(tamperedOrder);
            result.Total.Should().Be(5_000m, "OrderService must enforce DB product price (5,000) and reject/override manipulated client price (100)");
        }

        [Fact]
        public async Task CancelOrderAsync_ShouldRestoreStockAndReverseCashBox()
        {
            using var db = new IsolatedSqliteTestDb();
            var user = FinancialEntityBuilders.BuildUser();
            var product = FinancialEntityBuilders.BuildProduct("عصير مانجو", price: 1_500m, basePrice: 500m, stockQty: 30m);
            db.Context.Users.Add(user);
            db.Context.Products.Add(product);
            await db.Context.SaveChangesAsync();

            var shift = FinancialEntityBuilders.BuildActiveShift(user.Id, 20_000m);
            db.Context.Shifts.Add(shift);
            await db.Context.SaveChangesAsync();

            var orderService = db.ServiceProvider.GetRequiredService<IOrderService>();
            var cashBoxService = db.ServiceProvider.GetRequiredService<ICashBoxTransactionService>();

            var order = new Order
            {
                OrderNumber = "ORD-CANCEL-1",
                OrderType = "dinein",
                UserId = user.Id,
                SubTotal = 3_000m,
                Total = 3_000m,
                Status = OrderStatus.Completed,
                PaymentMethod = PaymentMethod.Cash,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { ProductId = product.Id, Quantity = 2, UnitPrice = 1_500m, TotalPrice = 3_000m }
                }
            };

            var saved = await orderService.PlaceOrderTransactionAsync(order, PaymentMethod.Cash, user, shift);
            await orderService.CancelOrderAsync(saved.Id, "خطأ كاشير", user.Id, restoreInventory: true);

            // Calling Cancel twice must be idempotent
            await orderService.CancelOrderAsync(saved.Id, "تكرار", user.Id, restoreInventory: true);

            (await cashBoxService.GetCashBoxBalanceAsync(shift.Id)).ShouldMatchAmount(20_000m);
            var reloadedProduct = await db.Context.Products.FindAsync(product.Id);
            reloadedProduct!.StockQuantity.Should().Be(30m);
        }

        [Theory]
        [InlineData(0, 100)]
        [InlineData(-1, 100)]
        [InlineData(1, 0)]
        [InlineData(1, -50)]
        public void ValidateOrder_WithInvalidQuantityOrPrice_ShouldReturnFalse(int qty, decimal unitPrice)
        {
            using var db = new IsolatedSqliteTestDb();
            var orderService = db.ServiceProvider.GetRequiredService<IOrderService>();

            var order = new Order
            {
                OrderNumber = "ORD-INV",
                UserId = 1,
                Total = qty * unitPrice,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { ProductId = 1, Quantity = qty, UnitPrice = unitPrice, TotalPrice = qty * unitPrice }
                }
            };

            orderService.ValidateOrder(order).Should().BeFalse();
        }
    }
}
