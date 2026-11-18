const fs = require('fs');

const path = 'POSSystem.WPF/Business/Services/ReportService.cs';
let content = fs.readFileSync(path, 'utf8');
content = content.replace(/\r\n/g, '\n');

const targetSelect = `.Select(r => new { r.ProductId, r.Name, r.Quantity, r.portionUnit })`;
const replaceSelect = `.Select(r => new { r.ProductId, r.WarehouseFromProductId, r.Name, r.Quantity, r.portionUnit })`;

const targetLoop = `                    foreach (var r in recipes)
                    {
                        if (string.IsNullOrWhiteSpace(r.Name) || r.Quantity <= 0) continue;
                        var trimmedName = r.Name.Trim();
                        if (warehouseProductByName.TryGetValue(trimmedName, out var whProd) && whProd.Price > 0)
                        {
                            decimal convertedQty = UnitConversionHelper.ConvertQuantity(r.Quantity, r.portionUnit, whProd.PortionUnit);
                            unitRecipeCost += convertedQty * whProd.Price;
                        }
                        else if (warehouseByName.TryGetValue(trimmedName, out var ingPrice) && ingPrice > 0)
                        {
                            unitRecipeCost += r.Quantity * ingPrice;
                        }
                    }`;

const replaceLoop = `                    foreach (var r in recipes)
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
                        else if (!string.IsNullOrWhiteSpace(r.Name) && warehouseByName.TryGetValue(r.Name.Trim(), out var ingPrice) && ingPrice > 0)
                        {
                            unitRecipeCost += r.Quantity * ingPrice;
                        }
                    }`;

if (!content.includes(targetSelect)) {
    console.error('targetSelect not found!');
    process.exit(1);
}
if (!content.includes(targetLoop)) {
    console.error('targetLoop not found!');
    process.exit(1);
}

content = content.replace(targetSelect, replaceSelect);
content = content.replace(targetLoop, replaceLoop);

fs.writeFileSync(path, content.replace(/\n/g, '\r\n'), 'utf8');
console.log('Successfully updated ReportService.cs!');

