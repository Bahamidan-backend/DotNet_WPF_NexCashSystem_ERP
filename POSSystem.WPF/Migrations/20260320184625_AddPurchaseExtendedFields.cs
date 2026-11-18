using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseExtendedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PurchaseId",
                table: "SupplierPayments",
                type: "INTEGER",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 20, 20, 19, 24, 418, DateTimeKind.Local).AddTicks(934), new DateTime(2026, 3, 20, 20, 19, 24, 418, DateTimeKind.Local).AddTicks(935) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 20, 20, 19, 24, 418, DateTimeKind.Local).AddTicks(954), new DateTime(2026, 3, 20, 20, 19, 24, 418, DateTimeKind.Local).AddTicks(955) });
        }
    }
}
