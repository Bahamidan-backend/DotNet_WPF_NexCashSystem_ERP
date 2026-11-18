using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Data;
using POSSystem.WPF.Models;

namespace DbCheck
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                using (var context = new POSDbContext())
                {
                    var conn = context.Database.GetDbConnection();
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA table_info(PurchaseItems);";
                        using (var reader = cmd.ExecuteReader())
                        {
                            Console.WriteLine("Columns in PurchaseItems:");
                            while (reader.Read())
                            {
                                Console.WriteLine($"- {reader["name"]} ({reader["type"]})");
                            }
                        }
                    }

                    var purchases = context.Purchases.Include(p => p.PurchaseItems).OrderByDescending(p => p.Id).Take(5).ToList();
                    Console.WriteLine("\n--- Recent Purchases ---");
                    foreach (var p in purchases)
                    {
                        Console.WriteLine($"Purchase ID: {p.Id}, Num: {p.PurchaseNumber}, Total: {p.TotalAmount}");
                        foreach (var item in p.PurchaseItems)
                        {
                            Console.WriteLine($"  Item ID: {item.Id}, ProductId: {item.ProductId}, AddonId: {item.AddonId}, Price: {item.UnitPrice}");
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
}
