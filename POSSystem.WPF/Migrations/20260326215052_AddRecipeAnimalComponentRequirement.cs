using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeAnimalComponentRequirement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnimalComponentRequirement",
                table: "Recipes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 27, 0, 50, 51, 584, DateTimeKind.Local).AddTicks(6759), new DateTime(2026, 3, 27, 0, 50, 51, 584, DateTimeKind.Local).AddTicks(6760) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 27, 0, 50, 51, 584, DateTimeKind.Local).AddTicks(6783), new DateTime(2026, 3, 27, 0, 50, 51, 584, DateTimeKind.Local).AddTicks(6783) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnimalComponentRequirement",
                table: "Recipes");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 26, 23, 50, 53, 866, DateTimeKind.Local).AddTicks(8735), new DateTime(2026, 3, 26, 23, 50, 53, 866, DateTimeKind.Local).AddTicks(8737) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 26, 23, 50, 53, 866, DateTimeKind.Local).AddTicks(8803), new DateTime(2026, 3, 26, 23, 50, 53, 866, DateTimeKind.Local).AddTicks(8805) });
        }
    }
}
