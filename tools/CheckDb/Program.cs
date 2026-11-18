using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Linq;

var allDbFiles = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "..", ".."), "poss.db", SearchOption.AllDirectories);

if (allDbFiles.Length == 0)
{
    Console.WriteLine("No poss.db files found anywhere.");
    return 1;
}

foreach (var dbPath in allDbFiles.Select(Path.GetFullPath).Distinct())
{
    Console.WriteLine($"--- Processing Database: {dbPath} ---");
    try 
    {
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        Console.WriteLine("Categories table info:");
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA table_info('Categories');";
            using var reader = cmd.ExecuteReader();
            var hasIsSub = false;
            while (reader.Read())
            {
                var name = reader.GetString(1);
                if (string.Equals(name, "IsSubCategory", StringComparison.OrdinalIgnoreCase)) hasIsSub = true;
            }

            if (!hasIsSub)
            {
                Console.WriteLine("Column IsSubCategory not found — adding it now...");
                using var addCmd = conn.CreateCommand();
                addCmd.CommandText = "ALTER TABLE Categories ADD COLUMN IsSubCategory INTEGER NOT NULL DEFAULT 0;";
                addCmd.ExecuteNonQuery();
                Console.WriteLine("Column added.");
            }
        }

        Console.WriteLine("AnimalBatches table info:");
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA table_info('AnimalBatches');";
            using (var reader = cmd.ExecuteReader())
            {
                var hasIsWeightMode = false;
                while (reader.Read())
                {
                    var name = reader.GetString(1);
                    if (string.Equals(name, "IsWeightMode", StringComparison.OrdinalIgnoreCase)) hasIsWeightMode = true;
                }

                if (!hasIsWeightMode)
                {
                    Console.WriteLine("Column IsWeightMode not found — adding it now...");
                    using var addCmd = conn.CreateCommand();
                    addCmd.CommandText = "ALTER TABLE AnimalBatches ADD COLUMN IsWeightMode INTEGER NOT NULL DEFAULT 1;";
                    addCmd.ExecuteNonQuery();
                    Console.WriteLine("Column IsWeightMode added.");
                }
                else
                {
                    Console.WriteLine("Column IsWeightMode already exists.");
                }
            }
        }

        Console.WriteLine("warehouseFromProducts table info:");
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA table_info('warehouseFromProducts');";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var name = reader.GetString(1);
                var type = reader.GetString(2);
                Console.WriteLine($"\t{name} ({type})");
            }
        }

        Console.WriteLine("Repairing WarehouseFromProduct units...");
        using (var repairCmd = conn.CreateCommand())
        {
            // Set Heads to Piece (5)
            repairCmd.CommandText = "UPDATE warehouseFromProducts SET PortionUnit = 5 WHERE Name LIKE '%رأس%' AND PortionUnit IS NULL;";
            int headsCount = repairCmd.ExecuteNonQuery();
            
            // Set Meat (Thumn) to Thumn (9)
            repairCmd.CommandText = "UPDATE warehouseFromProducts SET PortionUnit = 9 WHERE Name LIKE '%ثمن%' AND PortionUnit IS NULL;";
            int thumnCount = repairCmd.ExecuteNonQuery();

            // Set Others (Liver, Back, Meat kg) to KG (6)
            repairCmd.CommandText = "UPDATE warehouseFromProducts SET PortionUnit = 6 WHERE (Name LIKE '%كبدة%' OR Name LIKE '%ظهر%' OR Name LIKE '%كجم%') AND PortionUnit IS NULL;";
            int kgCount = repairCmd.ExecuteNonQuery();

            Console.WriteLine($"Repaired: {headsCount} heads, {thumnCount} thumns, {kgCount} others.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing {dbPath}: {ex.Message}");
    }
}

return 0;