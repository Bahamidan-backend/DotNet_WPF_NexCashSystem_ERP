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
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace POSSystem.Tests.UnitTests.Suppliers
{
    public class SupplierTests
    {
        [Fact]
        public async Task SupplierLedger_AfterCreditPurchasePaymentAndPurchaseReturn_MustReconcileWithTotalBalance()
        {
            using var db = new IsolatedSqliteTestDb();
            var user = FinancialEntityBuilders.BuildUser();
            var wh = FinancialEntityBuilders.BuildWarehouse();
            db.Context.Users.Add(user);
            db.Context.Warehouses.Add(wh);
            await db.Context.SaveChangesAsync();

            var whItem = FinancialEntityBuilders.BuildWarehouseProduct(wh.Id, "زيت طبخ", stockQty: 0m, price: 2_000m);
            var supplier = FinancialEntityBuilders.BuildSupplier("مورد الخير", initialBalance: 0m);
            db.Context.warehouseFromProducts.Add(whItem);
            db.Context.Suppliers.Add(supplier);
            await db.Context.SaveChangesAsync();

            var shift = FinancialEntityBuilders.BuildActiveShift(user.Id, 200_000m);
            db.Context.Shifts.Add(shift);
            await db.Context.SaveChangesAsync();

            var purchaseService = db.ServiceProvider.GetRequiredService<IPurchaseService>();
            var purchaseReturnService = db.ServiceProvider.GetRequiredService<IPurchaseReturnService>();
            var supplierService = db.ServiceProvider.GetRequiredService<ISupplierService>();

            // 1. Credit Purchase of 50,000 -> Supplier.TotalBalance = 50,000
            var purchase = await purchaseService.CreatePurchaseAsync(new Purchase
            {
                PurchaseNumber = "PUR-SUP-1",
                SupplierId = supplier.Id,
                SupplierName = supplier.Name,
                WarehouseId = wh.Id,
                PaymentMethod = PaymentMethod.OnCredit,
                PurchaseDate = DateTime.Now,
                TotalAmount = 50_000m,
                UserId = user.Id,
                PurchaseItems = new List<PurchaseItem>
                {
                    new PurchaseItem { ProductId = whItem.Id, Quantity = 25, UnitPrice = 2_000m, TotalPrice = 50_000m }
                }
            });

            // 2. Pay 20,000 to supplier -> Supplier.TotalBalance = 30,000
            await supplierService.RecordPaymentAsync(new SupplierPayment
            {
                SupplierId = supplier.Id,
                Amount = 20_000m,
                PaymentMethod = PaymentMethod.Cash,
                UserId = user.Id
            }, shift.Id);

            // 3. Return 5 units (10,000) to supplier on credit -> Supplier.TotalBalance = 20,000
            var origItem = purchase.PurchaseItems.First();
            await purchaseReturnService.ProcessReturnAsync(new PurchaseReturn
            {
                OriginalPurchaseId = purchase.Id,
                ReturnMethod = PaymentMethod.OnCredit,
                SettlementMode = PurchaseReturnSettlementMode.DebitNote,
                UserId = user.Id,
                Reason = "بضاعة زائدة",
                PurchaseReturnItems = new List<PurchaseReturnItem>
                {
                    new PurchaseReturnItem { OriginalPurchaseItemId = origItem.Id, Quantity = 5, UnitPrice = 2_000m, TotalAmount = 10_000m }
                }
            });

            var expectedBalance = AccountingReferenceEngine.ComputeExpectedSupplierBalance(
                new[] { 50_000m },
                new[] { 20_000m },
                new[] { 10_000m });

            var reloadedSupplier = await supplierService.GetSupplierByIdAsync(supplier.Id);
            reloadedSupplier!.TotalBalance.ShouldMatchAmount(expectedBalance);

            // Check if GetSupplierLedgerAsync includes the PurchaseReturn!
            var ledger = (await supplierService.GetSupplierLedgerAsync(supplier.Id)).ToList();
            var ledgerFinalBalance = ledger.FirstOrDefault()?.Balance ?? 0m;
            ledgerFinalBalance.ShouldMatchAmount(expectedBalance, "كشف حساب المورد يجب أن يطابق الرصيد الفعلي بعد احتساب مرتجعات المشتريات الآجلة");
        }
    }
}
