const { DatabaseSync } = require('node:sqlite');
const db = new DatabaseSync('C:/Users/AlShaheen/Desktop/nexSystem/poss.db');

console.log('Shifts:');
console.log(db.prepare("SELECT * FROM Shifts").all());

