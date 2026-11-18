const { DatabaseSync } = require('node:sqlite');
const db = new DatabaseSync('C:/Users/AlShaheen/Desktop/nexSystem/poss.db');

const tables = db.prepare("SELECT name, sql FROM sqlite_master WHERE type='table'").all();
for (const t of tables) {
    if (t.name.startsWith('sqlite_') || t.name === '__EFMigrationsHistory') continue;
    const fks = db.prepare(`PRAGMA foreign_key_list("${t.name}")`).all();
    if (fks.length > 0) {
        console.log(`\nTable: ${t.name}`);
        console.log(fks);
    }
}

