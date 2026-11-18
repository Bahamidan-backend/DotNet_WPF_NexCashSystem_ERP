using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Data;
using POSSystem.WPF.Data.UnitOfWork;
using POSSystem.WPF.Models;

Console.OutputEncoding = System.Text.Encoding.UTF8;
string dbPath = @"C:\Users\AlShaheen\Desktop\nexSystem\poss.db";

var optionsBuilder = new DbContextOptionsBuilder<POSDbContext>();
optionsBuilder.UseSqlite($"Data Source={dbPath}");
optionsBuilder.EnableSensitiveDataLogging();

using var dbContext = new POSDbContext(optionsBuilder.Options);
var unitOfWork = new UnitOfWork(dbContext);

var cashBoxTxService = new CashBoxTransactionService(unitOfWork);
var customerDebtService = new CustomerDebtService(unitOfWork);
var orderService = new OrderService(unitOfWork, customerDebtService, cashBoxTxService);

var user = await dbContext.Users.FirstAsync(u => u.Id == 2);
var shift = await dbContext.Shifts.FirstAsync(s => s.Id == 1 && s.IsActive);
var product = await dbContext.Products.FirstAsync(p => p.Id == 1);

Console.WriteLine($"User: {user.Id} ({user.Name}), Shift: {shift?.Id}, Product: {product.Id} ({product.Name})");

var order = new Order
{
    OrderNumber = await orderService.GenerateUniqueOrderNumberAsync(user.Id),
    OrderType = "dinein",
    UserId = user.Id,
    ShiftId = shift?.Id,
    SubTotal = product.Price,
    Total = product.Price,
    PaymentMethod = PaymentMethod.OnCredit,
    Status = OrderStatus.Completed,
    CreatedAt = DateTime.Now,
    UpdatedAt = DateTime.Now,
    OrderItems = new List<OrderItem>
    {
        new OrderItem
        {
            ProductId = product.Id,
            Quantity = 1,
            UnitPrice = product.Price,
            TotalPrice = product.Price,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            Size = "نفر"
        }
    }
};

var customerDebt = new CustomerDebt
{
    CustomerId = 0,
    CustomerName = "عميل تجربة",
    PhoneNumber = "0555555555",
    Amount = order.Total,
    IsPaid = false,
    Notes = "تجربة دفع دين",
    DebtDate = order.CreatedAt,
    UpdatedAt = order.UpdatedAt.Value
};

try
{
    Console.WriteLine("Executing PlaceOrderTransactionAsync with OnCredit...");
    await orderService.PlaceOrderTransactionAsync(order, PaymentMethod.OnCredit, user, shift, customerDebt);
    Console.WriteLine("SUCCESS! Debt created successfully!");
}
catch (Exception ex)
{
    Console.WriteLine("EXCEPTION CAUGHT:");
    Console.WriteLine(ex.ToString());
    if (ex.InnerException != null)
    {
        Console.WriteLine("\nINNER EXCEPTION:");
        Console.WriteLine(ex.InnerException.ToString());
    }
}
