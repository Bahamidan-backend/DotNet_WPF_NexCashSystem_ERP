using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class EnableAddonCascadeDeleteFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_addonAndorderItem_Addons_AddonID",
                table: "addonAndorderItem");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseItems_Addons_AddonId",
                table: "PurchaseItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnOrderItems_OrderItems_OriginalOrderItemId",
                table: "ReturnOrderItems");

            migrationBuilder.RenameColumn(
                name: "AddonID",
                table: "addonAndorderItem",
                newName: "AddonId");

            migrationBuilder.RenameIndex(
                name: "IX_addonAndorderItem_AddonID",
                table: "addonAndorderItem",
                newName: "IX_addonAndorderItem_AddonId");

            migrationBuilder.AddColumn<int>(
                name: "AddonId1",
                table: "addonAndorderItem",
                type: "INTEGER",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_addonAndorderItem_AddonId1",
                table: "addonAndorderItem",
                column: "AddonId1");

            migrationBuilder.AddForeignKey(
                name: "FK_addonAndorderItem_Addons_AddonId",
                table: "addonAndorderItem",
                column: "AddonId",
                principalTable: "Addons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_addonAndorderItem_Addons_AddonId1",
                table: "addonAndorderItem",
                column: "AddonId1",
                principalTable: "Addons",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseItems_Addons_AddonId",
                table: "PurchaseItems",
                column: "AddonId",
                principalTable: "Addons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnOrderItems_OrderItems_OriginalOrderItemId",
                table: "ReturnOrderItems",
                column: "OriginalOrderItemId",
                principalTable: "OrderItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_addonAndorderItem_Addons_AddonId",
                table: "addonAndorderItem");

            migrationBuilder.DropForeignKey(
                name: "FK_addonAndorderItem_Addons_AddonId1",
                table: "addonAndorderItem");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseItems_Addons_AddonId",
                table: "PurchaseItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnOrderItems_OrderItems_OriginalOrderItemId",
                table: "ReturnOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_addonAndorderItem_AddonId1",
                table: "addonAndorderItem");

            migrationBuilder.DropColumn(
                name: "AddonId1",
                table: "addonAndorderItem");

            migrationBuilder.RenameColumn(
                name: "AddonId",
                table: "addonAndorderItem",
                newName: "AddonID");

            migrationBuilder.RenameIndex(
                name: "IX_addonAndorderItem_AddonId",
                table: "addonAndorderItem",
                newName: "IX_addonAndorderItem_AddonID");

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

            migrationBuilder.AddForeignKey(
                name: "FK_addonAndorderItem_Addons_AddonID",
                table: "addonAndorderItem",
                column: "AddonID",
                principalTable: "Addons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseItems_Addons_AddonId",
                table: "PurchaseItems",
                column: "AddonId",
                principalTable: "Addons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnOrderItems_OrderItems_OriginalOrderItemId",
                table: "ReturnOrderItems",
                column: "OriginalOrderItemId",
                principalTable: "OrderItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
