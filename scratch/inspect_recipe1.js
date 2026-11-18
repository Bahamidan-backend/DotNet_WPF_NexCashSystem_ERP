const { DatabaseSync } = require('node:sqlite');
const db = new DatabaseSync('C:/Users/AlShaheen/Desktop/nexSystem/poss.db');

console.log('Recipes for Product 1:');
console.log(db.prepare("SELECT * FROM Recipes WHERE ProductId = 1").all());

