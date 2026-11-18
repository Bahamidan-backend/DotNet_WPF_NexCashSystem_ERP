using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_Purchases_PurchaseId",
                table: "SupplierPayments");

            migrationBuilder.DropIndex(
                name: "IX_SupplierPayments_PurchaseId",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "PurchaseId",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "IsReturn",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "Purchases");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Recipes",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.CreateTable(
                name: "PurchaseReturns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReturnNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OriginalPurchaseId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ReturnMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseReturns_Purchases_OriginalPurchaseId",
                        column: x => x.OriginalPurchaseId,
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReturns_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReturnItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PurchaseReturnId = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalPurchaseItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReasonCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReturnItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseReturnItems_PurchaseItems_OriginalPurchaseItemId",
                        column: x => x.OriginalPurchaseItemId,
                        principalTable: "PurchaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseReturnItems_PurchaseReturns_PurchaseReturnId",
                        column: x => x.PurchaseReturnId,
                        principalTable: "PurchaseReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 22, 0, 2, 57, 319, DateTimeKind.Local).AddTicks(4717), new DateTime(2026, 3, 22, 0, 2, 57, 319, DateTimeKind.Local).AddTicks(4719) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 22, 0, 2, 57, 319, DateTimeKind.Local).AddTicks(4782), new DateTime(2026, 3, 22, 0, 2, 57, 319, DateTimeKind.Local).AddTicks(4784) });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnItems_OriginalPurchaseItemId",
                table: "PurchaseReturnItems",
                column: "OriginalPurchaseItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnItems_PurchaseReturnId",
                table: "PurchaseReturnItems",
                column: "PurchaseReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_OriginalPurchaseId",
                table: "PurchaseReturns",
                column: "OriginalPurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_ReturnNumber",
                table: "PurchaseReturns",
                column: "ReturnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_UserId",
                table: "PurchaseReturns",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseReturnItems");

            migrationBuilder.DropTable(
                name: "PurchaseReturns");

            migrationBuilder.AddColumn<int>(
                name: "PurchaseId",
                table: "SupplierPayments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "Recipes",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AddColumn<bool>(
                name: "IsReturn",
                table: "Purchases",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "Purchases",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 20, 21, 46, 24, 504, DateTimeKind.Local).AddTicks(6594), new DateTime(2026, 3, 20, 21, 46, 24, 504, DateTimeKind.Local).AddTicks(6595) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 20, 21, 46, 24, 504, DateTimeKind.Local).AddTicks(6616), new DateTime(2026, 3, 20, 21, 46, 24, 504, DateTimeKind.Local).AddTicks(6617) });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_PurchaseId",
                table: "SupplierPayments",
                column: "PurchaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_Purchases_PurchaseId",
                table: "SupplierPayments",
                column: "PurchaseId",
                principalTable: "Purchases",
                principalColumn: "Id");
        }
    }
}
