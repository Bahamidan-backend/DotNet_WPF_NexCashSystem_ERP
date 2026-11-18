using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddIsMainToRecipe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMainIngredient",
                table: "Recipes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 8, 16, 22, 43, 583, DateTimeKind.Local).AddTicks(4835), new DateTime(2026, 6, 8, 16, 22, 43, 583, DateTimeKind.Local).AddTicks(4837) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 8, 16, 22, 43, 583, DateTimeKind.Local).AddTicks(4897), new DateTime(2026, 6, 8, 16, 22, 43, 583, DateTimeKind.Local).AddTicks(4898) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMainIngredient",
                table: "Recipes");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 8, 15, 16, 44, 613, DateTimeKind.Local).AddTicks(4587), new DateTime(2026, 6, 8, 15, 16, 44, 613, DateTimeKind.Local).AddTicks(4590) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 8, 15, 16, 44, 613, DateTimeKind.Local).AddTicks(4666), new DateTime(2026, 6, 8, 15, 16, 44, 613, DateTimeKind.Local).AddTicks(4667) });
        }
    }
}
