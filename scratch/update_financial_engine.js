const fs = require('fs');

const path = 'POSSystem.WPF/Business/Services/FinancialEngineService.cs';
let content = fs.readFileSync(path, 'utf8');
content = content.replace(/\r\n/g, '\n');

// Update Recipe select projection
const targetProjection = `                var productRecipes = await context.Recipes.AsNoTracking()
                    .Where(r => productIdsInSales.Contains(r.ProductId))
                    .Select(r => new
                    {
                        r.ProductId,
                        r.Name,
                        r.Quantity,
                        r.portionUnit
                    })
                    .ToListAsync();`;

const replaceProjection = `                var productRecipes = await context.Recipes.AsNoTracking()
                    .Where(r => productIdsInSales.Contains(r.ProductId))
                    .Select(r => new
                    {
                        r.ProductId,
                        r.WarehouseFromProductId,
                        r.Name,
                        r.Quantity,
                        r.portionUnit
                    })
                    .ToListAsync();`;

// Update Recipe lookup loop
const targetLookup = `                            foreach (var r in recipes)
                            {
                                if (string.IsNullOrWhiteSpace(r.Name) || r.Quantity <= 0) continue;

                                var trimmedName = r.Name.Trim();
                                if (warehouseProductByName.TryGetValue(trimmedName, out var whProd) && whProd.Price > 0)
                                {
                                    decimal convertedQty = UnitConversionHelper.ConvertQuantity(r.Quantity, r.portionUnit, whProd.PortionUnit);
                                    unitRecipeCost += convertedQty * whProd.Price;
                                }
                                else if (warehouseByName.TryGetValue(trimmedName, out var fallbackPrice) && fallbackPrice > 0)
                                {
                                    unitRecipeCost += r.Quantity * fallbackPrice;
                                }
                            }`;

const replaceLookup = `                            foreach (var r in recipes)
                            {
                                if (r.Quantity <= 0) continue;

                                WarehouseFromProduct? whProd = null;
                                if (r.WarehouseFromProductId.HasValue && warehouseById.ContainsKey(r.WarehouseFromProductId.Value))
                                {
                                    whProd = allWarehouseProducts.FirstOrDefault(w => w.Id == r.WarehouseFromProductId.Value);
                                }
                                else if (!string.IsNullOrWhiteSpace(r.Name) && warehouseProductByName.TryGetValue(r.Name.Trim(), out var byName))
                                {
                                    whProd = byName;
                                }

                                if (whProd != null && whProd.Price > 0)
                                {
                                    decimal convertedQty = UnitConversionHelper.ConvertQuantity(r.Quantity, r.portionUnit, whProd.PortionUnit);
                                    unitRecipeCost += convertedQty * whProd.Price;
                                }
                                else if (!string.IsNullOrWhiteSpace(r.Name) && warehouseByName.TryGetValue(r.Name.Trim(), out var fallbackPrice) && fallbackPrice > 0)
                                {
                                    unitRecipeCost += r.Quantity * fallbackPrice;
                                }
                            }`;

if (!content.includes(targetProjection)) {
    console.error('targetProjection not found!');
    process.exit(1);
}
if (!content.includes(targetLookup)) {
    console.error('targetLookup not found!');
    process.exit(1);
}

content = content.replace(targetProjection, replaceProjection);
content = content.replace(targetLookup, replaceLookup);

fs.writeFileSync(path, content.replace(/\n/g, '\r\n'), 'utf8');
console.log('Successfully updated FinancialEngineService.cs!');

