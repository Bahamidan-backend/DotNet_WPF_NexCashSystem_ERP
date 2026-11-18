using Bogus;
using POSSystem.WPF.Models;
using System;

namespace POSSystem.Tests.TestInfrastructure.TestDataBuilders
{
    public static class FinancialEntityBuilders
    {
        private static readonly Faker _faker = new("en");

        public static User BuildUser(
            string? name = null,
            string? email = null,
            UserRole role = UserRole.User,
            decimal? baseSalary = 100_000m,
            bool isActive = true)
        {
            return new User
            {
                Name = name ?? _faker.Name.FullName(),
                Email = email ?? _faker.Internet.Email(),
                Password = "TestPassword@123",
                Role = role,
                BaseSalary = baseSalary,
                HireDate = new DateTime(2026, 1, 1),
                IsActive = isActive,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        public static Shift BuildActiveShift(int userId, decimal startingCash = 100_000m)
        {
            return new Shift
            {
                ShiftNumber = $"SH-{Guid.NewGuid():N}"[..12],
                Name = "Test Shift",
                UserId = userId,
                Type = ShiftType.Morning,
                StartingCash = startingCash,
                StartTime = DateTime.Now,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        public static Product BuildProduct(
            string? name = null,
            decimal price = 5_000m,
            decimal basePrice = 2_000m,
            decimal stockQty = 50m,
            bool untracked = false)
        {
            return new Product
            {
                Name = name ?? _faker.Commerce.ProductName(),
                Price = price,
                BasePrice = basePrice,
                StockQuantity = stockQty,
                TotalValue = stockQty,
                CategoryId = 1,
                PersonNumberFood = 1m,
                IsUntrackedStock = untracked,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        public static Warehouse BuildWarehouse(string name = "المستودع الرئيسي")
        {
            return new Warehouse
            {
                Name = name,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        public static WarehouseFromProduct BuildWarehouseProduct(
            int warehouseId,
            string name,
            decimal stockQty = 100m,
            decimal price = 1_000m,
            Recipe.PortionUnit unit = Recipe.PortionUnit.Kg)
        {
            return new WarehouseFromProduct
            {
                WarehouseId = warehouseId,
                Name = name,
                StockQuantity = stockQty,
                Price = price,
                PortionUnit = unit,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        public static Supplier BuildSupplier(string? name = null, decimal initialBalance = 0m)
        {
            return new Supplier
            {
                Name = name ?? _faker.Company.CompanyName(),
                PhoneNumber = "777000111",
                TotalBalance = initialBalance,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        public static FinancialAccount BuildWalletAccount(string name, decimal initialBalance = 0m, bool allowOverdraft = false)
        {
            return new FinancialAccount
            {
                AccountName = name,
                Name = name,
                BankOrWalletName = name,
                AccountType = FinancialAccountType.ElectronicWallet,
                CurrentBalance = initialBalance,
                AllowOverdraft = allowOverdraft,
                Currency = "ريال يمني",
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }
    }
}
