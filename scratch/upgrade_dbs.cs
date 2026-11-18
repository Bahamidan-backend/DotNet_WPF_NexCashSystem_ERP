using System;
using Microsoft.Data.Sqlite;

string[] dbs = new[] {
    @"C:\Users\AlShaheen\Desktop\nexSystem\poss.db",
    @"c:\Users\AlShaheen\source\repos\PosSystemVersion55\POSSystem.WPF\poss.db",
    @"c:\Users\AlShaheen\source\repos\PosSystemVersion55\POSSystem.WPF\bin\Debug\net8.0-windows7.0\poss.db"
};

foreach (var db in dbs)
{
    if (!System.IO.File.Exists(db)) continue;
    Console.WriteLine($"Updating DB: {db}");
    using var conn = new SqliteConnection($"Data Source={db}");
    conn.Open();

    void Exec(string sql) {
        try {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
            Console.WriteLine($"  [OK] {sql.Substring(0, Math.Min(sql.Length, 40))}...");
        } catch (Exception ex) {
            Console.WriteLine($"  [SKIP/ERR] {ex.Message}");
        }
    }

    Exec("ALTER TABLE warehouseFromProducts ADD COLUMN MinimumStockLevel TEXT NOT NULL DEFAULT '5';");
    Exec("ALTER TABLE Expenses ADD COLUMN PaymentMethod INTEGER NOT NULL DEFAULT 0;");
    Exec(@"CREATE TABLE IF NOT EXISTS StockMovements (
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
    );");
    Exec("CREATE INDEX IF NOT EXISTS IX_StockMovements_WarehouseFromProductId ON StockMovements (WarehouseFromProductId);");
    Exec("CREATE INDEX IF NOT EXISTS IX_StockMovements_MovementDate ON StockMovements (MovementDate);");
}
Console.WriteLine("All DBs processed!");
