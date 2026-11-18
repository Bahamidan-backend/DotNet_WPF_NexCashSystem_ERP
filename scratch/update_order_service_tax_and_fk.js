const fs = require('fs');

const path = 'POSSystem.WPF/Business/Services/OrderService.cs';
let content = fs.readFileSync(path, 'utf8');

// Normalize line endings
content = content.replace(/\r\n/g, '\n');

// 1. In CreateOrderAsync
const targetOrderTax = `                Discount = order.Discount,
                Total = order.Total,`;

const replaceOrderTax = `                Discount = order.Discount,
                TaxRate = order.TaxRate > 0 ? order.TaxRate : 0.15m,
                TaxAmount = order.TaxAmount > 0 ? order.TaxAmount : Math.Round(order.SubTotal * (order.TaxRate > 0 ? order.TaxRate : 0.15m), 2),
                Total = order.Total,`;

if (!content.includes(targetOrderTax)) {
    console.error('targetOrderTax not found!');
    process.exit(1);
}
content = content.replace(targetOrderTax, replaceOrderTax);

// 2. In UpdateStockForOrderItemsAsync
const targetDeductFetch = `            // Pre-fetch raw material ingredients for any products that have recipes defined
            var allRecipeIngredientNames = products
                .Where(p => p.Recipe != null)
                .SelectMany(p => p.Recipe)
                .Where(r => !string.IsNullOrWhiteSpace(r.Name) && r.Quantity > 0)
                .Select(r => r.Name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var whIngredients = allRecipeIngredientNames.Any()
                ? await _unitOfWork.warehouseFromProduct.FindAsync(wp => wp.IsActive && allRecipeIngredientNames.Contains(wp.Name))
                : Enumerable.Empty<WarehouseFromProduct>();

            var whIngredientsMap = new Dictionary<string, WarehouseFromProduct>(StringComparer.OrdinalIgnoreCase);
            foreach (var whp in whIngredients)
            {
                if (!string.IsNullOrWhiteSpace(whp.Name) && !whIngredientsMap.ContainsKey(whp.Name.Trim()))
                    whIngredientsMap[whp.Name.Trim()] = whp;
            }`;

const replaceDeductFetch = `            // Pre-fetch raw material ingredients for any products that have recipes defined (by FK or by Name)
            var allRecipeIngredientIds = products
                .Where(p => p.Recipe != null)
                .SelectMany(p => p.Recipe)
                .Where(r => r.WarehouseFromProductId.HasValue)
                .Select(r => r.WarehouseFromProductId!.Value)
                .Distinct()
                .ToList();

            var allRecipeIngredientNames = products
                .Where(p => p.Recipe != null)
                .SelectMany(p => p.Recipe)
                .Where(r => !string.IsNullOrWhiteSpace(r.Name) && r.Quantity > 0)
                .Select(r => r.Name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var whIngredients = (allRecipeIngredientIds.Any() || allRecipeIngredientNames.Any())
                ? await _unitOfWork.warehouseFromProduct.FindAsync(wp => wp.IsActive && (allRecipeIngredientIds.Contains(wp.Id) || allRecipeIngredientNames.Contains(wp.Name)))
                : Enumerable.Empty<WarehouseFromProduct>();

            var whIngredientsById = whIngredients.ToDictionary(w => w.Id);
            var whIngredientsMap = new Dictionary<string, WarehouseFromProduct>(StringComparer.OrdinalIgnoreCase);
            foreach (var whp in whIngredients)
            {
                if (!string.IsNullOrWhiteSpace(whp.Name) && !whIngredientsMap.ContainsKey(whp.Name.Trim()))
                    whIngredientsMap[whp.Name.Trim()] = whp;
            }`;

if (!content.includes(targetDeductFetch)) {
    console.error('targetDeductFetch not found!');
    process.exit(1);
}
content = content.replace(targetDeductFetch, replaceDeductFetch);

const targetDeductLoop = `                        foreach (var recipe in product.Recipe)
                        {
                            if (string.IsNullOrWhiteSpace(recipe.Name) || recipe.Quantity <= 0) continue;
                            if (whIngredientsMap.TryGetValue(recipe.Name.Trim(), out var whIngredient))
                            {`;

const replaceDeductLoop = `                        foreach (var recipe in product.Recipe)
                        {
                            if (recipe.Quantity <= 0) continue;
                            WarehouseFromProduct? whIngredient = null;
                            if (recipe.WarehouseFromProductId.HasValue && whIngredientsById.TryGetValue(recipe.WarehouseFromProductId.Value, out var foundById))
                            {
                                whIngredient = foundById;
                            }
                            else if (!string.IsNullOrWhiteSpace(recipe.Name) && whIngredientsMap.TryGetValue(recipe.Name.Trim(), out var foundByName))
                            {
                                whIngredient = foundByName;
                            }

                            if (whIngredient != null)
                            {`;

if (!content.includes(targetDeductLoop)) {
    console.error('targetDeductLoop not found!');
    process.exit(1);
}
content = content.replace(targetDeductLoop, replaceDeductLoop);

// 3. In RestoreStockForOrderItemsAsync
const targetRestoreFetch = `            var allRestoreRecipeNames = products
                .Where(p => p.Recipe != null)
                .SelectMany(p => p.Recipe)
                .Where(r => !string.IsNullOrWhiteSpace(r.Name) && r.Quantity > 0)
                .Select(r => r.Name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var whRestoreIngredients = allRestoreRecipeNames.Any()
                ? await _unitOfWork.warehouseFromProduct.FindAsync(wp => wp.IsActive && allRestoreRecipeNames.Contains(wp.Name))
                : Enumerable.Empty<WarehouseFromProduct>();

            var whRestoreMap = new Dictionary<string, WarehouseFromProduct>(StringComparer.OrdinalIgnoreCase);
            foreach (var whp in whRestoreIngredients)
            {
                if (!string.IsNullOrWhiteSpace(whp.Name) && !whRestoreMap.ContainsKey(whp.Name.Trim()))
                    whRestoreMap[whp.Name.Trim()] = whp;
            }`;

const replaceRestoreFetch = `            var allRestoreRecipeIds = products
                .Where(p => p.Recipe != null)
                .SelectMany(p => p.Recipe)
                .Where(r => r.WarehouseFromProductId.HasValue)
                .Select(r => r.WarehouseFromProductId!.Value)
                .Distinct()
                .ToList();

            var allRestoreRecipeNames = products
                .Where(p => p.Recipe != null)
                .SelectMany(p => p.Recipe)
                .Where(r => !string.IsNullOrWhiteSpace(r.Name) && r.Quantity > 0)
                .Select(r => r.Name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var whRestoreIngredients = (allRestoreRecipeIds.Any() || allRestoreRecipeNames.Any())
                ? await _unitOfWork.warehouseFromProduct.FindAsync(wp => wp.IsActive && (allRestoreRecipeIds.Contains(wp.Id) || allRestoreRecipeNames.Contains(wp.Name)))
                : Enumerable.Empty<WarehouseFromProduct>();

            var whRestoreById = whRestoreIngredients.ToDictionary(w => w.Id);
            var whRestoreMap = new Dictionary<string, WarehouseFromProduct>(StringComparer.OrdinalIgnoreCase);
            foreach (var whp in whRestoreIngredients)
            {
                if (!string.IsNullOrWhiteSpace(whp.Name) && !whRestoreMap.ContainsKey(whp.Name.Trim()))
                    whRestoreMap[whp.Name.Trim()] = whp;
            }`;

if (!content.includes(targetRestoreFetch)) {
    console.error('targetRestoreFetch not found!');
    process.exit(1);
}
content = content.replace(targetRestoreFetch, replaceRestoreFetch);

const targetRestoreLoop = `                        foreach (var recipe in product.Recipe)
                        {
                            if (string.IsNullOrWhiteSpace(recipe.Name) || recipe.Quantity <= 0) continue;
                            if (whRestoreMap.TryGetValue(recipe.Name.Trim(), out var whIngredient))
                            {`;

const replaceRestoreLoop = `                        foreach (var recipe in product.Recipe)
                        {
                            if (recipe.Quantity <= 0) continue;
                            WarehouseFromProduct? whIngredient = null;
                            if (recipe.WarehouseFromProductId.HasValue && whRestoreById.TryGetValue(recipe.WarehouseFromProductId.Value, out var foundById))
                            {
                                whIngredient = foundById;
                            }
                            else if (!string.IsNullOrWhiteSpace(recipe.Name) && whRestoreMap.TryGetValue(recipe.Name.Trim(), out var foundByName))
                            {
                                whIngredient = foundByName;
                            }

                            if (whIngredient != null)
                            {`;

if (!content.includes(targetRestoreLoop)) {
    console.error('targetRestoreLoop not found!');
    process.exit(1);
}
content = content.replace(targetRestoreLoop, replaceRestoreLoop);

fs.writeFileSync(path, content.replace(/\n/g, '\r\n'), 'utf8');
console.log('Successfully updated OrderService.cs!');

