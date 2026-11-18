using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Helpers;

namespace POSSystem.WPF.Business.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _unitOfWork.Products.GetAllProductsAsync();
        }

        public async Task<IEnumerable<Product>> GetActiveProductsAsync()
        {
            return await _unitOfWork.Products.GetActiveProductsAsync();
        }


        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _unitOfWork.Products.GetByIdAsync(id);
        }


        public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetActiveProductsAsync();

            return await _unitOfWork.Products.SearchProductsAsync(searchTerm);
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            if (!await ValidateProductAsync(product))
                throw new ArgumentException("Invalid product data");

            var now = DateTime.Now;
            product.CreatedAt = now;
            product.UpdatedAt = now;

            // فصل الكائنات الملاحية والاكتفاء بالـ Foreign Keys لتسريع الحفظ ومنع التعارض
            _unitOfWork.ClearEntityTracking();
            var tempCat = product.Category;
            var tempKitchen = product.Kitchen;
            product.Category = null;
            product.Kitchen = null;

            if (product.ProductSizes != null && product.ProductSizes.Any())
            {
                var dedupedSizes = product.ProductSizes
                    .Where(ps => !string.IsNullOrWhiteSpace(ps.SizeName))
                    .GroupBy(ps => ps.SizeName.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.Last())
                    .ToList();

                foreach (var ps in dedupedSizes)
                {
                    ps.Id = 0;
                    ps.Product = null!;
                    ps.SizeName = ps.SizeName.Trim();
                    ps.Name = ps.SizeName;
                    ps.BasePrice = ps.BasePrice > 0 ? ps.BasePrice : ps.Price;
                    ps.IsActive = true;
                    ps.CreatedAt = now;
                    ps.UpdatedAt = now;
                }
                product.ProductSizes = dedupedSizes;
            }

            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();
            _unitOfWork.ClearEntityTracking();
            POSSystem.WPF.Business.ServiceLocator.NotifyProductsChanged();

            product.Category = tempCat;
            product.Kitchen = tempKitchen;

            return product;
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            if (!await ValidateProductAsync(product))
                throw new ArgumentException("Invalid product data");

            var now = DateTime.Now;
            product.UpdatedAt = now;

            // Extract and deduplicate target sizes BEFORE touching ChangeTracker
            var targetSizes = (product.ProductSizes ?? Enumerable.Empty<ProductSize>())
                .Where(ts => !string.IsNullOrWhiteSpace(ts.SizeName) && ts.Price > 0)
                .GroupBy(ts => ts.SizeName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.Last())
                .ToList();
            var tempCat = product.Category;
            var tempKitchen = product.Kitchen;
            product.ProductSizes = new List<ProductSize>();
            product.Category = null!;
            product.Kitchen = null;

            _unitOfWork.ClearEntityTracking();
            var context = _unitOfWork.GetContext();

            var existing = await context.Products.FirstOrDefaultAsync(p => p.Id == product.Id);
            if (existing != null)
            {
                existing.Name = product.Name;
                existing.Description = product.Description;
                existing.Price = product.Price;
                existing.BasePrice = product.BasePrice > 0 ? product.BasePrice : product.Price;
                existing.CategoryId = product.CategoryId;
                existing.CategoryName = product.CategoryName;
                existing.KitchenId = product.KitchenId;
                existing.ImagePath = product.ImagePath;
                existing.portionUnit = product.portionUnit;
                existing.TypeWarehoseFromProduct = product.TypeWarehoseFromProduct;
                existing.IsAnimalComponent = product.IsAnimalComponent;
                existing.AnimalComponentType = product.AnimalComponentType;
                if (product.YieldRate > 0)
                    existing.YieldRate = product.YieldRate;
                existing.IsActive = product.IsActive;
                existing.UpdatedAt = now;
            }
            else
            {
                context.Products.Update(product);
            }

            // Only modify ProductSizes when targetSizes has explicit size entries (prevents metadata/recipe updates from wiping sizes)
            if (targetSizes.Any(ts => !string.IsNullOrWhiteSpace(ts.SizeName) && ts.Price > 0))
            {
                var existingSizes = await context.ProductSizes
                    .Where(ps => ps.ProductId == product.Id)
                    .ToListAsync();

                var targetNames = targetSizes
                    .Where(ts => !string.IsNullOrWhiteSpace(ts.SizeName) && ts.Price > 0)
                    .Select(ts => ts.SizeName.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var toDelete = existingSizes
                    .Where(es => !targetNames.Contains(es.SizeName.Trim()))
                    .ToList();
                if (toDelete.Any())
                {
                    context.ProductSizes.RemoveRange(toDelete);
                }

                foreach (var targetSize in targetSizes)
                {
                    if (string.IsNullOrWhiteSpace(targetSize.SizeName) || targetSize.Price <= 0) continue;

                    var existingSize = existingSizes.FirstOrDefault(es =>
                        es.SizeName.Trim().Equals(targetSize.SizeName.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (existingSize != null)
                    {
                        existingSize.Price = targetSize.Price;
                        existingSize.BasePrice = targetSize.BasePrice > 0 ? targetSize.BasePrice : targetSize.Price;
                        existingSize.Name = targetSize.SizeName.Trim();
                        existingSize.IsActive = true;
                        existingSize.UpdatedAt = now;
                    }
                    else
                    {
                        context.ProductSizes.Add(new ProductSize
                        {
                            ProductId = product.Id,
                            Name = targetSize.SizeName.Trim(),
                            SizeName = targetSize.SizeName.Trim(),
                            Price = targetSize.Price,
                            BasePrice = targetSize.BasePrice > 0 ? targetSize.BasePrice : targetSize.Price,
                            IsActive = true,
                            CreatedAt = now,
                            UpdatedAt = now
                        });
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
            _unitOfWork.ClearEntityTracking();

            var savedSizes = await context.ProductSizes
                .AsNoTracking()
                .Where(ps => ps.ProductId == product.Id)
                .ToListAsync();
            foreach (var s in savedSizes)
            {
                s.Product = product;
            }
            product.ProductSizes = savedSizes;
            product.Category = tempCat!;
            product.Kitchen = tempKitchen;

            POSSystem.WPF.Business.ServiceLocator.NotifyProductsChanged();
            return product;
        }

        public async Task DeleteProductAsync(int productId)
        {
            var context = _unitOfWork.GetContext();
            int affected = await context.Products
                .Where(p => p.Id == productId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.IsActive, false)
                    .SetProperty(p => p.UpdatedAt, DateTime.Now));

            if (affected == 0)
                throw new ArgumentException("Product not found");

            _unitOfWork.ClearEntityTracking();
            POSSystem.WPF.Business.ServiceLocator.NotifyProductsChanged();
        }

        public async Task UpdateStockAsync(int productId, int newStock)
        {
            if (newStock < 0)
                throw new ArgumentException("Stock quantity cannot be negative");

            await _unitOfWork.Products.UpdateStockAsync(productId, newStock);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold = 10)
        {
            return await _unitOfWork.Products.GetLowStockProductsAsync(threshold);
        }

        public async Task<bool> ValidateProductAsync(Product product)
        {
            if (product == null) return false;
            if (string.IsNullOrWhiteSpace(product.Name)) return false;
            if (product.Price <= 0) return false;
            if (product.StockQuantity < 0) return false;

            var context = _unitOfWork.GetContext();
            bool exists = await context.Products.AnyAsync(p => p.Name == product.Name && p.Id != product.Id && p.IsActive);
            if (exists) return false;

            return true;
        }





        public async Task<int> UpdateProduct(Product product)
        {
            await UpdateProductAsync(product);
            return 1;
        }


        public async Task<int> DeleteProduct(Product product)
        {
            await DeleteProductAsync(product.Id);
            return 1;
        }

        public async Task ApplyBulkDiscountAsync(decimal percentage)
        {
            await _unitOfWork.Products.ApplyBulkDiscountAsync(percentage);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RevertBulkDiscountAsync()
        {
            await _unitOfWork.Products.RevertBulkDiscountAsync();
            await _unitOfWork.SaveChangesAsync();
        }
        public async Task<ServiceResult<int>> RefillAllOutOfStockAsync()
        {
            var products = await _unitOfWork.Products.GetActiveProductsAsync();
            var outOfStockProducts = products.Where(p => p.TotalValue == 0).ToList();
            
            if (!outOfStockProducts.Any())
                return ServiceResult<int>.Ok(0, "لا توجد منتجات منتهية للتعبئة.");

            int refilledCount = 0;
            var messages = new List<string>();
            var allWarehouseItemsList = await _unitOfWork.warehouseFromProduct.GetAllAsync();
            
            // Create a fast O(1) dictionary lookup for warehouse items by name and by ID
            var warehouseItemsDict = allWarehouseItemsList
                .Where(w => !string.IsNullOrWhiteSpace(w.Name))
                .GroupBy(w => w.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.FirstOrDefault(), StringComparer.OrdinalIgnoreCase);
            var warehouseItemsById = allWarehouseItemsList.ToDictionary(x => x.Id);
            
            // OPTIMIZATION: Fetch ONLY recipes for out-of-stock products to save memory & DB load
            var outOfStockIds = outOfStockProducts.Select(p => p.Id).ToList();
            var context = _unitOfWork.GetContext();
            var allRecipes = await context.Recipes
                .AsNoTracking()
                .Where(r => outOfStockIds.Contains(r.ProductId))
                .ToListAsync();
            var recipesByProductId = allRecipes.ToLookup(r => r.ProductId);
            var now = DateTime.Now;

            foreach (var product in outOfStockProducts)
            {
                // Logging intentionally removed from production code

                // 3. Get recipes (ingredients) for this product
                var productRecipes = recipesByProductId[product.Id].ToList();
                
                bool refilled = false;
                bool canRefill = true;
                var pendingDeductions = new List<(WarehouseFromProduct item, decimal amount)>();
                var debugSb = new System.Text.StringBuilder();
                
                // Auto-fill: Use LastCookedQty if available, otherwise default to 1
                decimal cookedQty = product.LastCookedQty > 0 ? product.LastCookedQty : 1m;

                bool hasRecipes = productRecipes.Any();
                bool hasDirectMapping = !string.IsNullOrWhiteSpace(product.TypeWarehoseFromProduct);

                if (!hasRecipes && !hasDirectMapping)
                {
                    // No recipe or warehouse mapping — skip this product
                    continue;
                }

                // 4A. Check Recipe Ingredients - use LastCookedQty for auto-fill
                if (hasRecipes)
                {

                    foreach (var recipe in productRecipes)
                    {
                        warehouseItemsDict.TryGetValue(recipe.Name.Trim(), out var warehouseProduct);
                            
                        if (warehouseProduct == null)
                        {
                            canRefill = false;
                            break;
                        }

                        decimal requiredQty = UnitConversionHelper.ConvertQuantity(recipe.Quantity * cookedQty, recipe.portionUnit, warehouseProduct.PortionUnit);
                        if (warehouseProduct.StockQuantity < requiredQty)
                        {
                            canRefill = false;
                            break;
                        }
                        pendingDeductions.Add((warehouseProduct, requiredQty));
                    }
                }

                // 4B. Check Direct Warehouse Mapping - use LastCookedQty for auto-fill
                if (canRefill && hasDirectMapping)
                {

                    warehouseItemsDict.TryGetValue(product.TypeWarehoseFromProduct.Trim(), out var warehouseProduct);
                    
                    if (warehouseProduct == null && int.TryParse(product.TypeWarehoseFromProduct.Trim(), out int wpId))
                    {
                        warehouseItemsById.TryGetValue(wpId, out warehouseProduct);
                    }

                    if (warehouseProduct != null && warehouseProduct.StockQuantity >= cookedQty)
                    {
                        // Check if we didn't already add this exact warehouse item from recipes
                        // If we did, we add to its amount
                        var existing = pendingDeductions.FirstOrDefault(x => x.item.Id == warehouseProduct.Id);
                        if (existing.item != null)
                        {
                            if (warehouseProduct.StockQuantity >= (existing.amount + cookedQty))
                            {
                                pendingDeductions.Remove(existing);
                                pendingDeductions.Add((warehouseProduct, existing.amount + cookedQty));
                            }
                            else
                            {
                                canRefill = false;
                            }
                        }
                        else
                        {
                            pendingDeductions.Add((warehouseProduct, cookedQty));
                        }
                    }
                    else
                    {

                        canRefill = false;
                    }
                }

                // 5. Apply Deductions if all checks passed and there is at least one deduction
                if (canRefill && pendingDeductions.Any())
                {
                    foreach (var (wp, amount) in pendingDeductions)
                    {
                        var oldStock = wp.StockQuantity;
                        wp.StockQuantity -= amount;
                        _unitOfWork.warehouseFromProduct.UpdateWarehouseFromProducts(wp);
                        debugSb.AppendLine($"- {wp.Name} (ID: {wp.Id}): {oldStock} -> {wp.StockQuantity} (خصم: {amount})");
                    }
                    refilled = true;
                    
                    messages.Add($"تم تعبئة المنتج: {product.Name}\nتم خصم العناصر التالية من المخزن (بكمية الطبخ السابقة: {cookedQty}):\n{debugSb}");
                }
                else if (hasRecipes || hasDirectMapping)
                {
                    messages.Add($"فشلت تعبئة المنتج: {product.Name}\nإما لعدم توفر مخزون كافٍ أو لعدم العثور على المكونات المرتبطة.");
                }

                if (refilled)
                {
                    // 6. Refill Product using LastCookedQty (previous cooked quantity) for auto-fill
                    int yieldRate = product.YieldRate > 0 ? product.YieldRate : 100;
                    
                    // Calculate the total portions based on LastCookedQty and YieldRate
                    decimal totalPortions = cookedQty * yieldRate;
                    
                    product.StockQuantity = cookedQty;
                    product.AvailableQtyInPOS = totalPortions;
                    product.TotalValue = totalPortions;
                    product.UpdatedAt = now;
                    _unitOfWork.Products.Update(product);
                    
                    refilledCount++;
                }
            }

            if (refilledCount > 0)
            {
                await _unitOfWork.SaveChangesAsync();
            }

            var resultMessage = messages.Any() ? string.Join("\n\n", messages) : null;
            return ServiceResult<int>.Ok(refilledCount, resultMessage);
        }
        public async Task<bool> DeductStockAsync(int productId, string size, decimal quantity)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null) return false;

            // Use PortionSizeHelper to avoid duplicating size logic that also exists in OrderService
            decimal multiplier = PortionSizeHelper.GetSizeMultiplier(size?.Trim() ?? string.Empty);
            decimal portionDeduction = quantity * multiplier;
            decimal physicalDeduction = product.PersonNumberFood > 0
                ? portionDeduction / product.PersonNumberFood
                : portionDeduction;

            // Sync TotalValue (Portions) and StockQuantity (Physical Units)
            // If the product uses portions (TotalValue > 0), we deduct from TotalValue
            if (product.TotalValue > 0)
            {
                if (product.TotalValue < portionDeduction) return false;
                
                product.TotalValue -= portionDeduction;
                // Update StockQuantity to match remaining TotalValue
                if (product.PersonNumberFood > 0)
                {
                    product.StockQuantity = product.TotalValue / product.PersonNumberFood;
                }
                else
                {
                    product.StockQuantity = product.TotalValue;
                }
            }
            else
            {
                // Fallback for non-portional products
                if (product.StockQuantity < physicalDeduction) return false;
                product.StockQuantity -= physicalDeduction;
            }

            product.UpdatedAt = DateTime.Now;
            _unitOfWork.Products.Update(product);

            // AUTOMATIC SYNC: If the product is an animal component, deduct from Animal Batches too
            if (product.IsAnimalComponent && product.AnimalComponentType.HasValue)
            {
                try
                {
                    var animalBatchService = ServiceLocator.AnimalBatchService;
                    // For animal batches, we usually deduct the physical weight or unit count
                    await animalBatchService.ConsumeFromAnyBatchAsync(product.AnimalComponentType.Value, physicalDeduction);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error syncing with animal batches during sale deduction: {ex.Message}");
                }
            }

            return true;
        }

        public async Task UpdateProductRecipeStock(POSSystem.WPF.Models.DTOs.UpdateRecipeDto dto)
        {
            var batchService = new KitchenCookingBatchService(_unitOfWork);
            await batchService.CreateBatchAsync(new POSSystem.WPF.Models.DTOs.CreateCookingBatchDto
            {
                ProductId = dto.ProductId,
                WarehouseId = 1,
                MainIngredientQuantity = dto.NewQty,
                ActualPortions = dto.ExpectedYieldRate > 0 ? dto.NewQty * dto.ExpectedYieldRate : 0
            });
        }

        public async Task<Product?> IncreaseProductQuantityAsync(int productId, decimal addedQuantity, string note)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
                return null;

            product.PersonNumberFood += addedQuantity;
            product.TotalValue = product.StockQuantity * product.PersonNumberFood;
            product.UpdatedAt = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(note))
            {
                product.Description = (product.Description ?? "") + $"\n[تعديل كاشير: +{addedQuantity} - الملاحظة: {note}]";
            }

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            return product;
        }
    }
}
