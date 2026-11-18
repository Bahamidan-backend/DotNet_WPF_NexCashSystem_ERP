using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddAddonIngredients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BaseAddonId",
                table: "Addons",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BaseAddonQuantity",
                table: "Addons",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 28, 19, 21, 14, 610, DateTimeKind.Local).AddTicks(2105), new DateTime(2026, 3, 28, 19, 21, 14, 610, DateTimeKind.Local).AddTicks(2105) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 28, 19, 21, 14, 610, DateTimeKind.Local).AddTicks(2125), new DateTime(2026, 3, 28, 19, 21, 14, 610, DateTimeKind.Local).AddTicks(2126) });

            migrationBuilder.CreateIndex(
                name: "IX_Addons_BaseAddonId",
                table: "Addons",
                column: "BaseAddonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Addons_Addons_BaseAddonId",
                table: "Addons",
                column: "BaseAddonId",
                principalTable: "Addons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addons_Addons_BaseAddonId",
                table: "Addons");

            migrationBuilder.DropIndex(
                name: "IX_Addons_BaseAddonId",
                table: "Addons");

            migrationBuilder.DropColumn(
                name: "BaseAddonId",
                table: "Addons");

            migrationBuilder.DropColumn(
                name: "BaseAddonQuantity",
                table: "Addons");

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
    }
}
