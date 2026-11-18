namespace POSSystem.WPF.Data.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(POSDbContext context) : base(context)
        {
        }

        public override async Task<Product?> GetByIdAsync(int id)
        {
            var product = await GetBaseProductQuery(asNoTracking: false)
                .Include(p => p.Recipe)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product != null)
            {
                DeduplicateSizes(new[] { product });
            }
            return product;
        }

        public async Task<Product> GetProductsByIDForRecipeAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Recipe)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new InvalidOperationException($"Product with id {id} was not found.");
        }

        /// <summary>
        /// Removes duplicate ProductSize entries and filters out deprecated quarter sizes (ربع نفر / ربع حبة / ربع ذبيحة).
        /// </summary>
        private static void DeduplicateSizes(IEnumerable<Product> products)
        {
            foreach (var p in products)
            {
                if (p.ProductSizes != null && p.ProductSizes.Any())
                {
                    var deduped = p.ProductSizes
                        .Where(s => s.SizeName != null &&
                                    !s.SizeName.Trim().Contains("ربع نفر") &&
                                    !s.SizeName.Trim().Contains("ربع ذبيحة"))
                        .GroupBy(s => s.SizeName)
                        .Select(g => g.First())
                        .ToList();
                    foreach (var s in deduped)
                    {
                        s.Product = p;
                    }
                    p.ProductSizes = deduped;
                }
            }
        }

        private IQueryable<Product> GetBaseProductQuery(bool asNoTracking = true)
        {
            IQueryable<Product> query = _dbSet;
            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            return query
                .Include(p => p.Category)
                    .ThenInclude(c => c.Kitchen)
                .Include(p => p.Kitchen)
                .Include(p => p.ProductSizes)
                .AsSplitQuery();
        }

        public async Task<IEnumerable<Product>> GetActiveProductsAsync()
        {
            var products = await GetBaseProductQuery()
                .Where(p => p.IsActive)
                .ToListAsync();

            DeduplicateSizes(products);
            return products;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            var products = await GetBaseProductQuery().ToListAsync();
            DeduplicateSizes(products);
            return products;
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
        {
            var term = searchTerm.Trim();
            return await _dbSet
                .AsNoTracking()
                .Where(p => p.IsActive &&
                    (EF.Functions.Like(p.Name, $"%{term}%") ||
                     (p.Description != null && EF.Functions.Like(p.Description, $"%{term}%"))))
                .ToListAsync();
        }

        public async Task UpdateStockAsync(int productId, int newStock)
        {
            var product = await _dbSet.FirstOrDefaultAsync(p => p.Id == productId);
            if (product != null)
            {
                decimal factor = product.PersonNumberFood > 0 ? product.PersonNumberFood : 1m;
                product.AvailableQtyInPOS = newStock * factor;
                product.StockQuantity = newStock;
                product.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetTotalProductForHederAsync()
        {
            return await _context.Products.CountAsync();
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold = 10)
        {
            var rawActiveProducts = await _dbSet
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.TypeWarehoseFromProduct != Product.UntrackedStockMarker && p.AvailableQtyInPOS <= threshold)
                .ToListAsync();
            var activeProducts = rawActiveProducts
                .Where(p => !p.IsUntrackedStock)
                .OrderBy(p => p.AvailableQtyInPOS)
                .ToList();

            foreach (var p in activeProducts)
            {
                p.StockQuantity = p.PersonNumberFood > 0 ? p.AvailableQtyInPOS / p.PersonNumberFood : p.AvailableQtyInPOS;
            }

            return activeProducts;
        }

        public async Task<int> GetLowStockProductsForHederAsync(int threshold = 10)
        {
            return await _dbSet
                .AsNoTracking()
                .CountAsync(p => p.IsActive && p.TypeWarehoseFromProduct != Product.UntrackedStockMarker && p.AvailableQtyInPOS > 0 && p.AvailableQtyInPOS <= threshold);
        }

        public async Task<int> GetOutOfStockProductsForHederAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .CountAsync(p => p.IsActive && p.TypeWarehoseFromProduct != Product.UntrackedStockMarker && p.AvailableQtyInPOS <= 0);
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.CategoryId == categoryId && p.IsActive)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalInventoryValueAsync()
        {
            var pricesAndQty = await _dbSet
                .AsNoTracking()
                .Where(p => p.IsActive)
                .Select(p => new { p.Price, p.AvailableQtyInPOS })
                .ToListAsync();
            return pricesAndQty.Sum(p => p.Price * p.AvailableQtyInPOS);
        }

        public async Task<decimal> GetTotalStockQuantityForHederAsync()
        {
            var quantities = await _dbSet
                .AsNoTracking()
                .Where(p => p.IsActive)
                .Select(p => p.AvailableQtyInPOS)
                .ToListAsync();
            return quantities.Sum();
        }

        public async Task<IEnumerable<Product>> GetProductsWithLowStockAsync(int threshold = 10)
        {
            return await GetLowStockProductsAsync(threshold);
        }

        public async Task<PagedResult<Product>> GetProductsPagedAsync(int pageNumber, int pageSize)
        {
            var totalCount = await _dbSet.AsNoTracking().CountAsync();
            var items = await GetBaseProductQuery()
                .OrderBy(p => p.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            DeduplicateSizes(items);
            return new PagedResult<Product>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<PagedResult<Product>> SearchProductsPagedAsync(string searchTerm, int pageNumber, int pageSize)
        {
            var term = searchTerm.Trim();
            var query = _dbSet
                .AsNoTracking()
                .Where(p => p.IsActive &&
                    (EF.Functions.Like(p.Name, $"%{term}%") ||
                     (p.Description != null && EF.Functions.Like(p.Description, $"%{term}%")) ||
                     (p.Category != null && EF.Functions.Like(p.Category.Name, $"%{term}%"))));

            var totalCount = await query.CountAsync();
            var items = await query
                .Include(p => p.Category)
                    .ThenInclude(c => c.Kitchen)
                .Include(p => p.Kitchen)
                .Include(p => p.ProductSizes)
                .AsSplitQuery()
                .OrderBy(p => p.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            DeduplicateSizes(items);
            return new PagedResult<Product>(items, totalCount, pageNumber, pageSize);
        }

        public async Task ApplyBulkDiscountAsync(decimal percentage)
        {
            decimal factor = 1m - (percentage / 100m);
            if (factor < 0m) factor = 0m;

            var products = await _context.Products.Include(p => p.ProductSizes).Where(p => p.IsActive).ToListAsync();
            foreach (var p in products)
            {
                if (p.BasePrice <= 0) p.BasePrice = p.Price;
                p.Price = Math.Round(p.BasePrice * factor, 2);
                if (p.ProductSizes != null)
                {
                    foreach (var s in p.ProductSizes)
                    {
                        if (s.BasePrice <= 0) s.BasePrice = s.Price;
                        s.Price = Math.Round(s.BasePrice * factor, 2);
                    }
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task RevertBulkDiscountAsync()
        {
            var products = await _context.Products.Include(p => p.ProductSizes).Where(p => p.IsActive).ToListAsync();
            foreach (var p in products)
            {
                if (p.BasePrice > 0) p.Price = p.BasePrice;
                if (p.ProductSizes != null)
                {
                    foreach (var s in p.ProductSizes)
                    {
                        if (s.BasePrice > 0) s.Price = s.BasePrice;
                    }
                }
            }
            await _context.SaveChangesAsync();
        }
    }
}
