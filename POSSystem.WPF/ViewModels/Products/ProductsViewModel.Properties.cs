using System;
using System.Collections.Generic;
using System.Threading;
using POSSystem.WPF.Models;
using POSSystem.WPF.Services;
using POSSystem.WPF.Shered.Proberty;

namespace POSSystem.WPF.ViewModels.Products
{
    public partial class ProductsViewModel
    {
        private readonly POSSystem.WPF.Business.Services.interfase.ICategoryService _categoryService;
        private readonly POSSystem.WPF.Business.Services.interfase.IWarehouseService _warehouseService;
        private readonly POSSystem.WPF.Business.Services.interfase.IProductService _productService;
        private readonly POSSystem.WPF.Business.Services.interfase.ISaleService _saleService;
        private readonly POSSystem.WPF.Business.Services.interfase.IOrderService _orderService;
        private readonly POSSystem.WPF.Data.UnitOfWork.IUnitOfWork _unitOfWork;
        
        private static List<Product> _allProductsCache = new List<Product>();
        private static List<WarehouseFromProduct> _warehouseCache;
        private static List<Sale> _todaySalesCache;
        private static DateTime _lastCacheUpdate = DateTime.MinValue;
        private CancellationTokenSource _summaryCts;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(2);
        private readonly SemaphoreSlim _summarySemaphore = new SemaphoreSlim(1, 1);

        public ProductViewModel productViewModel { get; set; }
        public ProductsProberty productsProberty { get; }
    }
}
