const { DatabaseSync } = require('node:sqlite');
const fs = require('fs');

const dbs = [
    'POSSystem.WPF/poss.db',
    'POSSystem.WPF/bin/Debug/net8.0-windows7.0/poss.db',
    'C:/Users/AlShaheen/Desktop/nexSystem/poss.db'
];

for (const dbPath of dbs) {
    console.log(`\n========================================`);
    console.log(`Synchronizing DB: ${dbPath}`);
    if (!fs.existsSync(dbPath)) {
        console.log(`File does not exist, skipping.`);
        continue;
    }

    const db = new DatabaseSync(dbPath);

    function columnExists(table, col) {
        try {
            const cols = db.prepare(`PRAGMA table_info("${table}");`).all();
            return cols.some(c => c.name.toLowerCase() === col.toLowerCase());
        } catch {
            return false;
        }
    }

    function addColumnIfMissing(table, col, def) {
        if (!columnExists(table, col)) {
            try {
                console.log(`Adding ${table}.${col}...`);
                db.exec(`ALTER TABLE "${table}" ADD "${col}" ${def};`);
                console.log(`  Added ${table}.${col} successfully.`);
            } catch (err) {
                console.log(`  Error adding ${table}.${col}: ${err.message}`);
            }
        } else {
            console.log(`  ${table}.${col} already exists.`);
        }
    }

    // 1. StockMovements
    db.exec(`
        CREATE TABLE IF NOT EXISTS "StockMovements" (
            "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
            "WarehouseFromProductId" INTEGER NOT NULL,
            "WarehouseId" INTEGER,
            "MovementType" INTEGER NOT NULL,
            "Quantity" TEXT NOT NULL,
            "BalanceBefore" TEXT NOT NULL,
            "BalanceAfter" TEXT NOT NULL,
            "UnitCost" TEXT NOT NULL,
            "TotalCost" TEXT NOT NULL,
            "MovementDate" TEXT NOT NULL,
            "ReferenceNumber" TEXT,
            "OrderId" INTEGER,
            "PurchaseId" INTEGER,
            "UserId" INTEGER,
            "Notes" TEXT,
            "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now','localtime'))
        );
    `);
    addColumnIfMissing('StockMovements', 'CreatedAt', "TEXT NOT NULL DEFAULT (datetime('now','localtime'))");
    addColumnIfMissing('StockMovements', 'MovementDate', "TEXT NOT NULL DEFAULT (datetime('now','localtime'))");
    addColumnIfMissing('StockMovements', 'ReferenceNumber', "TEXT");
    addColumnIfMissing('StockMovements', 'OrderId', "INTEGER");
    addColumnIfMissing('StockMovements', 'PurchaseId', "INTEGER");
    addColumnIfMissing('StockMovements', 'UserId', "INTEGER");
    addColumnIfMissing('StockMovements', 'Notes', "TEXT");

    // 2. warehouseFromProducts
    addColumnIfMissing('warehouseFromProducts', 'MinimumStockLevel', "TEXT NOT NULL DEFAULT '5'");

    // 3. Recipes
    addColumnIfMissing('Recipes', 'WarehouseFromProductId', "INTEGER");

    // 4. Orders
    addColumnIfMissing('Orders', 'TaxRate', "TEXT NOT NULL DEFAULT '0.15'");
    addColumnIfMissing('Orders', 'TaxAmount', "TEXT NOT NULL DEFAULT '0'");

    // 5. Expenses
    addColumnIfMissing('Expenses', 'PaymentMethod', "INTEGER NOT NULL DEFAULT 0");

    // 6. HeldOrders
    db.exec(`
        CREATE TABLE IF NOT EXISTS "HeldOrders" (
            "Id" TEXT PRIMARY KEY NOT NULL,
            "HoldTime" TEXT NOT NULL,
            "CustomerName" TEXT,
            "CustomerPhone" TEXT,
            "OrderType" TEXT,
            "DiscountAmount" TEXT NOT NULL DEFAULT '0',
            "DiscountCode" TEXT,
            "HasDiscount" INTEGER NOT NULL DEFAULT 0,
            "TotalAmount" TEXT NOT NULL DEFAULT '0',
            "DeliveryAddress" TEXT,
            "DeliveryDriver" TEXT,
            "DeliveryFee" TEXT NOT NULL DEFAULT '0',
            "ShiftId" INTEGER,
            "UserId" INTEGER,
            "ItemsJson" TEXT
        );
        CREATE INDEX IF NOT EXISTS "IX_HeldOrders_HoldTime" ON "HeldOrders" ("HoldTime");
        CREATE INDEX IF NOT EXISTS "IX_HeldOrders_ShiftId" ON "HeldOrders" ("ShiftId");
    `);

    // 7. StockWastes
    db.exec(`
        CREATE TABLE IF NOT EXISTS "StockWastes" (
            "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
            "WarehouseFromProductId" INTEGER NOT NULL,
            "WarehouseId" INTEGER,
            "Quantity" TEXT NOT NULL,
            "PortionUnit" INTEGER,
            "UnitCost" TEXT NOT NULL,
            "TotalCost" TEXT NOT NULL,
            "Reason" INTEGER NOT NULL,
            "WasteDate" TEXT NOT NULL,
            "ReportedByUserId" INTEGER,
            "Notes" TEXT,
            "Name" TEXT,
            "Description" TEXT,
            "CreatedAt" TEXT NOT NULL DEFAULT (datetime('now','localtime')),
            "UpdatedAt" TEXT NOT NULL DEFAULT (datetime('now','localtime')),
            "IsActive" INTEGER NOT NULL DEFAULT 1
        );
        CREATE INDEX IF NOT EXISTS "IX_StockWastes_WarehouseFromProductId" ON "StockWastes" ("WarehouseFromProductId");
        CREATE INDEX IF NOT EXISTS "IX_StockWastes_WarehouseId" ON "StockWastes" ("WarehouseId");
        CREATE INDEX IF NOT EXISTS "IX_StockWastes_WasteDate" ON "StockWastes" ("WasteDate");
    `);

    // 8. Synchronize __EFMigrationsHistory
    const targetMigrations = [
        '20261106070829_AddHeldOrdersTable',
        '20261106121010_AddStockMovementsTable',
        '20261106130205_AddMinimumStockLevelToWarehouseFromProduct',
        '20261106135723_AddRecipeForeignKeyWasteAndOrderTax'
    ];

    const existingMigrations = new Set(
        db.prepare('SELECT "MigrationId" FROM "__EFMigrationsHistory";').all().map(m => m.MigrationId)
    );

    const insertMigration = db.prepare('INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (?, ?);');
    for (const m of targetMigrations) {
        if (!existingMigrations.has(m)) {
            console.log(`Recording migration in history: ${m}`);
            insertMigration.run(m, '8.0.10');
        }
    }

    db.close();
    console.log(`Database sync complete for: ${dbPath}`);
}

