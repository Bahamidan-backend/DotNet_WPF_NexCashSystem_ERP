const { DatabaseSync } = require('node:sqlite');
const db = new DatabaseSync('C:/Users/AlShaheen/Desktop/nexSystem/poss.db');
const tables = db.prepare("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name").all();
for (const t of tables) {
  try {
    const count = db.prepare(`SELECT COUNT(*) as c FROM "${t.name}"`).get().c;
    console.log(`${t.name}: ${count}`);
  } catch(e) {
    console.log(`${t.name}: error ${e.message}`);
  }
}

