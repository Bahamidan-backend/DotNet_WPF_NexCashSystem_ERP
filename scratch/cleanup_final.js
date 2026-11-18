const { DatabaseSync } = require('node:sqlite');
const db = new DatabaseSync('C:/Users/AlShaheen/Desktop/nexSystem/poss.db');

db.exec("DELETE FROM CustomerDebts WHERE Id = 3;");
db.exec("DELETE FROM OrderItems WHERE OrderId = 9;");
db.exec("DELETE FROM StockMovements WHERE OrderId = 9;");
db.exec("DELETE FROM Orders WHERE Id = 9;");
db.exec("DELETE FROM Customers WHERE Id = 3;");

console.log('CustomerDebts remaining:', db.prepare("SELECT COUNT(*) AS c FROM CustomerDebts").all()[0].c);
console.log('Customers remaining:', db.prepare("SELECT COUNT(*) AS c FROM Customers").all()[0].c);
console.log('Orders remaining:', db.prepare("SELECT COUNT(*) AS c FROM Orders").all()[0].c);
console.log('FK Check:', db.prepare("PRAGMA foreign_key_check").all());

