const { DatabaseSync } = require('node:sqlite');
const fs = require('fs');

const dbs = [
    'C:\\Users\\AlShaheen\\source\\repos\\PosSystemVersion55\\POSSystem.WPF\\poss.db',
    'C:\\Users\\AlShaheen\\source\\repos\\PosSystemVersion55\\POSSystem.WPF\\bin\\Debug\\net8.0-windows7.0\\poss.db',
    'C:\\Users\\AlShaheen\\Desktop\\nexSystem\\poss.db'
];

for (const dbPath of dbs) {
    console.log(`\n========================================`);
    console.log(`Checking: ${dbPath}`);
    if (!fs.existsSync(dbPath)) {
        console.log(`FILE DOES NOT EXIST!`);
        continue;
    }
    try {
        const db = new DatabaseSync(dbPath);
        const columns = db.prepare("PRAGMA table_info(warehouseFromProducts);").all();
        console.log(`warehouseFromProducts columns (${columns.length}):`);
        console.log(columns.map(c => c.name).join(', '));

        const hasMinStock = columns.some(c => c.name === 'MinimumStockLevel');
        console.log(`Has MinimumStockLevel: ${hasMinStock ? '✅ YES' : '❌ NO'}`);

        const tables = db.prepare("SELECT name FROM sqlite_master WHERE type='table';").all().map(t => t.name);
        console.log(`Tables in DB: ${tables.join(', ')}`);
        
        db.close();
    } catch (err) {
        console.log(`Error reading DB: ${err.message}`);
    }
}

