const fs = require('fs');
let s = fs.readFileSync('DbCheck/Program.cs', 'utf8');

const target = '// MODULE 5: ORDER CANCELLATIONS & SALES RETURNS (1,000 TESTS)';

const testModuleCode = `// MODULE 4B: KITCHEN COOKING BATCHES (BATCH COOKING, POT SCALING AND OVERRIDES)
            // =========================================================================
            Console.WriteLine("\\n--> Executing Module 4B: Kitchen Cooking Batches and Pot Scaling (Custom Quantities and POS Portions)...");
            var batchService = new KitchenCookingBatchService(unitOfWork);

            // 1. Setup dish with standard recipe
            var dishRice = await context.warehouseFromProducts.FirstOrDefaultAsync(w => w.Name == "أرز بشاور هندي كيس 40كجم")
                ?? (await unitOfWork.warehouseFromProduct.CreateAsync(new WarehouseFromProduct
                {
                    Name = "أرز بشاور هندي كيس 40كجم",
                    StockQuantity = 50m,
                    PortionUnit = Recipe.PortionUnit.Bag,
                    Price = 280m,
                    WarehouseId = 1,
                    IsActive = true
                }));

            var dishOil = await context.warehouseFromProducts.FirstOrDefaultAsync(w => w.Name == "زيت طبخ نباتي 1لتر")
                ?? (await unitOfWork.warehouseFromProduct.CreateAsync(new WarehouseFromProduct
                {
                    Name = "زيت طبخ نباتي 1لتر",
                    StockQuantity = 100m,
                    PortionUnit = Recipe.PortionUnit.Liter,
                    Price = 12m,
                    WarehouseId = 1,
                    IsActive = true
                }));

            var dishSpices = await context.warehouseFromProducts.FirstOrDefaultAsync(w => w.Name == "بهارات مشكلة مطحونة كجم")
                ?? (await unitOfWork.warehouseFromProduct.CreateAsync(new WarehouseFromProduct
                {
                    Name = "بهارات مشكلة مطحونة كجم",
                    StockQuantity = 20m,
                    PortionUnit = Recipe.PortionUnit.Kg,
                    Price = 45m,
                    WarehouseId = 1,
                    IsActive = true
                }));

            dishRice.StockQuantity = 50m;
            dishOil.StockQuantity = 100m;
            dishSpices.StockQuantity = 20m;
            await context.SaveChangesAsync();

            var mandiProduct = await context.Products.Include(p => p.Recipe).FirstOrDefaultAsync(p => p.Name == "نفر رز مندي قدر صباحي")
                ?? new Product
                {
                    Name = "نفر رز مندي قدر صباحي",
                    Price = 16m,
                    BasePrice = 6m,
                    YieldRate = 120,
                    AvailableQtyInPOS = 0,
                    CategoryId = posProduct.CategoryId,
                    portionUnit = Recipe.PortionUnit.Piece
                };

            if (mandiProduct.Id == 0)
            {
                await context.Products.AddAsync(mandiProduct);
                await context.SaveChangesAsync();
            }

            mandiProduct.AvailableQtyInPOS = 0;
            mandiProduct.YieldRate = 120;

            // Define standard recipe without touching warehouse stock
            var mandiRecipes = await context.Recipes.Where(r => r.ProductId == mandiProduct.Id).ToListAsync();
            context.Recipes.RemoveRange(mandiRecipes);
            await context.SaveChangesAsync();

            await context.Recipes.AddRangeAsync(
                new Recipe { ProductId = mandiProduct.Id, WarehouseFromProductId = dishRice.Id, Name = dishRice.Name, Quantity = 1m, portionUnit = Recipe.PortionUnit.Bag, IsMainIngredient = true },
                new Recipe { ProductId = mandiProduct.Id, WarehouseFromProductId = dishOil.Id, Name = dishOil.Name, Quantity = 2m, portionUnit = Recipe.PortionUnit.Liter, IsMainIngredient = false },
                new Recipe { ProductId = mandiProduct.Id, WarehouseFromProductId = dishSpices.Id, Name = dishSpices.Name, Quantity = 0.5m, portionUnit = Recipe.PortionUnit.Kg, IsMainIngredient = false }
            );
            await context.SaveChangesAsync();

            // Verify raw stock was NOT touched when defining recipe
            var rawRiceAfterDef = await context.warehouseFromProducts.AsNoTracking().FirstAsync(w => w.Id == dishRice.Id);
            Assert(rawRiceAfterDef.StockQuantity == 50m, "Recipe Definition Does Not Touch Raw Warehouse Stock");

            // Test 4B.1: Morning Pot Batch (1.0 Bag = 120 portions)
            try
            {
                var batch1 = await batchService.CreateBatchAsync(new POSSystem.WPF.Models.DTOs.CreateCookingBatchDto
                {
                    ProductId = mandiProduct.Id,
                    WarehouseId = 1,
                    BatchMultiplier = 1.0m,
                    MainIngredientQuantity = 1.0m,
                    ActualPortions = 120m,
                    CookedByUserId = adminUser.Id,
                    Notes = "أمر طبخ قدر الصباح رقم 1"
                });

                var rice1 = await context.warehouseFromProducts.AsNoTracking().FirstAsync(w => w.Id == dishRice.Id);
                var oil1 = await context.warehouseFromProducts.AsNoTracking().FirstAsync(w => w.Id == dishOil.Id);
                var spices1 = await context.warehouseFromProducts.AsNoTracking().FirstAsync(w => w.Id == dishSpices.Id);
                var prod1 = await context.Products.AsNoTracking().FirstAsync(p => p.Id == mandiProduct.Id);

                Assert(batch1 != null && batch1.BatchNumber.StartsWith("COOK-"), "Batch 1 Created Successfully");
                Assert(rice1.StockQuantity == 49m, "Batch 1: Rice decremented by exactly 1 bag", $"Expected 49, got {rice1.StockQuantity}");
                Assert(oil1.StockQuantity == 98m, "Batch 1: Oil decremented by 2 liters", $"Expected 98, got {oil1.StockQuantity}");
                Assert(spices1.StockQuantity == 19.5m, "Batch 1: Spices decremented by 0.5 kg", $"Expected 19.5, got {spices1.StockQuantity}");
                Assert(prod1.AvailableQtyInPOS == 120m, "Batch 1: POS portions increased to 120", $"Expected 120, got {prod1.AvailableQtyInPOS}");
            }
            catch (Exception ex)
            {
                RecordException("Cooking Batch 1 (Morning Pot)", ex);
            }

            // Test 4B.2: Selling 10 portions in POS (Verify NO double deduction of rice, oil, spices)
            try
            {
                var saleOrder = new Order
                {
                    OrderNumber = "COOK-POS-SALE-001",
                    UserId = cashierUser.Id,
                    PaymentMethod = PaymentMethod.Cash,
                    Status = OrderStatus.Completed,
                    SubTotal = 160m,
                    Total = 160m,
                    CreatedAt = DateTime.Now
                };
                saleOrder.OrderItems.Add(new OrderItem
                {
                    ProductId = mandiProduct.Id,
                    Quantity = 10,
                    UnitPrice = 16m,
                    TotalPrice = 160m
                });
                await orderService.PlaceOrderTransactionAsync(saleOrder, PaymentMethod.Cash, cashierUser, activeShift);

                var riceAfterSale = await context.warehouseFromProducts.AsNoTracking().FirstAsync(w => w.Id == dishRice.Id);
                var oilAfterSale = await context.warehouseFromProducts.AsNoTracking().FirstAsync(w => w.Id == dishOil.Id);
                var spicesAfterSale = await context.warehouseFromProducts.AsNoTracking().FirstAsync(w => w.Id == dishSpices.Id);
                var prodAfterSale = await context.Products.AsNoTracking().FirstAsync(p => p.Id == mandiProduct.Id);

                Assert(prodAfterSale.AvailableQtyInPOS == 110m, "POS Sale: Portions reduced from 120 to 110", $"Expected 110, got {prodAfterSale.AvailableQtyInPOS}");
                Assert(riceAfterSale.StockQuantity == 49m, "POS Sale: NO double deduction on Rice (still 49)");
                Assert(oilAfterSale.StockQuantity == 98m, "POS Sale: NO double deduction on Oil (still 98)");
                Assert(spicesAfterSale.StockQuantity == 19.5m, "POS Sale: NO double deduction on Spices (still 19.5)");
            }
            catch (Exception ex)
            {
                RecordException("POS Sale after Cooking Batch", ex);
            }

            // Test 4B.3: Afternoon Small Batch (0.5 Bag = 60 portions)
            try
            {
                var batch2 = await batchService.CreateBatchAsync(new POSSystem.WPF.Models.DTOs.CreateCookingBatchDto
                {
                    ProductId = mandiProduct.Id,
                    WarehouseId = 1,
                    BatchMultiplier = 0.5m,
                    MainIngredientQuantity = 0.5m,
                    ActualPortions = 60m,
                    CookedByUserId = cashierUser.Id,
                    Notes = "أمر طبخ قدر العصر نصف كيس"
                });

                var rice2 = await context.warehouseFromProducts.AsNoTracking().FirstAsync(w => w.Id == dishRice.Id);
                var oil2 = await context.warehouseFromProducts.AsNoTracking().FirstAsync(w => w.Id == dishOil.Id);
                var spices2 = await context.warehouseFromProducts.AsNoTracking().FirstAsync(w => w.Id == dishSpices.Id);
                var prod2 = await context.Products.AsNoTracking().FirstAsync(p => p.Id == mandiProduct.Id);

                Assert(rice2.StockQuantity == 48.5m, "Batch 2: Rice decremented by 0.5 bag to 48.5", $"Expected 48.5, got {rice2.StockQuantity}");
                Assert(oil2.StockQuantity == 97m, "Batch 2: Oil decremented by 1 liter to 97", $"Expected 97, got {oil2.StockQuantity}");
                Assert(spices2.StockQuantity == 19.25m, "Batch 2: Spices decremented by 0.25 kg to 19.25", $"Expected 19.25, got {spices2.StockQuantity}");
                Assert(prod2.AvailableQtyInPOS == 170m, "Batch 2: POS portions accumulated to 170 (110 + 60)", $"Expected 170, got {prod2.AvailableQtyInPOS}");
            }
            catch (Exception ex)
            {
                RecordException("Cooking Batch 2 (Afternoon 0.5 Pot)", ex);
            }

            // Test 4B.4: Insufficient Stock Rejection
            try
            {
                bool threw = false;
                try
                {
                    await batchService.CreateBatchAsync(new POSSystem.WPF.Models.DTOs.CreateCookingBatchDto
                    {
                        ProductId = mandiProduct.Id,
                        WarehouseId = 1,
                        MainIngredientQuantity = 500m,
                        ActualPortions = 60000m
                    });
                }
                catch (InvalidOperationException)
                {
                    threw = true;
                }
                Assert(threw, "Insufficient Stock Exception Thrown when Cooking Exceeds Warehouse Balance");
            }
            catch (Exception ex)
            {
                RecordException("Insufficient Stock Check", ex);
            }

            // Test 4B.5: Manual Actual Portions Override (e.g. 1 Bag yielded 115 portions instead of standard 120)
            try
            {
                var batch3 = await batchService.CreateBatchAsync(new POSSystem.WPF.Models.DTOs.CreateCookingBatchDto
                {
                    ProductId = mandiProduct.Id,
                    WarehouseId = 1,
                    MainIngredientQuantity = 1.0m,
                    ActualPortions = 115m,
                    Notes = "قدر مع تعديل يدوي للأنفار الفعلية 115"
                });

                var rice3 = await context.warehouseFromProducts.AsNoTracking().FirstAsync(w => w.Id == dishRice.Id);
                var prod3 = await context.Products.AsNoTracking().FirstAsync(p => p.Id == mandiProduct.Id);

                Assert(rice3.StockQuantity == 47.5m, "Batch 3: Rice decremented to 47.5");
                Assert(prod3.AvailableQtyInPOS == 285m, "Batch 3: Custom 115 portions added to 170 -> 285", $"Expected 285, got {prod3.AvailableQtyInPOS}");
            }
            catch (Exception ex)
            {
                RecordException("Cooking Batch 3 (Manual Override)", ex);
            }

            // =========================================================================
            ` + target;

if (s.includes(target)) {
    s = s.replace(target, testModuleCode);
    fs.writeFileSync('DbCheck/Program.cs', s, 'utf8');
    console.log('SUCCESS adding Module 4B to DbCheck/Program.cs');
} else {
    console.log('Target not found');
}
