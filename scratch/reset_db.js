const { DatabaseSync } = require('node:sqlite');
const fs = require('node:fs');
const path = require('node:path');

const targetDbs = [
    'C:/Users/AlShaheen/Desktop/nexSystem/poss.db',
    'POSSystem.WPF/poss.db',
    'POSSystem.WPF/bin/Debug/net8.0-windows7.0/poss.db'
];

const timestamp = new Date().toISOString().replace(/[:.]/g, '-');

for (const dbPath of targetDbs) {
    if (!fs.existsSync(dbPath)) continue;

    const backupPath = `${dbPath}.backup_${timestamp}`;
    fs.copyFileSync(dbPath, backupPath);
    console.log(`Backed up ${dbPath} -> ${backupPath}`);

    const db = new DatabaseSync(dbPath);

    // Disable foreign keys temporarily for truncation
    db.exec("PRAGMA foreign_keys = OFF;");

    const tablesToClear = [
        'Orders',
        'OrderItems',
        'addonAndorderItem',
        'CashBoxTransactions',
        'Shifts',
        'CustomerDebts',
        'StockMovements',
        'StockWastes',
        'Purchases',
        'PurchaseItems',
        'PurchaseReturns',
        'PurchaseReturnItems',
        'ReturnOrders',
        'ReturnOrderItems',
        'SupplierPayments',
        'Expenses',
        'BankDeposits',
        'ReceiptVouchers',
        'Deliveries',
        'HeldOrders',
        'EmployeeAdvances',
        'EmployeeSalaries',
        'AnimalBatches',
        'BatchComponents',
        'Sales',
        'SaleItems'
    ];

    for (const table of tablesToClear) {
        try {
            db.exec(`DELETE FROM "${table}";`);
            db.exec(`DELETE FROM sqlite_sequence WHERE name='${table}';`);
            console.log(`Cleared ${table} in ${dbPath}`);
        } catch (e) {
            console.log(`Table ${table} not cleared: ${e.message}`);
        }
    }

    // Reset Products AvailableQtyInPOS to 0 and LastCookedQty to 0
    try {
        db.exec("UPDATE Products SET AvailableQtyInPOS = 0, LastCookedQty = 0;");
        console.log(`Reset Products stock counters in ${dbPath}`);
    } catch(e) {
        console.log(`Products reset error: ${e.message}`);
    }

    // Re-enable foreign keys and run vacuum
    db.exec("PRAGMA foreign_keys = ON;");
    db.exec("VACUUM;");
    console.log(`Vacuum completed for ${dbPath}`);
}

