using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using POSSystem.Tests.TestInfrastructure.DatabaseFixtures;
using POSSystem.Tests.TestInfrastructure.TestDataBuilders;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace POSSystem.Tests.UnitTests.Inventory
{
    public class InventoryAndCarcassTests
    {
        [Fact]
        public async Task CarcassPurchase_ShouldDecomposeIntoMeatPortionsHeadLiverAndBackWithPortionUnit()
        {
            using var db = new IsolatedSqliteTestDb();
            var user = FinancialEntityBuilders.BuildUser();
            var wh = FinancialEntityBuilders.BuildWarehouse();
            db.Context.Users.Add(user);
            db.Context.Warehouses.Add(wh);
            await db.Context.SaveChangesAsync();

            var carcassProduct = FinancialEntityBuilders.BuildWarehouseProduct(
                wh.Id, "خروف نعيمي", stockQty: 0m, price: 80_000m, unit: Recipe.PortionUnit.Carcass);
            db.Context.warehouseFromProducts.Add(carcassProduct);
            await db.Context.SaveChangesAsync();

            var purchaseService = db.ServiceProvider.GetRequiredService<IPurchaseService>();
            await purchaseService.CreatePurchaseAsync(new Purchase
            {
                PurchaseNumber = "PUR-CARC-1",
                WarehouseId = wh.Id,
                PaymentMethod = PaymentMethod.OnCredit,
                SupplierName = "مورد مواشي",
                PurchaseDate = DateTime.Now,
                TotalAmount = 400_000m,
                UserId = user.Id,
                PurchaseItems = new List<PurchaseItem>
                {
                    new PurchaseItem { ProductId = carcassProduct.Id, Quantity = 5, UnitPrice = 80_000m, TotalPrice = 400_000m, Notes = "[تفكيك: 16 جزء]" }
                }
            });

            var meat = await db.Context.warehouseFromProducts.AsNoTracking()
                .FirstOrDefaultAsync(w => w.WarehouseId == wh.Id && w.Name == "لحم خروف (نفر/جزء)");
            var head = await db.Context.warehouseFromProducts.AsNoTracking()
                .FirstOrDefaultAsync(w => w.WarehouseId == wh.Id && w.Name == "رأس خروف");

            meat.Should().NotBeNull();
            meat!.StockQuantity.Should().Be(80m);
            meat.PortionUnit.Should().Be(Recipe.PortionUnit.Portion);
            head.Should().NotBeNull();
            head!.StockQuantity.Should().Be(5m);
        }

        [Fact]
        public void UnitConversion_KgGramLiterMlCarcassPortion_ShouldConvertAccurately()
        {
            UnitConversionHelper.ConvertQuantity(2.5m, Recipe.PortionUnit.Kg, Recipe.PortionUnit.Gram).Should().Be(2500m);
            UnitConversionHelper.ConvertQuantity(500m, Recipe.PortionUnit.Gram, Recipe.PortionUnit.Kg).Should().Be(0.5m);
            UnitConversionHelper.ConvertQuantity(1m, Recipe.PortionUnit.Bag, Recipe.PortionUnit.Kg).Should().Be(40m);
            UnitConversionHelper.ConvertQuantity(1m, Recipe.PortionUnit.Carcass, Recipe.PortionUnit.Portion).Should().Be(8m);
        }
    }
}
