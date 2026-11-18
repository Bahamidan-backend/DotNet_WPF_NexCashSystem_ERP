using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddPaidAmountToCustomerDebt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "CustomerDebts",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 23, 19, 39, 32, 562, DateTimeKind.Local).AddTicks(9736), new DateTime(2026, 4, 23, 19, 39, 32, 562, DateTimeKind.Local).AddTicks(9737) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 23, 19, 39, 32, 562, DateTimeKind.Local).AddTicks(9757), new DateTime(2026, 4, 23, 19, 39, 32, 562, DateTimeKind.Local).AddTicks(9758) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "CustomerDebts");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 23, 11, 22, 15, 966, DateTimeKind.Local).AddTicks(4538), new DateTime(2026, 4, 23, 11, 22, 15, 966, DateTimeKind.Local).AddTicks(4539) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 23, 11, 22, 15, 966, DateTimeKind.Local).AddTicks(4561), new DateTime(2026, 4, 23, 11, 22, 15, 966, DateTimeKind.Local).AddTicks(4561) });
        }
    }
}
