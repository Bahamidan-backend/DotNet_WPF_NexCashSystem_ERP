using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseCostAndSupplierToAnimalBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedRevenue",
                table: "AnimalBatches",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PurchaseCost",
                table: "AnimalBatches",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SupplierId",
                table: "AnimalBatches",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 8, 7, 2, 36, 880, DateTimeKind.Local).AddTicks(410), new DateTime(2026, 4, 8, 7, 2, 36, 880, DateTimeKind.Local).AddTicks(411) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 8, 7, 2, 36, 880, DateTimeKind.Local).AddTicks(435), new DateTime(2026, 4, 8, 7, 2, 36, 880, DateTimeKind.Local).AddTicks(435) });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalBatches_SupplierId",
                table: "AnimalBatches",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_AnimalBatches_Suppliers_SupplierId",
                table: "AnimalBatches",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnimalBatches_Suppliers_SupplierId",
                table: "AnimalBatches");

            migrationBuilder.DropIndex(
                name: "IX_AnimalBatches_SupplierId",
                table: "AnimalBatches");

            migrationBuilder.DropColumn(
                name: "ExpectedRevenue",
                table: "AnimalBatches");

            migrationBuilder.DropColumn(
                name: "PurchaseCost",
                table: "AnimalBatches");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "AnimalBatches");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 30, 21, 16, 54, 802, DateTimeKind.Local).AddTicks(2242), new DateTime(2026, 3, 30, 21, 16, 54, 802, DateTimeKind.Local).AddTicks(2245) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 30, 21, 16, 54, 802, DateTimeKind.Local).AddTicks(2306), new DateTime(2026, 3, 30, 21, 16, 54, 802, DateTimeKind.Local).AddTicks(2308) });
        }
    }
}
