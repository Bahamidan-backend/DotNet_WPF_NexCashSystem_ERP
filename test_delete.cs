using POSSystem.WPF.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                using var context = new POSDbContext();
                var addon = context.Addons.FirstOrDefault();
                if (addon != null) {
                    addon.IsActive = false;
                    context.Addons.Update(addon);
                    context.SaveChanges();
                    Console.WriteLine("Success: Addon soft deleted.");
                } else {
                    Console.WriteLine("No addons found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine("INNER: " + ex.InnerException.Message);
                }
            }
        }
    }
}
