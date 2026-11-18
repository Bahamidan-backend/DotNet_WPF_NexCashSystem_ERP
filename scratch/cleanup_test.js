const { DatabaseSync } = require('node:sqlite');
const db = new DatabaseSync('C:/Users/AlShaheen/Desktop/nexSystem/poss.db');

db.exec("DELETE FROM CustomerDebts WHERE Id IN (1, 2);");
db.exec("DELETE FROM OrderItems WHERE OrderId IN (7, 8);");
db.exec("DELETE FROM StockMovements WHERE OrderId IN (7, 8);");
db.exec("DELETE FROM Orders WHERE Id IN (7, 8);");
db.exec("DELETE FROM Customers WHERE Id IN (1, 2);");

console.log('CustomerDebts remaining:', db.prepare("SELECT COUNT(*) AS c FROM CustomerDebts").all()[0].c);
console.log('Customers remaining:', db.prepare("SELECT COUNT(*) AS c FROM Customers").all()[0].c);
console.log('Orders remaining:', db.prepare("SELECT COUNT(*) AS c FROM Orders").all()[0].c);
