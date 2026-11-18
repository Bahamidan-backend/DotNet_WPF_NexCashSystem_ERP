using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class ChangeBackAndLiverToWeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackCount",
                table: "AnimalBatches");

            migrationBuilder.DropColumn(
                name: "LiverCount",
                table: "AnimalBatches");

            migrationBuilder.AddColumn<decimal>(
                name: "BackWeight",
                table: "AnimalBatches",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LiverWeight",
                table: "AnimalBatches",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 30, 15, 2, 8, 54, DateTimeKind.Local).AddTicks(1202), new DateTime(2026, 3, 30, 15, 2, 8, 54, DateTimeKind.Local).AddTicks(1203) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 30, 15, 2, 8, 54, DateTimeKind.Local).AddTicks(1222), new DateTime(2026, 3, 30, 15, 2, 8, 54, DateTimeKind.Local).AddTicks(1222) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackWeight",
                table: "AnimalBatches");

            migrationBuilder.DropColumn(
                name: "LiverWeight",
                table: "AnimalBatches");

            migrationBuilder.AddColumn<int>(
                name: "BackCount",
                table: "AnimalBatches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LiverCount",
                table: "AnimalBatches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 28, 23, 52, 18, 226, DateTimeKind.Local).AddTicks(7942), new DateTime(2026, 3, 28, 23, 52, 18, 226, DateTimeKind.Local).AddTicks(7943) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 28, 23, 52, 18, 226, DateTimeKind.Local).AddTicks(7962), new DateTime(2026, 3, 28, 23, 52, 18, 226, DateTimeKind.Local).AddTicks(7962) });
        }
    }
}
