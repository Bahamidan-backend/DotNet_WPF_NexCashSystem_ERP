const fs = require('fs');

const path = 'POSSystem.WPF/Controls/RecipeManagementControl.xaml.cs';
let content = fs.readFileSync(path, 'utf8');

const target = `                    var newRecipe = new Recipe
                    {
                        Name = callFromCategry.Name,
                        Quantity = parsedQuantity,`;

const replace = `                    var newRecipe = new Recipe
                    {
                        Name = callFromCategry.Name,
                        WarehouseFromProductId = callFromCategry.Id,
                        Quantity = parsedQuantity,`;

// Normalize \r\n to \n for replacement then save
const normalized = content.replace(/\r\n/g, '\n');
const normalizedTarget = target.replace(/\r\n/g, '\n');
const normalizedReplace = replace.replace(/\r\n/g, '\n');

if (normalized.includes(normalizedTarget)) {
    const updated = normalized.replace(normalizedTarget, normalizedReplace);
    fs.writeFileSync(path, updated.replace(/\n/g, '\r\n'), 'utf8');
    console.log('Successfully added WarehouseFromProductId to newRecipe!');
} else {
    console.error('Target not found in normalized text!');
}
