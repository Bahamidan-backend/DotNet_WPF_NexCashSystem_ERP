using System;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        string[] dbs = new string[]
        {
            @"C:\Users\AlShaheen\source\repos\PosSystemVersion55\POSSystem.WPF\poss.db",
            @"C:\Users\AlShaheen\source\repos\PosSystemVersion55\POSSystem.WPF\bin\Debug\net8.0-windows7.0\poss.db",
            @"C:\Users\AlShaheen\Desktop\nexSystem\poss.db"
        };

        foreach (var db in dbs)
        {
            Console.WriteLine($"\n=== Checking: {db} ===");
            if (!System.IO.File.Exists(db))
            {
                Console.WriteLine("File NOT found!");
                continue;
            }

            using var conn = new SqliteConnection($"Data Source={db}");
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(warehouseFromProducts);";
            using var reader = cmd.ExecuteReader();
            var cols = new System.Collections.Generic.List<string>();
            while (reader.Read())
            {
                cols.Add(reader["name"].ToString()!);
            }
            Console.WriteLine($"warehouseFromProducts columns ({cols.Count}): {string.Join(", ", cols)}");

            cmd.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory;";
            try
            {
                var migrationCount = cmd.ExecuteScalar();
                Console.WriteLine($"EF Migrations Count: {migrationCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking migrations: {ex.Message}");
            }
        }
    }
}

