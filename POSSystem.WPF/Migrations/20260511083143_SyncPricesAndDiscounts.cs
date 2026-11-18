using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class SyncPricesAndDiscounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BasePrice",
                table: "ProductSizes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BuyingPrice",
                table: "Addons",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BasePrice",
                table: "ProductSizes");

            migrationBuilder.DropColumn(
                name: "BuyingPrice",
                table: "Addons");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 2, 10, 46, 18, 221, DateTimeKind.Local).AddTicks(4540), new DateTime(2026, 5, 2, 10, 46, 18, 221, DateTimeKind.Local).AddTicks(4541) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 2, 10, 46, 18, 221, DateTimeKind.Local).AddTicks(4600), new DateTime(2026, 5, 2, 10, 46, 18, 221, DateTimeKind.Local).AddTicks(4602) });
        }
    }
}
