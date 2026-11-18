const { DatabaseSync } = require('node:sqlite');
const db = new DatabaseSync('C:/Users/AlShaheen/Desktop/nexSystem/poss.db');

console.log('Orders table DDL:');
console.log(db.prepare("SELECT sql FROM sqlite_master WHERE name='Orders'").all()[0].sql);

