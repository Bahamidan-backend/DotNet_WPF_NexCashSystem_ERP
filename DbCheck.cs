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
                    var products = context.Products.ToList();
                    Console.WriteLine("--- Products ---");
                    foreach (var p in products)
                    {
                        Console.WriteLine($"ID: {p.Id}, Name: {p.Name}, StockQty: {p.StockQuantity}, TotalValue: {p.TotalValue}, Price: {p.Price}, BasePrice: {p.BasePrice}, PersonNumberFood: {p.PersonNumberFood}, TypeWarehoseFromProduct: {p.TypeWarehoseFromProduct}");
                    }

                    var wp = context.warehouseFromProducts.ToList();
                    Console.WriteLine("\n--- WarehouseFromProduct ---");
                    foreach (var item in wp)
                    {
                        Console.WriteLine($"ID: {item.Id}, Name: {item.Name}, Price: {item.Price}, StockQty: {item.StockQuantity}");
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
