using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeColumnsToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalValue",
                table: "Products",
                newName: "LastCookedQty");

            migrationBuilder.RenameColumn(
                name: "StockQuantity",
                table: "Products",
                newName: "AvailableQtyInPOS");

            migrationBuilder.AddColumn<int>(
                name: "YieldRate",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 8, 15, 16, 44, 613, DateTimeKind.Local).AddTicks(4587), new DateTime(2026, 6, 8, 15, 16, 44, 613, DateTimeKind.Local).AddTicks(4590) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 8, 15, 16, 44, 613, DateTimeKind.Local).AddTicks(4666), new DateTime(2026, 6, 8, 15, 16, 44, 613, DateTimeKind.Local).AddTicks(4667) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YieldRate",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "LastCookedQty",
                table: "Products",
                newName: "TotalValue");

            migrationBuilder.RenameColumn(
                name: "AvailableQtyInPOS",
                table: "Products",
                newName: "StockQuantity");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 11, 11, 31, 41, 107, DateTimeKind.Local).AddTicks(7560), new DateTime(2026, 5, 11, 11, 31, 41, 107, DateTimeKind.Local).AddTicks(7561) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 11, 11, 31, 41, 107, DateTimeKind.Local).AddTicks(7580), new DateTime(2026, 5, 11, 11, 31, 41, 107, DateTimeKind.Local).AddTicks(7581) });
        }
    }
}
