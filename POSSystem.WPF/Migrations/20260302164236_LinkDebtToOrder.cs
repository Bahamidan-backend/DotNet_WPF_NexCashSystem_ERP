using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class LinkDebtToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderId",
                table: "CustomerDebts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 19, 42, 35, 336, DateTimeKind.Local).AddTicks(9271), new DateTime(2026, 3, 2, 19, 42, 35, 336, DateTimeKind.Local).AddTicks(9272) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 19, 42, 35, 336, DateTimeKind.Local).AddTicks(9315), new DateTime(2026, 3, 2, 19, 42, 35, 336, DateTimeKind.Local).AddTicks(9316) });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDebts_OrderId",
                table: "CustomerDebts",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerDebts_Orders_OrderId",
                table: "CustomerDebts",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerDebts_Orders_OrderId",
                table: "CustomerDebts");

            migrationBuilder.DropIndex(
                name: "IX_CustomerDebts_OrderId",
                table: "CustomerDebts");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "CustomerDebts");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 15, 31, 56, 693, DateTimeKind.Local).AddTicks(6371), new DateTime(2026, 3, 2, 15, 31, 56, 693, DateTimeKind.Local).AddTicks(6373) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 15, 31, 56, 693, DateTimeKind.Local).AddTicks(6440), new DateTime(2026, 3, 2, 15, 31, 56, 693, DateTimeKind.Local).AddTicks(6441) });
        }
    }
}
