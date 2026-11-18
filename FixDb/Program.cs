using System;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        string dbPath = @"c:\Users\AlShaheen\source\repos\PosSystemVersion3\POSSystem.WPF\poss.db";
        string connectionString = $"Data Source={dbPath};Foreign Keys=False;";

        try
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory;";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine(reader.GetString(0));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
