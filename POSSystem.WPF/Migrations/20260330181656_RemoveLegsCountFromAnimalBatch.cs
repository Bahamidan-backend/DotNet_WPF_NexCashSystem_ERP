using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegsCountFromAnimalBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LegsCount",
                table: "AnimalBatches");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 30, 21, 16, 54, 802, DateTimeKind.Local).AddTicks(2242), new DateTime(2026, 3, 30, 21, 16, 54, 802, DateTimeKind.Local).AddTicks(2245) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 30, 21, 16, 54, 802, DateTimeKind.Local).AddTicks(2306), new DateTime(2026, 3, 30, 21, 16, 54, 802, DateTimeKind.Local).AddTicks(2308) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LegsCount",
                table: "AnimalBatches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

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
    }
}
