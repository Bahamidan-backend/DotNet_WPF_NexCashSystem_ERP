const { DatabaseSync } = require('node:sqlite');
const db = new DatabaseSync('C:/Users/AlShaheen/Desktop/nexSystem/poss.db');

console.log('CustomerDebts schema:');
console.log(db.prepare("SELECT sql FROM sqlite_master WHERE name='CustomerDebts'").all()[0].sql);

console.log('\nCustomerDebts foreign keys:');
console.log(db.prepare("PRAGMA foreign_key_list(CustomerDebts)").all());

console.log('\nForeign key check across entire DB:');
console.log(db.prepare("PRAGMA foreign_key_check").all());

console.log('\nLast 5 orders:');
console.log(db.prepare("SELECT Id, OrderNumber, UserId, ShiftId, PaymentMethod, Total FROM Orders ORDER BY Id DESC LIMIT 5").all());

console.log('\nLast 5 CustomerDebts:');
console.log(db.prepare("SELECT * FROM CustomerDebts ORDER BY Id DESC LIMIT 5").all());

console.log('\nLast 5 Customers:');
console.log(db.prepare("SELECT * FROM Customers ORDER BY Id DESC LIMIT 5").all());

