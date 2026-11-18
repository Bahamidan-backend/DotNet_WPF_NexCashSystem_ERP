const { DatabaseSync } = require('node:sqlite');
const db = new DatabaseSync('C:/Users/AlShaheen/Desktop/nexSystem/poss.db');

const tables = db.prepare("SELECT name FROM sqlite_master WHERE type='table'").all();
for (const t of tables) {
    const fks = db.prepare(`PRAGMA foreign_key_list("${t.name}")`).all();
    for (const fk of fks) {
        if (fk.table === 'Customers') {
            console.log(`Table '${t.name}' has FK on Customers:`, fk);
            const rows = db.prepare(`SELECT * FROM "${t.name}" WHERE "${fk.from}" IN (1, 2)`).all();
            console.log(`Rows in '${t.name}':`, rows);
        }
    }
}

