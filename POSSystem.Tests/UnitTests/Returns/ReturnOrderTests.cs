using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.Tests.TestInfrastructure.Assertions;
using POSSystem.Tests.TestInfrastructure.DatabaseFixtures;
using POSSystem.Tests.TestInfrastructure.TestDataBuilders;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace POSSystem.Tests.UnitTests.Returns
{
    public class ReturnOrderTests
    {
        [Fact]
        public async Task ProcessReturn_PartialThenFullAndOverReturn_ShouldEnforceLimitsAndRestoreStockOnlyForRestock()
        {
            using var db = new IsolatedSqliteTestDb();
            var user = FinancialEntityBuilders.BuildUser();
            var product = FinancialEntityBuilders.BuildProduct("دجاج شواية", price: 4_000m, basePrice: 2_000m, stockQty: 10m);
            db.Context.Users.Add(user);
            db.Context.Products.Add(product);
            await db.Context.SaveChangesAsync();

            var shift = FinancialEntityBuilders.BuildActiveShift(user.Id, 100_000m);
            db.Context.Shifts.Add(shift);
            await db.Context.SaveChangesAsync();

            var orderService = db.ServiceProvider.GetRequiredService<IOrderService>();
            var returnService = db.ServiceProvider.GetRequiredService<IReturnOrderService>();
            var cashBoxService = db.ServiceProvider.GetRequiredService<ICashBoxTransactionService>();

            var order = new Order
            {
                OrderNumber = "ORD-RET-1",
                OrderType = "dinein",
                UserId = user.Id,
                SubTotal = 12_000m,
                Discount = 0m,
                Total = 12_000m,
                Status = OrderStatus.Completed,
                PaymentMethod = PaymentMethod.Cash,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { ProductId = product.Id, Quantity = 3, UnitPrice = 4_000m, TotalPrice = 12_000m }
                }
            };

            var savedOrder = await orderService.PlaceOrderTransactionAsync(order, PaymentMethod.Cash, user, shift);
            var origItem = savedOrder.OrderItems.First();

            // Stock = 7, CashBox = 112,000
            // 1. Return 1 with Restock -> Stock = 8, CashBox = 108,000
            var ok1 = await returnService.ProcessReturnAsync(new ReturnOrder
            {
                OriginalOrderId = savedOrder.Id,
                CashierId = user.Id,
                ReturnMethod = PaymentMethod.Cash,
                Reason = "مرتجع جزئي سليم",
                ReturnOrderItems = new List<ReturnOrderItem>
                {
                    new ReturnOrderItem { OriginalOrderItemId = origItem.Id, Quantity = 1, Condition = ReturnItemCondition.Restock }
                }
            });
            ok1.Should().BeTrue();
            (await db.Context.Products.FindAsync(product.Id))!.StockQuantity.Should().Be(8m);
            (await cashBoxService.GetCashBoxBalanceAsync(shift.Id)).ShouldMatchAmount(108_000m);

            // 2. Attempt to return 3 more (exceeds remaining 2) -> must fail
            Func<Task> overReturn = () => returnService.ProcessReturnAsync(new ReturnOrder
            {
                OriginalOrderId = savedOrder.Id,
                CashierId = user.Id,
                ReturnMethod = PaymentMethod.Cash,
                Reason = "مرتجع متجاوز",
                ReturnOrderItems = new List<ReturnOrderItem>
                {
                    new ReturnOrderItem { OriginalOrderItemId = origItem.Id, Quantity = 3, Condition = ReturnItemCondition.Restock }
                }
            });
            await overReturn.Should().ThrowAsync<Exception>();

            // 3. Return remaining 2 as Waste -> Stock stays 8 (NOT 10), CashBox = 100,000
            var ok2 = await returnService.ProcessReturnAsync(new ReturnOrder
            {
                OriginalOrderId = savedOrder.Id,
                CashierId = user.Id,
                ReturnMethod = PaymentMethod.Cash,
                Reason = "مرتجع تالف",
                ReturnOrderItems = new List<ReturnOrderItem>
                {
                    new ReturnOrderItem { OriginalOrderItemId = origItem.Id, Quantity = 2, Condition = ReturnItemCondition.Waste }
                }
            });
            ok2.Should().BeTrue();
            (await db.Context.Products.FindAsync(product.Id))!.StockQuantity.Should().Be(8m);
            (await cashBoxService.GetCashBoxBalanceAsync(shift.Id)).ShouldMatchAmount(100_000m);

            // 4. Subsequent return on fully returned order must fail
            Func<Task> dupReturn = () => returnService.ProcessReturnAsync(new ReturnOrder
            {
                OriginalOrderId = savedOrder.Id,
                CashierId = user.Id,
                ReturnMethod = PaymentMethod.Cash,
                Reason = "تكرار",
                ReturnOrderItems = new List<ReturnOrderItem>
                {
                    new ReturnOrderItem { OriginalOrderItemId = origItem.Id, Quantity = 1, Condition = ReturnItemCondition.Restock }
                }
            });
            await dupReturn.Should().ThrowAsync<Exception>();
        }
    }
}
