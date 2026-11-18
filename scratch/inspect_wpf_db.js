const { DatabaseSync } = require('node:sqlite');
const db = new DatabaseSync('POSSystem.WPF/poss.db');

console.log('Orders in POSSystem.WPF/poss.db:');
console.log(db.prepare("SELECT Id, OrderNumber, UserId, ShiftId, PaymentMethod, Total FROM Orders ORDER BY Id DESC LIMIT 5").all());

console.log('\nCustomerDebts in POSSystem.WPF/poss.db:');
console.log(db.prepare("SELECT * FROM CustomerDebts ORDER BY Id DESC LIMIT 5").all());

