using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Services.Logging;
using POSSystem.WPF.Business.Services;
using POSSystem.WPF.Helpers;
using POSSystem.WPF.Models;

namespace POSSystem.WPF.Data
{
    public class DatabaseInitializer
    {
        private readonly POSDbContext _context;

        public DatabaseInitializer(POSDbContext context)
        {
            _context = context;
        }

        private async Task<HashSet<string>> GetExistingColumnsAsync(string tableName)
        {
            var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var conn = _context.Database.GetDbConnection();
                bool wasClosed = conn.State != System.Data.ConnectionState.Open;
                if (wasClosed) await conn.OpenAsync();
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"PRAGMA table_info({tableName});";
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        if (reader.FieldCount > 1 && !reader.IsDBNull(1))
                        {
                            cols.Add(reader.GetString(1));
                        }
                    }
                }
                finally
                {
                    if (wasClosed) await conn.CloseAsync();
                }
            }
            catch { }
            return cols;
        }

        public async Task InitializeAsync()
        {
            // 1. High-performance & reliability SQLite PRAGMAs
            try
            {
                await _context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
                await _context.Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;");
                await _context.Database.ExecuteSqlRawAsync("PRAGMA temp_store=MEMORY;");
                await _context.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;");
                await _context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON;");
                await _context.Database.ExecuteSqlRawAsync("PRAGMA cache_size=-20000;");
            }
            catch { }

            // 2. Run EF Core Migrations FIRST
            try
            {
                await _context.Database.MigrateAsync();
                AppLogger.LogInfo("Database migrations applied successfully.");
            }
            catch (Exception ex)
            {
                AppLogger.LogWarning($"Database migration warning (non-fatal, safe fallbacks active): {ex.Message}");
            }

            // 3. FIX ISSUE-PERF-004: Inspect PRAGMA table_info before executing ALTER TABLE so 0 exceptions are thrown on startup
            var requiredColumns = new (string Table, string Column, string AlterSql)[]
            {
                ("warehouseFromProducts", "MinimumStockLevel", "ALTER TABLE warehouseFromProducts ADD COLUMN MinimumStockLevel TEXT NOT NULL DEFAULT '5';"),
                ("Recipes", "WarehouseFromProductId", "ALTER TABLE Recipes ADD COLUMN WarehouseFromProductId INTEGER;"),
                ("Orders", "TaxRate", "ALTER TABLE Orders ADD COLUMN TaxRate TEXT NOT NULL DEFAULT '0';"),
                ("Orders", "TaxAmount", "ALTER TABLE Orders ADD COLUMN TaxAmount TEXT NOT NULL DEFAULT '0';"),
                ("StockMovements", "CreatedAt", "ALTER TABLE StockMovements ADD COLUMN CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime'));"),
                ("Expenses", "PaymentMethod", "ALTER TABLE Expenses ADD COLUMN PaymentMethod INTEGER NOT NULL DEFAULT 0;"),
                ("Customers", "CreditLimit", "ALTER TABLE Customers ADD COLUMN CreditLimit TEXT NOT NULL DEFAULT '0';"),
                ("EmployeeAdvances", "PaymentMethod", "ALTER TABLE EmployeeAdvances ADD COLUMN PaymentMethod INTEGER NOT NULL DEFAULT 0;"),
                ("ReceiptVouchers", "PaymentMethod", "ALTER TABLE ReceiptVouchers ADD COLUMN PaymentMethod INTEGER NOT NULL DEFAULT 0;"),
                ("ReceiptVouchers", "EmployeeAdvanceId", "ALTER TABLE ReceiptVouchers ADD COLUMN EmployeeAdvanceId INTEGER;"),
                ("EmployeeAdvances", "ReceiptVoucherId", "ALTER TABLE EmployeeAdvances ADD COLUMN ReceiptVoucherId INTEGER;"),
                ("EmployeeSalaries", "PaymentMethod", "ALTER TABLE EmployeeSalaries ADD COLUMN PaymentMethod INTEGER NOT NULL DEFAULT 0;"),
                ("OrderItems", "UnitCost", "ALTER TABLE OrderItems ADD COLUMN UnitCost TEXT NOT NULL DEFAULT '0';"),
                ("OrderItems", "TotalCost", "ALTER TABLE OrderItems ADD COLUMN TotalCost TEXT NOT NULL DEFAULT '0';"),
                ("Users", "PermissionsJson", "ALTER TABLE Users ADD COLUMN PermissionsJson TEXT;"),
                ("Users", "RoleTemplateName", "ALTER TABLE Users ADD COLUMN RoleTemplateName TEXT;")
            };

            var columnsCache = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var (table, column, alterSql) in requiredColumns)
            {
                if (!columnsCache.TryGetValue(table, out var existingCols))
                {
                    existingCols = await GetExistingColumnsAsync(table);
                    columnsCache[table] = existingCols;
                }

                if (existingCols.Count > 0 && !existingCols.Contains(column))
                {
                    try
                    {
                        await _context.Database.ExecuteSqlRawAsync(alterSql);
                        existingCols.Add(column);
                    }
                    catch { }
                }
            }

            try
            {
                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS FinancialAccounts (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL DEFAULT '',
                        Description TEXT,
                        AccountName TEXT NOT NULL DEFAULT '',
                        AccountType INTEGER NOT NULL DEFAULT 1,
                        BankOrWalletName TEXT NOT NULL DEFAULT '',
                        AccountNumber TEXT,
                        Currency TEXT NOT NULL DEFAULT 'ريال يمني',
                        CurrentBalance TEXT NOT NULL DEFAULT '0',
                        TotalInflow TEXT NOT NULL DEFAULT '0',
                        TotalOutflow TEXT NOT NULL DEFAULT '0',
                        LastTransactionDate TEXT,
                        AllowOverdraft INTEGER NOT NULL DEFAULT 0,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1
                    );
                    CREATE INDEX IF NOT EXISTS IX_FinancialAccounts_AccountType ON FinancialAccounts(AccountType);
                    CREATE INDEX IF NOT EXISTS IX_FinancialAccounts_IsActive ON FinancialAccounts(IsActive);

                    CREATE TABLE IF NOT EXISTS FinancialAccountTransactions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL DEFAULT '',
                        Description TEXT,
                        FinancialAccountId INTEGER NOT NULL,
                        RelatedAccountId INTEGER,
                        TransactionType INTEGER NOT NULL,
                        AmountIn TEXT NOT NULL DEFAULT '0',
                        AmountOut TEXT NOT NULL DEFAULT '0',
                        BalanceAfter TEXT NOT NULL DEFAULT '0',
                        ReferenceNumber TEXT,
                        SourceEntityType TEXT,
                        SourceEntityId INTEGER,
                        PerformedByUserId INTEGER,
                        PerformedByUserName TEXT NOT NULL DEFAULT 'النظام',
                        TransactionDate TEXT NOT NULL,
                        Notes TEXT,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1
                    );
                    CREATE INDEX IF NOT EXISTS IX_FinancialAccountTransactions_AccountId ON FinancialAccountTransactions(FinancialAccountId);
                    CREATE INDEX IF NOT EXISTS IX_FinancialAccountTransactions_Date ON FinancialAccountTransactions(TransactionDate);
                    CREATE INDEX IF NOT EXISTS IX_FinancialAccountTransactions_Type ON FinancialAccountTransactions(TransactionType);
                ");
            }
            catch { }

            try
            {
                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS StockWastes (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        WarehouseFromProductId INTEGER NOT NULL,
                        WarehouseId INTEGER,
                        Quantity TEXT NOT NULL,
                        PortionUnit INTEGER,
                        UnitCost TEXT NOT NULL,
                        TotalCost TEXT NOT NULL,
                        Reason INTEGER NOT NULL,
                        WasteDate TEXT NOT NULL,
                        ReportedByUserId INTEGER,
                        Notes TEXT,
                        Name TEXT,
                        Description TEXT,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL,
                        IsActive INTEGER NOT NULL DEFAULT 1
                    );
                    CREATE INDEX IF NOT EXISTS IX_StockWastes_WarehouseFromProductId ON StockWastes (WarehouseFromProductId);
                    CREATE INDEX IF NOT EXISTS IX_StockWastes_WarehouseId ON StockWastes (WarehouseId);
                    CREATE INDEX IF NOT EXISTS IX_StockWastes_WasteDate ON StockWastes (WasteDate);
                ");
            }
            catch { }

            try
            {
                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS HeldOrders (
                        Id TEXT PRIMARY KEY NOT NULL,
                        HoldTime TEXT NOT NULL,
                        CustomerName TEXT,
                        CustomerPhone TEXT,
                        OrderType TEXT,
                        DiscountAmount TEXT NOT NULL DEFAULT '0',
                        DiscountCode TEXT,
                        HasDiscount INTEGER NOT NULL DEFAULT 0,
                        TotalAmount TEXT NOT NULL DEFAULT '0',
                        DeliveryAddress TEXT,
                        DeliveryDriver TEXT,
                        DeliveryFee TEXT NOT NULL DEFAULT '0',
                        ShiftId INTEGER,
                        UserId INTEGER,
                        ItemsJson TEXT
                    );
                    CREATE INDEX IF NOT EXISTS IX_HeldOrders_HoldTime ON HeldOrders(HoldTime);
                    CREATE INDEX IF NOT EXISTS IX_HeldOrders_ShiftId ON HeldOrders(ShiftId);
                ");
            }
            catch { }

            try
            {
                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS StockMovements (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        WarehouseFromProductId INTEGER NOT NULL,
                        WarehouseId INTEGER,
                        MovementType INTEGER NOT NULL,
                        Quantity TEXT NOT NULL,
                        BalanceBefore TEXT NOT NULL,
                        BalanceAfter TEXT NOT NULL,
                        UnitCost TEXT NOT NULL,
                        TotalCost TEXT NOT NULL,
                        MovementDate TEXT NOT NULL,
                        ReferenceNumber TEXT,
                        OrderId INTEGER,
                        PurchaseId INTEGER,
                        UserId INTEGER,
                        Notes TEXT
                    );
                    CREATE INDEX IF NOT EXISTS IX_StockMovements_WarehouseFromProductId ON StockMovements (WarehouseFromProductId);
                    CREATE INDEX IF NOT EXISTS IX_StockMovements_WarehouseId ON StockMovements (WarehouseId);
                    CREATE INDEX IF NOT EXISTS IX_StockMovements_MovementDate ON StockMovements (MovementDate);
                    CREATE INDEX IF NOT EXISTS IX_StockMovements_MovementType ON StockMovements (MovementType);
                ");
            }
            catch { }

            // FIX ISSUE-PERF-003: Complete Index Suite covering all operational, recipe, payroll, debt, and voucher joins
            try
            {
                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS KitchenCookingBatches (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        BatchNumber TEXT NOT NULL,
                        ProductId INTEGER NOT NULL,
                        WarehouseId INTEGER NOT NULL,
                        MainIngredientId INTEGER,
                        BatchMultiplier TEXT NOT NULL DEFAULT '1.0',
                        MainIngredientQuantity TEXT NOT NULL DEFAULT '0',
                        PortionUnit INTEGER NOT NULL DEFAULT 1,
                        ExpectedPortions TEXT NOT NULL DEFAULT '0',
                        ActualPortions TEXT NOT NULL DEFAULT '0',
                        TotalRawCost TEXT NOT NULL DEFAULT '0',
                        CookedByUserId INTEGER,
                        ShiftId INTEGER,
                        Notes TEXT,
                        CookedAt TEXT NOT NULL,
                        Status TEXT NOT NULL DEFAULT 'Completed'
                    );
                    CREATE INDEX IF NOT EXISTS IX_KitchenCookingBatches_ProductId ON KitchenCookingBatches(ProductId);
                    CREATE INDEX IF NOT EXISTS IX_KitchenCookingBatches_WarehouseId ON KitchenCookingBatches(WarehouseId);
                    CREATE INDEX IF NOT EXISTS IX_KitchenCookingBatches_CookedAt ON KitchenCookingBatches(CookedAt);

                    CREATE TABLE IF NOT EXISTS KitchenCookingBatchItems (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        KitchenCookingBatchId INTEGER NOT NULL,
                        WarehouseFromProductId INTEGER NOT NULL,
                        ItemName TEXT NOT NULL,
                        Quantity TEXT NOT NULL,
                        PortionUnit INTEGER NOT NULL DEFAULT 1,
                        UnitCost TEXT NOT NULL DEFAULT '0',
                        TotalCost TEXT NOT NULL DEFAULT '0'
                    );
                    CREATE INDEX IF NOT EXISTS IX_KitchenCookingBatchItems_BatchId ON KitchenCookingBatchItems(KitchenCookingBatchId);
                    CREATE INDEX IF NOT EXISTS IX_KitchenCookingBatchItems_WarehouseItemId ON KitchenCookingBatchItems(WarehouseFromProductId);

                    CREATE INDEX IF NOT EXISTS IX_Orders_PaymentMethod_Status ON Orders(PaymentMethod, Status);
                    CREATE INDEX IF NOT EXISTS IX_Orders_CreatedAt_Status ON Orders(CreatedAt, Status);
                    CREATE INDEX IF NOT EXISTS IX_Purchases_PurchaseDate ON Purchases(PurchaseDate);
                    CREATE INDEX IF NOT EXISTS IX_Purchases_SupplierId ON Purchases(SupplierId);
                    CREATE INDEX IF NOT EXISTS IX_PurchaseItems_PurchaseId ON PurchaseItems(PurchaseId);
                    CREATE INDEX IF NOT EXISTS IX_PurchaseItems_ProductId ON PurchaseItems(ProductId);
                    CREATE INDEX IF NOT EXISTS IX_ReceiptVouchers_Type_PaymentMethod ON ReceiptVouchers(Type, PaymentMethod);
                    CREATE INDEX IF NOT EXISTS IX_ReceiptVouchers_VoucherDate ON ReceiptVouchers(VoucherDate);
                    CREATE INDEX IF NOT EXISTS IX_ReceiptVouchers_CustomerDebtId ON ReceiptVouchers(CustomerDebtId);
                    CREATE INDEX IF NOT EXISTS IX_ReceiptVouchers_EmployeeAdvanceId ON ReceiptVouchers(EmployeeAdvanceId);
                    CREATE INDEX IF NOT EXISTS IX_Shifts_StartTime ON Shifts(StartTime);
                    CREATE INDEX IF NOT EXISTS IX_Customers_Name ON Customers(Name);
                    CREATE INDEX IF NOT EXISTS IX_Customers_PhoneNumber ON Customers(PhoneNumber);
                    CREATE INDEX IF NOT EXISTS IX_CustomerDebts_OrderId ON CustomerDebts(OrderId);
                    CREATE INDEX IF NOT EXISTS IX_CashBoxTransactions_ShiftId_TransactionDate ON CashBoxTransactions(ShiftId, TransactionDate);
                    CREATE INDEX IF NOT EXISTS IX_BankDeposits_DepositDate ON BankDeposits(DepositDate);
                    CREATE INDEX IF NOT EXISTS IX_Recipes_ProductId ON Recipes(ProductId);
                    CREATE INDEX IF NOT EXISTS IX_Recipes_WarehouseFromProductId ON Recipes(WarehouseFromProductId);
                    CREATE INDEX IF NOT EXISTS IX_EmployeeAdvances_EmployeeId_IsRepaid ON EmployeeAdvances(EmployeeId, IsRepaid);
                    CREATE INDEX IF NOT EXISTS IX_EmployeeSalaries_EmployeeId_IsPaid_SalaryDate ON EmployeeSalaries(EmployeeId, IsPaid, SalaryDate);
                    CREATE INDEX IF NOT EXISTS IX_ReturnOrderItems_ReturnOrderId ON ReturnOrderItems(ReturnOrderId);
                    CREATE INDEX IF NOT EXISTS IX_ReturnOrderItems_OriginalOrderItemId ON ReturnOrderItems(OriginalOrderItemId);
                    CREATE INDEX IF NOT EXISTS IX_SupplierPayments_SupplierId_PaymentDate ON SupplierPayments(SupplierId, PaymentDate);
                    PRAGMA optimize;
                ");
            }
            catch { }

            // 4. Ensure default Admin (admin@pos.com / admin123) and Cashier (user@pos.com / user123) exist and are active
            try
            {
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@pos.com" || u.Role == UserRole.Admin);
                if (adminUser == null)
                {
                    _context.Users.Add(new User
                    {
                        Email = "admin@pos.com",
                        Password = PasswordHasher.HashPassword("admin123"),
                        Name = "Administrator",
                        Role = UserRole.Admin,
                        RoleTemplateName = "مدير كامل الصلاحيات",
                        IsActive = true,
                        MustChangePassword = false,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });
                }
                else
                {
                    adminUser.IsActive = true;
                    adminUser.MustChangePassword = false;
                }

                var cashierUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "user@pos.com");
                if (cashierUser == null)
                {
                    _context.Users.Add(new User
                    {
                        Email = "user@pos.com",
                        Password = PasswordHasher.HashPassword("user123"),
                        Name = "Cashier User",
                        JobTitle = "كاشير",
                        Role = UserRole.User,
                        RoleTemplateName = "كاشير",
                        IsActive = true,
                        MustChangePassword = false,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });
                }
                else
                {
                    cashierUser.IsActive = true;
                    cashierUser.MustChangePassword = false;
                    if (string.IsNullOrWhiteSpace(cashierUser.RoleTemplateName))
                        cashierUser.RoleTemplateName = "كاشير";
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                AppLogger.LogWarning($"Default users verification warning: {ex.Message}");
            }

            // 5. تنظيف ومعالجة الحركات المعلقة/اليتيمة في الصندوق من المناوبات النشطة
            try
            {
                var activeShiftIds = await _context.Shifts
                    .Where(s => s.IsActive)
                    .Select(s => s.Id)
                    .ToListAsync();

                if (activeShiftIds.Any())
                {
                    var orphaned = await _context.CashBoxTransactions
                        .Where(t => activeShiftIds.Contains(t.ShiftId) &&
                            ((t.Type == CashBoxTransactionType.Advance && t.EmployeeAdvanceId.HasValue && !_context.EmployeeAdvances.Any(a => a.Id == t.EmployeeAdvanceId.Value)) ||
                             (t.Type == CashBoxTransactionType.Debt && t.Name.Contains("سحب شخصي للموظف") && _context.CashBoxTransactions.Any(other => other.Id != t.Id && other.ShiftId == t.ShiftId && other.Amount == t.Amount && other.UserId == t.UserId && other.Type == CashBoxTransactionType.Advance))))
                        .ToListAsync();

                    if (orphaned.Count > 0)
                    {
                        _context.CashBoxTransactions.RemoveRange(orphaned);
                        await _context.SaveChangesAsync();
                        AppLogger.LogInfo($"Cleaned up {orphaned.Count} orphaned cashbox transactions on startup.");
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogWarning($"Orphaned cashbox transactions cleanup warning: {ex.Message}");
            }
        }
    }
}
