using System;

namespace POSSystem.WPF.Services
{
    [Obsolete("This service is deprecated. Use IOrderService, IProductService, etc. via ServiceLocator instead.")]
    public class DatabaseService : IDisposable
    {
        public DatabaseService()
        {
        }

        public void Dispose()
        {
        }
    }
}
