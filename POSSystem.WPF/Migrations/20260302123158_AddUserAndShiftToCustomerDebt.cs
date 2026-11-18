using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAndShiftToCustomerDebt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShiftId",
                table: "CustomerDebts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "CustomerDebts",
                type: "INTEGER",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "CustomerDebts");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "CustomerDebts");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 25, 15, 15, 7, 527, DateTimeKind.Local).AddTicks(9203), new DateTime(2026, 2, 25, 15, 15, 7, 527, DateTimeKind.Local).AddTicks(9203) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 25, 15, 15, 7, 527, DateTimeKind.Local).AddTicks(9224), new DateTime(2026, 2, 25, 15, 15, 7, 527, DateTimeKind.Local).AddTicks(9224) });
        }
    }
}
