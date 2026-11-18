using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddIsInHouseToAddon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsInHouse",
                table: "Addons",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 15, 14, 14, 9, 209, DateTimeKind.Local).AddTicks(2989), new DateTime(2026, 3, 15, 14, 14, 9, 209, DateTimeKind.Local).AddTicks(2990) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 15, 14, 14, 9, 209, DateTimeKind.Local).AddTicks(3009), new DateTime(2026, 3, 15, 14, 14, 9, 209, DateTimeKind.Local).AddTicks(3010) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsInHouse",
                table: "Addons");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 15, 3, 12, 51, 438, DateTimeKind.Local).AddTicks(9567), new DateTime(2026, 3, 15, 3, 12, 51, 438, DateTimeKind.Local).AddTicks(9569) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 15, 3, 12, 51, 438, DateTimeKind.Local).AddTicks(9641), new DateTime(2026, 3, 15, 3, 12, 51, 438, DateTimeKind.Local).AddTicks(9642) });
        }
    }
}
