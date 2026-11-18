const { DatabaseSync } = require('node:sqlite');
const fs = require('node:fs');
const path = require('node:path');

const targetDbs = [
    'C:/Users/AlShaheen/Desktop/nexSystem/poss.db',
    'POSSystem.WPF/poss.db',
    'POSSystem.WPF/bin/Debug/net8.0-windows7.0/poss.db'
];

const timestamp = new Date().toISOString().replace(/[:.]/g, '-');

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

for (const dbPath of targetDbs) {
    if (!fs.existsSync(dbPath)) {
        console.log(`Skipping (not found): ${dbPath}`);
        continue;
    }

    const backupPath = `${dbPath}.backup_${timestamp}`;
    fs.copyFileSync(dbPath, backupPath);
    console.log(`Backed up ${dbPath} -> ${backupPath}`);

    const db = new DatabaseSync(dbPath);

    // Disable foreign keys temporarily for truncation
    db.exec("PRAGMA foreign_keys = OFF;");

    for (const table of tablesToClear) {
        try {
            db.exec(`DELETE FROM "${table}";`);
            db.exec(`DELETE FROM sqlite_sequence WHERE name='${table}';`);
        } catch (e) {
            console.log(`Table ${table} note: ${e.message}`);
        }
    }

    // Reset Supplier Balances to 0
    try {
        db.exec("UPDATE Suppliers SET TotalBalance = 0;");
    } catch(e) { }

    // Reset Products AvailableQtyInPOS and LastCookedQty to 0
    try {
        db.exec("UPDATE Products SET AvailableQtyInPOS = 0, LastCookedQty = 0;");
    } catch(e) { }

    // Re-enable foreign keys and run vacuum
    db.exec("PRAGMA foreign_keys = ON;");
    db.exec("VACUUM;");
    console.log(`Reset & Vacuum completed for ${dbPath}`);
}

// Remove test_audit.db files if any
const desktopDir = 'C:/Users/AlShaheen/Desktop/nexSystem';
if (fs.existsSync(desktopDir)) {
    const files = fs.readdirSync(desktopDir);
    for (const f of files) {
        if (f.startsWith('test_audit.db')) {
            try {
                fs.unlinkSync(path.join(desktopDir, f));
                console.log(`Removed leftover test db: ${f}`);
            } catch(e) { }
        }
    }
}

console.log("ALL DATABASES CLEANED & RESET SUCCESSFULLY!");

