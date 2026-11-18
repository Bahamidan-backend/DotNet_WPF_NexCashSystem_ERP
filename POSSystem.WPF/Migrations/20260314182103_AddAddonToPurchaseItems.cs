using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddAddonToPurchaseItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "PurchaseItems",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "AddonId",
                table: "PurchaseItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 14, 21, 21, 2, 995, DateTimeKind.Local).AddTicks(8228), new DateTime(2026, 3, 14, 21, 21, 2, 995, DateTimeKind.Local).AddTicks(8228) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 14, 21, 21, 2, 995, DateTimeKind.Local).AddTicks(8250), new DateTime(2026, 3, 14, 21, 21, 2, 995, DateTimeKind.Local).AddTicks(8250) });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItems_AddonId",
                table: "PurchaseItems",
                column: "AddonId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseItems_Addons_AddonId",
                table: "PurchaseItems",
                column: "AddonId",
                principalTable: "Addons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseItems_Addons_AddonId",
                table: "PurchaseItems");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseItems_AddonId",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "AddonId",
                table: "PurchaseItems");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "PurchaseItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 13, 23, 29, 14, 580, DateTimeKind.Local).AddTicks(9103), new DateTime(2026, 3, 13, 23, 29, 14, 580, DateTimeKind.Local).AddTicks(9104) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 13, 23, 29, 14, 580, DateTimeKind.Local).AddTicks(9169), new DateTime(2026, 3, 13, 23, 29, 14, 580, DateTimeKind.Local).AddTicks(9169) });
        }
    }
}
