const { DatabaseSync } = require('node:sqlite');
const db = new DatabaseSync('C:/Users/AlShaheen/Desktop/nexSystem/poss.db');

console.log('warehouseFromProducts:');
console.log(db.prepare("SELECT Id, Name, WarehouseId, StockQuantity FROM warehouseFromProducts").all());

