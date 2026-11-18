using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Warehouses_WarehouseId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseItems_warehouseFromProducts_ProductId",
                table: "PurchaseItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Warehouses_WarehouseId",
                table: "Purchases");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Categories",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 20, 17, 40, 6, 796, DateTimeKind.Local).AddTicks(463), new DateTime(2026, 3, 20, 17, 40, 6, 796, DateTimeKind.Local).AddTicks(463) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 20, 17, 40, 6, 796, DateTimeKind.Local).AddTicks(483), new DateTime(2026, 3, 20, 17, 40, 6, 796, DateTimeKind.Local).AddTicks(484) });

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Warehouses_WarehouseId",
                table: "Products",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseItems_warehouseFromProducts_ProductId",
                table: "PurchaseItems",
                column: "ProductId",
                principalTable: "warehouseFromProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Warehouses_WarehouseId",
                table: "Purchases",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Warehouses_WarehouseId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseItems_warehouseFromProducts_ProductId",
                table: "PurchaseItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Warehouses_WarehouseId",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Categories");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 18, 5, 22, 1, 210, DateTimeKind.Local).AddTicks(306), new DateTime(2026, 3, 18, 5, 22, 1, 210, DateTimeKind.Local).AddTicks(307) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 18, 5, 22, 1, 210, DateTimeKind.Local).AddTicks(360), new DateTime(2026, 3, 18, 5, 22, 1, 210, DateTimeKind.Local).AddTicks(361) });

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Warehouses_WarehouseId",
                table: "Products",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseItems_warehouseFromProducts_ProductId",
                table: "PurchaseItems",
                column: "ProductId",
                principalTable: "warehouseFromProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Warehouses_WarehouseId",
                table: "Purchases",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id");
        }
    }
}
