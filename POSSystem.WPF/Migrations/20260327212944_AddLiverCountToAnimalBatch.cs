using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddLiverCountToAnimalBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                values: new object[] { new DateTime(2026, 3, 28, 0, 29, 43, 733, DateTimeKind.Local).AddTicks(7497), new DateTime(2026, 3, 28, 0, 29, 43, 733, DateTimeKind.Local).AddTicks(7500) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 28, 0, 29, 43, 733, DateTimeKind.Local).AddTicks(7567), new DateTime(2026, 3, 28, 0, 29, 43, 733, DateTimeKind.Local).AddTicks(7568) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LiverCount",
                table: "AnimalBatches");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 27, 2, 3, 8, 709, DateTimeKind.Local).AddTicks(9504), new DateTime(2026, 3, 27, 2, 3, 8, 709, DateTimeKind.Local).AddTicks(9505) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 27, 2, 3, 8, 709, DateTimeKind.Local).AddTicks(9531), new DateTime(2026, 3, 27, 2, 3, 8, 709, DateTimeKind.Local).AddTicks(9532) });
        }
    }
}
