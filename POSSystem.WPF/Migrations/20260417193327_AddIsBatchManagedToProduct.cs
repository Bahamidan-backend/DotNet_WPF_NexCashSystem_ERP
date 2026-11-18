using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddIsBatchManagedToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedRevenue",
                table: "AnimalBatches");

            migrationBuilder.AddColumn<bool>(
                name: "IsBatchManaged",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 17, 22, 33, 26, 997, DateTimeKind.Local).AddTicks(8149), new DateTime(2026, 4, 17, 22, 33, 26, 997, DateTimeKind.Local).AddTicks(8150) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 17, 22, 33, 26, 997, DateTimeKind.Local).AddTicks(8170), new DateTime(2026, 4, 17, 22, 33, 26, 997, DateTimeKind.Local).AddTicks(8170) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBatchManaged",
                table: "Products");

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedRevenue",
                table: "AnimalBatches",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

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
        }
    }
}
