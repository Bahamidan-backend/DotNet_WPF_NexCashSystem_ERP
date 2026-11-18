const fs = require('fs');
const file = 'POSSystem.WPF/Business/Services/OrderService.cs';
let content = fs.readFileSync(file, 'utf8');

// Normalize line endings for replacement
const isCRLF = content.includes('\r\n');
const lines = content.split(/\r?\n/);

let idx1 = -1;
let idx2 = -1;

for (let i = 0; i < lines.length; i++) {
    if (lines[i].includes('whIngredient.StockQuantity = Math.Max(0, whIngredient.StockQuantity - ingredientQty);')) {
        idx1 = i;
    }
    if (lines[i].includes('whIngredient.StockQuantity += ingredientQty;')) {
        idx2 = i;
    }
}

console.log('idx1:', idx1, 'idx2:', idx2);

if (idx1 !== -1 && idx2 !== -1) {
    const rep1 = [
        '                                decimal convertedQty = UnitConversionHelper.ConvertQuantity(ingredientQty, recipe.portionUnit, whIngredient.PortionUnit);',
        '                                decimal beforeQty = whIngredient.StockQuantity;',
        '                                whIngredient.StockQuantity = Math.Max(0, whIngredient.StockQuantity - convertedQty);',
        '                                decimal afterQty = whIngredient.StockQuantity;',
        '                                whIngredient.UpdatedAt = now;',
        '                                _unitOfWork.warehouseFromProduct.Update(whIngredient);',
        '',
        '                                // تسجيل حركة استهلاك المخزون (StockMovement)',
        '                                await _unitOfWork.StockMovements.AddAsync(new StockMovement',
        '                                {',
        '                                    WarehouseFromProductId = whIngredient.Id,',
        '                                    WarehouseId = whIngredient.WarehouseId,',
        '                                    MovementType = StockMovementType.Sale,',
        '                                    Quantity = -convertedQty,',
        '                                    BalanceBefore = beforeQty,',
        '                                    BalanceAfter = afterQty,',
        '                                    UnitCost = whIngredient.Price,',
        '                                    TotalCost = convertedQty * whIngredient.Price,',
        '                                    OrderId = item.OrderId,',
        '                                    Notes = $"استهلاك مكونات الوجبة \'{product.Name}\' للطلب #{item.OrderId}",',
        '                                    MovementDate = now,',
        '                                    CreatedAt = now',
        '                                });'
    ];

    const rep2 = [
        '                                decimal convertedQty = UnitConversionHelper.ConvertQuantity(ingredientQty, recipe.portionUnit, whIngredient.PortionUnit);',
        '                                decimal beforeQty = whIngredient.StockQuantity;',
        '                                whIngredient.StockQuantity += convertedQty;',
        '                                decimal afterQty = whIngredient.StockQuantity;',
        '                                whIngredient.UpdatedAt = now;',
        '                                _unitOfWork.warehouseFromProduct.Update(whIngredient);',
        '',
        '                                // تسجيل حركة استعادة المرتجع في المخزون (StockMovement)',
        '                                await _unitOfWork.StockMovements.AddAsync(new StockMovement',
        '                                {',
        '                                    WarehouseFromProductId = whIngredient.Id,',
        '                                    WarehouseId = whIngredient.WarehouseId,',
        '                                    MovementType = StockMovementType.SaleReturn,',
        '                                    Quantity = convertedQty,',
        '                                    BalanceBefore = beforeQty,',
        '                                    BalanceAfter = afterQty,',
        '                                    UnitCost = whIngredient.Price,',
        '                                    TotalCost = convertedQty * whIngredient.Price,',
        '                                    OrderId = item.OrderId,',
        '                                    Notes = $"استعادة مكونات الوجبة \'{product.Name}\' من مرتجع الطلب #{item.OrderId}",',
        '                                    MovementDate = now,',
        '                                    CreatedAt = now',
        '                                });'
    ];

    // Replace 3 lines at idx2 first (higher index)
    lines.splice(idx2, 3, ...rep2);
    // Then replace 3 lines at idx1
    lines.splice(idx1, 3, ...rep1);

    const le = isCRLF ? '\r\n' : '\n';
    fs.writeFileSync(file, lines.join(le), 'utf8');
    console.log('SUCCESSFULLY_UPDATED');
} else {
    console.log('COULD_NOT_FIND_INDICES');
}

