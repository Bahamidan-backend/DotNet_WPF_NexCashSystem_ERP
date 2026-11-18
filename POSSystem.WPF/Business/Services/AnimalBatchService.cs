using Microsoft.EntityFrameworkCore;
using POSSystem.WPF.Business.Services.interfase;
using POSSystem.WPF.Models;
using static POSSystem.WPF.Models.Recipe;

namespace POSSystem.WPF.Business.Services
{
    public class AnimalBatchService : IAnimalBatchService
    {
        private readonly IUnitOfWork _unitOfWork;
        private static readonly ComponentType[] AllComponentTypes =
            Enum.GetValues<ComponentType>().Where(t => t != ComponentType.None).ToArray();

        public AnimalBatchService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<AnimalBatch>> GetAllBatchesAsync()
        {
            return await _unitOfWork.AnimalBatches.GetAllBatchesWithComponentsAsync();
        }

        public async Task<IEnumerable<AnimalBatch>> GetBatchesByTypeAsync(AnimalType type)
        {
            return await _unitOfWork.AnimalBatches.GetBatchesByAnimalTypeAsync(type);
        }

        public async Task<IEnumerable<AnimalBatch>> GetFullAnimalsOnlyAsync()
        {
            return await _unitOfWork.AnimalBatches.GetFullAnimalsOnlyAsync();
        }

        public async Task<AnimalBatch> GetBatchByIdAsync(int id)
        {
            return await _unitOfWork.AnimalBatches.GetBatchWithComponentsAsync(id)
                ?? throw new InvalidOperationException($"Animal batch with id {id} was not found.");
        }

        public async Task<AnimalBatch> CreateBatchAsync(AnimalBatch batch)
        {
            _unitOfWork.AnimalBatches.AddBatchWithComponents(batch);
            await _unitOfWork.SaveChangesAsync();
            return batch;
        }

        public async Task UpdateBatchComponentsAsync(BatchComponent component)
        {
            _unitOfWork.AnimalBatches.UpdateBatchComponents(component);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<bool> ConsumeFromBatchAsync(int batchId, ComponentType componentType, decimal quantity)
        {
            bool startNewTransaction = !_unitOfWork.HasActiveTransaction;
            if (startNewTransaction)
            {
                await _unitOfWork.BeginTransactionAsync();
            }

            try
            {
                var batch = await _unitOfWork.AnimalBatches.GetBatchWithComponentsAsync(batchId);
                if (batch == null)
                {
                    if (startNewTransaction) await _unitOfWork.RollbackTransactionAsync();
                    return false;
                }

                // Check for multi-component availability first
                foreach (var type in AllComponentTypes)
                {
                    if (!componentType.HasFlag(type)) continue;

                    var comp = batch.Components.FirstOrDefault(c => c.ComponentType == type);
                    if (comp == null || comp.Quantity < quantity)
                    {
                        if (startNewTransaction) await _unitOfWork.RollbackTransactionAsync();
                        return false;
                    }
                }

                // Deduct each component in the flags
                foreach (var type in AllComponentTypes)
                {
                    if (!componentType.HasFlag(type)) continue;

                    var comp = batch.Components.FirstOrDefault(c => c.ComponentType == type);
                    if (comp == null) continue;
                    comp.Quantity -= quantity;

                    switch (type)
                    {
                        case ComponentType.Head:
                            batch.HeadCount = (int)Math.Max(0, batch.HeadCount - quantity);
                            break;
                        case ComponentType.Back:
                            batch.BackWeight = Math.Max(0m, batch.BackWeight - quantity);
                            break;

                        case ComponentType.Liver:
                            batch.LiverWeight = Math.Max(0m, batch.LiverWeight - quantity);
                            break;
                        case ComponentType.Meat:
                            batch.RemainingWeight = Math.Max(0m, batch.RemainingWeight - quantity);
                            break;
                    }

                    if (comp.Quantity <= 0)
                    {
                        batch.Components.Remove(comp);
                    }
                    _unitOfWork.AnimalBatches.UpdateBatchComponents(comp);
                }

                _unitOfWork.AnimalBatches.Update(batch);
                await _unitOfWork.SaveChangesAsync();

                if (startNewTransaction) await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch
            {
                if (startNewTransaction) await _unitOfWork.RollbackTransactionAsync();
                return false;
            }
        }

        public async Task<bool> ConsumeFromAnyBatchAsync(ComponentType componentType, decimal quantity)
        {
            bool startNewTransaction = !_unitOfWork.HasActiveTransaction;
            if (startNewTransaction)
                await _unitOfWork.BeginTransactionAsync();

            try
            {
                decimal remainingToConsume = quantity;
                
                // Get all batches ordered by oldest first, that have components with stock
                var batches = await _unitOfWork.GetContext().AnimalBatches
                    .Include(b => b.Components)
                    .Where(b => b.Components.Any(c => c.Quantity > 0))
                    .OrderBy(b => b.BatchDate)
                    .ToListAsync();
                
                // REFACTORED APPROACH for AnyBatch: Loop through flags first
                foreach (var type in AllComponentTypes)
                {
                    if (!componentType.HasFlag(type)) continue;
                    
                    decimal typeRemaining = quantity;
                    foreach (var batch in batches)
                    {
                        if (typeRemaining <= 0) break;
                        
                        var comp = batch.Components.FirstOrDefault(c => c.ComponentType == type && c.Quantity > 0);
                        if (comp == null) continue;
                        
                        decimal consumed = Math.Min(comp.Quantity, typeRemaining);
                        comp.Quantity -= consumed;
                        typeRemaining -= consumed;
                        
                        switch (type)
                        {
                            case ComponentType.Head: batch.HeadCount -= (int)consumed; break;
                            case ComponentType.Back: batch.BackWeight -= consumed; break;

                            case ComponentType.Liver: batch.LiverWeight -= consumed; break;
                            case ComponentType.Meat: batch.RemainingWeight -= consumed; break;
                        }
                        
                        if (comp.Quantity <= 0) batch.Components.Remove(comp);
                        _unitOfWork.AnimalBatches.UpdateBatchComponents(comp);
                        _unitOfWork.AnimalBatches.Update(batch);
                    }

                    // If after iterating all batches, we still have remaining for this type, then we don't have enough stock
                    if (typeRemaining > 0)
                    {
                        if (startNewTransaction) await _unitOfWork.RollbackTransactionAsync();
                        return false;
                    }
                }
                
                if (startNewTransaction)
                {
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();
                }
                return true;
            }
            catch
            {
                if (startNewTransaction) await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> RestoreToAnyBatchAsync(ComponentType componentType, decimal quantity)
        {
            bool startNewTransaction = !_unitOfWork.HasActiveTransaction;
            if (startNewTransaction)
                await _unitOfWork.BeginTransactionAsync();

            try
            {
                decimal remainingToRestore = quantity;
                
                // Get all batches ordered by NEWEST first, to restore to the latest available batch
                var batches = await _unitOfWork.GetContext().AnimalBatches
                    .Include(b => b.Components)
                    .OrderByDescending(b => b.BatchDate)
                    .ToListAsync();

                foreach (var type in AllComponentTypes)
                {
                    if (!componentType.HasFlag(type)) continue;

                    decimal typeRemaining = quantity;
                    foreach (var batch in batches)
                    {
                        if (typeRemaining <= 0) break;

                        var comp = batch.Components.FirstOrDefault(c => c.ComponentType == type);
                        
                        // If component doesn't exist in this batch, we might need to add it back if the batch is of the right type
                        // For simplicity, we only restore to batches that had that component type originally or are still active.
                        if (comp == null)
                        {
                            // Create a new component record if it was completely consumed (removed from collection)
                            comp = new BatchComponent 
                            { 
                                AnimalBatchId = batch.Id, 
                                ComponentType = type, 
                                Quantity = 0, 
                                OriginalQuantity = 0, // We don't know original, but we are restoring
                                Unit = type == ComponentType.Head ? "حبة" : (type == ComponentType.Meat && !batch.IsWeightMode ? "نفر" : "كجم")
                            };
                            batch.Components.Add(comp);
                        }

                        decimal maxCanRestore = comp.OriginalQuantity > 0 
                            ? Math.Max(0, comp.OriginalQuantity - comp.Quantity) 
                            : typeRemaining;
                        if (maxCanRestore <= 0) continue;
                        decimal canRestore = Math.Min(typeRemaining, maxCanRestore);

                        comp.Quantity += canRestore;
                        typeRemaining -= canRestore;

                        switch (type)
                        {
                            case ComponentType.Head: batch.HeadCount += (int)canRestore; break;
                            case ComponentType.Back: batch.BackWeight += canRestore; break;
                            case ComponentType.Liver: batch.LiverWeight += canRestore; break;
                            case ComponentType.Meat: batch.RemainingWeight += canRestore; break;
                        }

                        _unitOfWork.AnimalBatches.UpdateBatchComponents(comp);
                        _unitOfWork.AnimalBatches.Update(batch);
                    }

                    if (typeRemaining > 0 && batches.Any())
                    {
                        var firstBatch = batches.First();
                        var firstComp = firstBatch.Components.FirstOrDefault(c => c.ComponentType == type);
                        if (firstComp == null)
                        {
                            firstComp = new BatchComponent
                            {
                                AnimalBatchId = firstBatch.Id,
                                ComponentType = type,
                                Quantity = 0,
                                OriginalQuantity = 0,
                                Unit = type == ComponentType.Head ? "حبة" : (type == ComponentType.Meat && !firstBatch.IsWeightMode ? "نفر" : "كجم")
                            };
                            firstBatch.Components.Add(firstComp);
                        }

                        firstComp.Quantity += typeRemaining;
                        switch (type)
                        {
                            case ComponentType.Head: firstBatch.HeadCount += (int)typeRemaining; break;
                            case ComponentType.Back: firstBatch.BackWeight += typeRemaining; break;
                            case ComponentType.Liver: firstBatch.LiverWeight += typeRemaining; break;
                            case ComponentType.Meat: firstBatch.RemainingWeight += typeRemaining; break;
                        }

                        _unitOfWork.AnimalBatches.UpdateBatchComponents(firstComp);
                        _unitOfWork.AnimalBatches.Update(firstBatch);
                    }
                }

                if (startNewTransaction)
                {
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();
                }
                return true;
            }
            catch
            {
                if (startNewTransaction) await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> SellFullAnimalAsync(int batchId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var batch = await _unitOfWork.AnimalBatches.GetBatchWithComponentsAsync(batchId);
                if (batch == null || !batch.IsFullAnimal)
                    return false;

                // 1. Sync with Warehouse Inventory (Deduct from general stock)
                if (batch.WarehouseId.HasValue)
                {
                    string animalTypeName = batch.Type switch
                    {
                        AnimalType.Sheep => "خروف",
                        AnimalType.Cow => "بقرة",
                        AnimalType.Camel => "جمل",
                        _ => "ذبيحة"
                    };

                    string liverName = batch.IsWeightMode ? $"كبدة {animalTypeName} (كجم)" : $"كبدة {animalTypeName} (حبة)";
                    string backName = batch.IsWeightMode ? $"ظهر {animalTypeName} (كجم)" : $"ظهر {animalTypeName} (حبة)";

                    // Identify products to deduct from warehouse
                    var deductions = new List<(string Name, decimal Quantity)>
                    {
                        ($"رأس {animalTypeName}", batch.HeadCount),
                        (liverName, batch.LiverWeight),
                        (backName, batch.BackWeight)
                    };

                    // Add Meat deduction (handle both weight and thumn modes)
                    string meatName = batch.IsWeightMode == false 
                        ? $"لحم {animalTypeName} (نفر/جزء)" 
                        : $"لحم {animalTypeName} (كجم)";
                    deductions.Add((meatName, batch.RemainingWeight));

                    // OPTIMIZATION: Fetch all relevant warehouse products in ONE query instead of inside the loop (N+1 fix)
                    var deductionNames = deductions.Select(d => d.Name).ToList();
                    var warehouseProducts = await _unitOfWork.warehouseFromProduct.FindAsync(
                        wfp => wfp.WarehouseId == batch.WarehouseId.Value && deductionNames.Contains(wfp.Name));
                        
                    var existingProductsDict = warehouseProducts.Where(w => !string.IsNullOrWhiteSpace(w.Name)).GroupBy(w => w.Name.Trim(), StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                    foreach (var deduction in deductions)
                    {
                        if (existingProductsDict.TryGetValue(deduction.Name, out var warehouseProduct))
                        {
                            if (warehouseProduct.StockQuantity < deduction.Quantity)
                            {
                                throw new InvalidOperationException(
                                    $"الرصيد المتوفر بالمستودع للمادة '{warehouseProduct.Name}' ({warehouseProduct.StockQuantity:N0}) أقل من المطلوب لاستهلاك الدفعة ({deduction.Quantity:N0}).");
                            }

                            decimal beforeQty = warehouseProduct.StockQuantity;
                            warehouseProduct.StockQuantity -= deduction.Quantity;
                            decimal afterQty = warehouseProduct.StockQuantity;
                            warehouseProduct.UpdatedAt = DateTime.Now;
                            _unitOfWork.warehouseFromProduct.Update(warehouseProduct);

                            await _unitOfWork.StockMovements.AddAsync(new StockMovement
                            {
                                WarehouseFromProductId = warehouseProduct.Id,
                                WarehouseId = warehouseProduct.WarehouseId,
                                MovementType = StockMovementType.RecipeCooking,
                                Quantity = -deduction.Quantity,
                                BalanceBefore = beforeQty,
                                BalanceAfter = afterQty,
                                UnitCost = warehouseProduct.Price,
                                TotalCost = deduction.Quantity * warehouseProduct.Price,
                                ReferenceNumber = $"AB-CNS-{batch.Id}",
                                Notes = $"استهلاك مكونات دفعة الذبائح #{batch.Id} ({batch.Name})",
                                MovementDate = DateTime.Now,
                                CreatedAt = DateTime.Now
                            });
                        }
                    }
                }

                // 2. Mark batch components as consumed
                foreach (var component in batch.Components)
                {
                    component.Quantity = 0;
                }

                // Set batch remaining values to 0
                batch.HeadCount = 0;
                batch.BackWeight = 0;
                batch.LiverWeight = 0;
                batch.RemainingWeight = 0;

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return false;
            }
        }

        public async Task<bool> DeleteBatchAsync(int batchId)
        {
            var context = _unitOfWork.GetContext();
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                await context.BatchComponents.Where(c => c.AnimalBatchId == batchId).ExecuteDeleteAsync();
                int deleted = await context.AnimalBatches.Where(b => b.Id == batchId).ExecuteDeleteAsync();
                if (deleted == 0)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                await transaction.CommitAsync();
                _unitOfWork.ClearEntityTracking();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<int> GetFullAnimalsCountAsync()
        {
            var context = _unitOfWork.GetContext();
            return await context.AnimalBatches
                .AsNoTracking()
                .CountAsync(b => b.HeadCount > 0 && b.BackWeight > 0 && b.LiverWeight > 0 && b.RemainingWeight > 0);
        }

        public async Task<Dictionary<AnimalType, int>> GetFullAnimalsCountByTypeAsync()
        {
            var context = _unitOfWork.GetContext();
            return await context.AnimalBatches
                .AsNoTracking()
                .Where(b => b.HeadCount > 0 && b.BackWeight > 0 && b.LiverWeight > 0 && b.RemainingWeight > 0)
                .GroupBy(b => b.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count);
        }

        public async Task<bool> BreakdownCarcassesFromInventoryAsync(int sourceWarehouseId, int warehouseFromProductId, int targetWarehouseId, int count, bool isWeightMode, int portionsCount, decimal averageWeight, decimal averageLiverWeight, decimal averageBackWeight, int categoryIdToUse, int? supplierId = null)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var sourceWarehouseItem = await _unitOfWork.warehouseFromProduct.GetByIdAsync(warehouseFromProductId);
                if (sourceWarehouseItem == null || sourceWarehouseItem.StockQuantity < count)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return false;
                }

                // 1. Deduct from Source Warehouse (Barn)
                decimal sourceBefore = sourceWarehouseItem.StockQuantity;
                sourceWarehouseItem.StockQuantity -= count;
                decimal sourceAfter = sourceWarehouseItem.StockQuantity;
                sourceWarehouseItem.UpdatedAt = DateTime.Now;
                _unitOfWork.warehouseFromProduct.Update(sourceWarehouseItem);

                string breakdownRef = $"AB-BRK-{DateTime.Now:yyyyMMddHHmmss}-{sourceWarehouseItem.Id}";

                await _unitOfWork.StockMovements.AddAsync(new StockMovement
                {
                    WarehouseFromProductId = sourceWarehouseItem.Id,
                    WarehouseId = sourceWarehouseId,
                    MovementType = StockMovementType.RecipeCooking,
                    Quantity = -count,
                    BalanceBefore = sourceBefore,
                    BalanceAfter = sourceAfter,
                    UnitCost = sourceWarehouseItem.Price,
                    TotalCost = count * sourceWarehouseItem.Price,
                    ReferenceNumber = breakdownRef,
                    Notes = $"تفكيك وسلخ ذبائح ({count} رأس) من {sourceWarehouseItem.Name}",
                    MovementDate = DateTime.Now,
                    CreatedAt = DateTime.Now
                });

                // Determine AnimalType from product properties or name
                AnimalType type = AnimalType.Sheep; // Default
                string sourceItemName = sourceWarehouseItem.Name?.ToLower() ?? "خروف";
                if (sourceItemName.Contains("بقر") || sourceItemName.Contains("cow")) type = AnimalType.Cow;
                else if (sourceItemName.Contains("جمل") || sourceItemName.Contains("camel")) type = AnimalType.Camel;

                string animalTypeName = type switch
                {
                    AnimalType.Sheep => "خروف",
                    AnimalType.Cow => "بقرة",
                    AnimalType.Camel => "جمل",
                    _ => "ذبيحة"
                };

                // Create or Get Component Products
                var componentsToCreate = new List<(string Name, decimal Quantity)>();
                
                // Common components
                componentsToCreate.Add(($"رأس {animalTypeName}", count));

                // Meat, liver, and back depend on the method
                if (isWeightMode)
                {
                    componentsToCreate.Add(($"كبدة {animalTypeName} (كجم)", count * averageLiverWeight));
                    componentsToCreate.Add(($"ظهر {animalTypeName} (كجم)", count * averageBackWeight));
                    componentsToCreate.Add(($"لحم {animalTypeName} (كجم)", count * averageWeight));
                }
                else
                {
                    componentsToCreate.Add(($"كبدة {animalTypeName} (حبة)", count * 1));
                    componentsToCreate.Add(($"ظهر {animalTypeName} (حبة)", count * 2)); // 2 backs per carcass

                    // Portions Mode (الثمن/الأجزاء)
                    int portionsTotal = count * portionsCount;
                    componentsToCreate.Add(($"لحم {animalTypeName} (نفر/جزء)", portionsTotal));
                }

                decimal purchaseCost = sourceWarehouseItem.Price * count;

                // OPTIMIZATION: Fetch all target products in ONE database query (N+1 fix)
                var compNames = componentsToCreate.Select(c => c.Name).ToList();
                var existingTargetProducts = await _unitOfWork.warehouseFromProduct.FindAsync(
                    wfp => wfp.WarehouseId == targetWarehouseId && compNames.Contains(wfp.Name));
                    
                var existingTargetProductsDict = existingTargetProducts.Where(w => !string.IsNullOrWhiteSpace(w.Name)).GroupBy(w => w.Name.Trim(), StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                foreach (var comp in componentsToCreate)
                {
                    // Determine PortionUnit based on component name
                    PortionUnit componentPortionUnit = comp.Name.Contains("حبة") || comp.Name.Contains("رأس") ? PortionUnit.حبة :
                                                      (comp.Name.Contains("نفر") || comp.Name.Contains("ثمن") || comp.Name.Contains("جزء")) ? PortionUnit.Portion :
                                                      PortionUnit.كجم;

                    decimal allocatedCost = 0;
                    if (purchaseCost > 0 && comp.Quantity > 0)
                    {
                        decimal share = 0.70m; // Default for meat
                        string lowerName = comp.Name?.ToLower() ?? "";
                        if (lowerName.Contains("كبد") || lowerName.Contains("liver")) share = 0.12m;
                        else if (lowerName.Contains("ظهر") || lowerName.Contains("back")) share = 0.10m;
                        else if (lowerName.Contains("رأس") || lowerName.Contains("راس") || lowerName.Contains("head")) share = 0.08m;
                        allocatedCost = Math.Round((purchaseCost * share) / comp.Quantity, 2);
                    }

                    decimal compBefore = 0m;
                    if (!existingTargetProductsDict.TryGetValue(comp.Name, out var targetWarehouseProduct))
                    {
                        targetWarehouseProduct = new WarehouseFromProduct
                        {
                            WarehouseId = targetWarehouseId,
                            Name = comp.Name,
                            StockQuantity = comp.Quantity,
                            Price = allocatedCost,
                            IsActive = true,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            PortionUnit = componentPortionUnit
                        };
                        await _unitOfWork.warehouseFromProduct.AddAsync(targetWarehouseProduct);
                    }
                    else
                    {
                        compBefore = targetWarehouseProduct.StockQuantity;
                        var prevVal = targetWarehouseProduct.StockQuantity * targetWarehouseProduct.Price;
                        var newVal = comp.Quantity * allocatedCost;
                        targetWarehouseProduct.StockQuantity += comp.Quantity;
                        if (targetWarehouseProduct.StockQuantity > 0)
                            targetWarehouseProduct.Price = Math.Round((prevVal + newVal) / targetWarehouseProduct.StockQuantity, 2);

                        targetWarehouseProduct.PortionUnit = componentPortionUnit;
                        targetWarehouseProduct.UpdatedAt = DateTime.Now;
                        _unitOfWork.warehouseFromProduct.Update(targetWarehouseProduct);
                    }

                    await _unitOfWork.StockMovements.AddAsync(new StockMovement
                    {
                        WarehouseFromProduct = targetWarehouseProduct,
                        WarehouseId = targetWarehouseId,
                        MovementType = StockMovementType.InitialStock,
                        Quantity = comp.Quantity,
                        BalanceBefore = compBefore,
                        BalanceAfter = compBefore + comp.Quantity,
                        UnitCost = allocatedCost,
                        TotalCost = comp.Quantity * allocatedCost,
                        ReferenceNumber = breakdownRef,
                        Notes = $"استلام جزء ناتج عن تفكيك وسلخ ذبائح ({comp.Name})",
                        MovementDate = DateTime.Now,
                        CreatedAt = DateTime.Now
                    });
                }
                // 2. Create AnimalBatch record for tracking in the Batch Management screen
                AnimalType typeForBatch = AnimalType.Sheep; // Default
                string sourceName = sourceWarehouseItem.Name?.ToLower() ?? "خروف";
                if (sourceName.Contains("بقر") || sourceName.Contains("cow")) typeForBatch = AnimalType.Cow;
                else if (sourceName.Contains("جمل") || sourceName.Contains("camel")) typeForBatch = AnimalType.Camel;

                var animalBatch = new AnimalBatch
                {
                    Type = typeForBatch,
                    BatchDate = DateTime.Now,
                    WarehouseId = targetWarehouseId,
                    HeadCount = count,
                    LiverWeight = isWeightMode ? (count * averageLiverWeight) : (count * 1),
                    BackWeight = isWeightMode ? (count * averageBackWeight) : (count * 2),
                    RemainingWeight = isWeightMode ? (count * averageWeight) : (count * portionsCount),
                    IsWeightMode = isWeightMode,
                    PurchaseCost = purchaseCost,
                    SupplierId = supplierId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                animalBatch.Components.Add(new BatchComponent { ComponentType = ComponentType.Head, Quantity = count, OriginalQuantity = count, Unit = "حبة" });
                animalBatch.Components.Add(new BatchComponent { ComponentType = ComponentType.Liver, Quantity = animalBatch.LiverWeight, OriginalQuantity = animalBatch.LiverWeight, Unit = isWeightMode ? "كجم" : "حبة" });
                animalBatch.Components.Add(new BatchComponent { ComponentType = ComponentType.Back, Quantity = animalBatch.BackWeight, OriginalQuantity = animalBatch.BackWeight, Unit = isWeightMode ? "كجم" : "حبة" });
                animalBatch.Components.Add(new BatchComponent { ComponentType = ComponentType.Meat, Quantity = animalBatch.RemainingWeight, OriginalQuantity = animalBatch.RemainingWeight, Unit = isWeightMode ? "كجم" : "نفر" });

                _unitOfWork.AnimalBatches.AddBatchWithComponents(animalBatch);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return false;
            }
        }

        public async Task<bool> AddPartialCarcassAsync(int targetWarehouseId, AnimalType animalType, decimal headCount, decimal liverQty, decimal backQty, decimal meatQty, bool isWeightMode,  int? supplierId = null)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                string animalTypeName = animalType switch
                {
                    AnimalType.Sheep => "خروف",
                    AnimalType.Cow => "بقرة",
                    AnimalType.Camel => "جمل",
                    _ => "ذبيحة"
                };

                var componentsToCreate = new List<(string Name, decimal Quantity)>();
                
                if (headCount > 0) componentsToCreate.Add(($"رأس {animalTypeName}", headCount));
                
                if (isWeightMode)
                {
                    if (liverQty > 0) componentsToCreate.Add(($"كبدة {animalTypeName} (كجم)", liverQty));
                    if (backQty > 0) componentsToCreate.Add(($"ظهر {animalTypeName} (كجم)", backQty));
                    if (meatQty > 0) componentsToCreate.Add(($"لحم {animalTypeName} (كجم)", meatQty));
                }
                else
                {
                    if (liverQty > 0) componentsToCreate.Add(($"كبدة {animalTypeName} (حبة)", liverQty));
                    if (backQty > 0) componentsToCreate.Add(($"ظهر {animalTypeName} (حبة)", backQty));
                    if (meatQty > 0) componentsToCreate.Add(($"لحم {animalTypeName} (نفر/جزء)", meatQty));
                }

                var compNames = componentsToCreate.Select(c => c.Name).ToList();
                var existingTargetProducts = await _unitOfWork.warehouseFromProduct.FindAsync(
                    wfp => wfp.WarehouseId == targetWarehouseId && compNames.Contains(wfp.Name));
                    
                var existingTargetProductsDict = existingTargetProducts.Where(w => !string.IsNullOrWhiteSpace(w.Name)).GroupBy(w => w.Name.Trim(), StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                foreach (var comp in componentsToCreate)
                {
                    PortionUnit componentPortionUnit = comp.Name.Contains("حبة") || comp.Name.Contains("رأس") ? PortionUnit.حبة :
                                                      (comp.Name.Contains("نفر") || comp.Name.Contains("ثمن") || comp.Name.Contains("جزء")) ? PortionUnit.Portion :
                                                      PortionUnit.كجم;

                    if (!existingTargetProductsDict.TryGetValue(comp.Name, out var targetWarehouseProduct))
                    {
                        targetWarehouseProduct = new WarehouseFromProduct
                        {
                            WarehouseId = targetWarehouseId,
                            Name = comp.Name,
                            StockQuantity = comp.Quantity,
                            Price = 0,
                            IsActive = true,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            PortionUnit = componentPortionUnit
                        };
                        await _unitOfWork.warehouseFromProduct.AddAsync(targetWarehouseProduct);
                    }
                    else
                    {
                        targetWarehouseProduct.StockQuantity += comp.Quantity;
                        targetWarehouseProduct.PortionUnit = componentPortionUnit;
                        targetWarehouseProduct.UpdatedAt = DateTime.Now;
                        _unitOfWork.warehouseFromProduct.Update(targetWarehouseProduct);
                    }
                }

                var animalBatch = new AnimalBatch
                {
                    Type = animalType,
                    BatchDate = DateTime.Now,
                    WarehouseId = targetWarehouseId,
                    HeadCount = (int)headCount,
                    LiverWeight = liverQty,
                    BackWeight = backQty,
                    RemainingWeight = meatQty,
                    IsWeightMode = isWeightMode,
                   // PurchaseCost = purchaseCost,
                    SupplierId = supplierId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                if (headCount > 0) animalBatch.Components.Add(new BatchComponent { ComponentType = ComponentType.Head, Quantity = headCount, OriginalQuantity = headCount, Unit = "حبة" });
                if (liverQty > 0) animalBatch.Components.Add(new BatchComponent { ComponentType = ComponentType.Liver, Quantity = liverQty, OriginalQuantity = liverQty, Unit = isWeightMode ? "كجم" : "حبة" });
                if (backQty > 0) animalBatch.Components.Add(new BatchComponent { ComponentType = ComponentType.Back, Quantity = backQty, OriginalQuantity = backQty, Unit = isWeightMode ? "كجم" : "حبة" });
                if (meatQty > 0) animalBatch.Components.Add(new BatchComponent { ComponentType = ComponentType.Meat, Quantity = meatQty, OriginalQuantity = meatQty, Unit = isWeightMode ? "كجم" : "نفر" });

                _unitOfWork.AnimalBatches.AddBatchWithComponents(animalBatch);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                return false;
            }
        }
    }
}
