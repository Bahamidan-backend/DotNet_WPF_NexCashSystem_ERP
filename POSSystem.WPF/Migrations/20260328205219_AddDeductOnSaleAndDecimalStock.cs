using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddDeductOnSaleAndDecimalStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addons_Addons_BaseAddonId",
                table: "Addons");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Addons",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<decimal>(
                name: "BaseAddonQuantity",
                table: "Addons",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<bool>(
                name: "DeductOnSale",
                table: "Addons",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.AddForeignKey(
                name: "FK_Addons_Addons_BaseAddonId",
                table: "Addons",
                column: "BaseAddonId",
                principalTable: "Addons",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addons_Addons_BaseAddonId",
                table: "Addons");

            migrationBuilder.DropColumn(
                name: "DeductOnSale",
                table: "Addons");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "Addons",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "BaseAddonQuantity",
                table: "Addons",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Addons_Addons_BaseAddonId",
                table: "Addons",
                column: "BaseAddonId",
                principalTable: "Addons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
