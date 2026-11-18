using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddHeldOrdersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductSizes_ProductId",
                table: "ProductSizes");

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "Expenses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "HeldOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HoldTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CustomerName = table.Column<string>(type: "TEXT", nullable: true),
                    CustomerPhone = table.Column<string>(type: "TEXT", nullable: true),
                    OrderType = table.Column<string>(type: "TEXT", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountCode = table.Column<string>(type: "TEXT", nullable: true),
                    HasDiscount = table.Column<bool>(type: "INTEGER", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeliveryAddress = table.Column<string>(type: "TEXT", nullable: true),
                    DeliveryDriver = table.Column<string>(type: "TEXT", nullable: true),
                    DeliveryFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ShiftId = table.Column<int>(type: "INTEGER", nullable: true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true),
                    ItemsJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeldOrders", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "PBKDF2$100000$jZkgt758JEJDHVC+huwxxA==$PnA1ecUDcDrC5plCdmhlA4cHgB0vHWvkMe3QqGgn330=");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "Password",
                value: "PBKDF2$100000$3rYlCNpbkRd3akhogpLkRg==$zheJcEeD7KDIYHsabGt4CuoOUvtVeSDc7cKMmUXA5Ls=");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnOrders_ReturnDate",
                table: "ReturnOrders",
                column: "ReturnDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSizes_ProductId_SizeName",
                table: "ProductSizes",
                columns: new[] { "ProductId", "SizeName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ExpenseDate",
                table: "Expenses",
                column: "ExpenseDate");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDebts_IsPaid_DebtDate",
                table: "CustomerDebts",
                columns: new[] { "IsPaid", "DebtDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDebts_ShiftId",
                table: "CustomerDebts",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_CashBoxTransactions_TransactionDate",
                table: "CashBoxTransactions",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalBatches_CreatedAt",
                table: "AnimalBatches",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalBatches_Type",
                table: "AnimalBatches",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_HeldOrders_HoldTime",
                table: "HeldOrders",
                column: "HoldTime");

            migrationBuilder.CreateIndex(
                name: "IX_HeldOrders_ShiftId",
                table: "HeldOrders",
                column: "ShiftId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HeldOrders");

            migrationBuilder.DropIndex(
                name: "IX_ReturnOrders_ReturnDate",
                table: "ReturnOrders");

            migrationBuilder.DropIndex(
                name: "IX_ProductSizes_ProductId_SizeName",
                table: "ProductSizes");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_ExpenseDate",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_CustomerDebts_IsPaid_DebtDate",
                table: "CustomerDebts");

            migrationBuilder.DropIndex(
                name: "IX_CustomerDebts_ShiftId",
                table: "CustomerDebts");

            migrationBuilder.DropIndex(
                name: "IX_CashBoxTransactions_TransactionDate",
                table: "CashBoxTransactions");

            migrationBuilder.DropIndex(
                name: "IX_AnimalBatches_CreatedAt",
                table: "AnimalBatches");

            migrationBuilder.DropIndex(
                name: "IX_AnimalBatches_Type",
                table: "AnimalBatches");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Expenses");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "PBKDF2$100000$54tbZfqTF8VARSv9ATnoOQ==$zpcRoOx/XdH+OCxyXf/naqLTXsAaJl/x3hH17y17f+s=");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "Password",
                value: "PBKDF2$100000$W7NTW3TxGitn1KYN6zoUtw==$u/yuTZw6sLHGT08OSZkmm8s1ZKW/MwBueleXfXV8t/8=");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSizes_ProductId",
                table: "ProductSizes",
                column: "ProductId");
        }
    }
}
