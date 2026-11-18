using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class MakeAddonOrderable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AddonOrderItem");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "OrderItems",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "AddonId",
                table: "OrderItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderItemId",
                table: "Addons",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 13, 22, 54, 4, 57, DateTimeKind.Local).AddTicks(8779), new DateTime(2026, 3, 13, 22, 54, 4, 57, DateTimeKind.Local).AddTicks(8784) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 13, 22, 54, 4, 57, DateTimeKind.Local).AddTicks(8838), new DateTime(2026, 3, 13, 22, 54, 4, 57, DateTimeKind.Local).AddTicks(8839) });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_AddonId",
                table: "OrderItems",
                column: "AddonId");

            migrationBuilder.CreateIndex(
                name: "IX_Addons_OrderItemId",
                table: "Addons",
                column: "OrderItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Addons_OrderItems_OrderItemId",
                table: "Addons",
                column: "OrderItemId",
                principalTable: "OrderItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Addons_AddonId",
                table: "OrderItems",
                column: "AddonId",
                principalTable: "Addons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addons_OrderItems_OrderItemId",
                table: "Addons");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Addons_AddonId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_AddonId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_Addons_OrderItemId",
                table: "Addons");

            migrationBuilder.DropColumn(
                name: "AddonId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "OrderItemId",
                table: "Addons");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "OrderItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "AddonOrderItem",
                columns: table => new
                {
                    AddonsId = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderItemsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddonOrderItem", x => new { x.AddonsId, x.OrderItemsId });
                    table.ForeignKey(
                        name: "FK_AddonOrderItem_Addons_AddonsId",
                        column: x => x.AddonsId,
                        principalTable: "Addons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AddonOrderItem_OrderItems_OrderItemsId",
                        column: x => x.OrderItemsId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 6, 4, 14, 11, 856, DateTimeKind.Local).AddTicks(136), new DateTime(2026, 3, 6, 4, 14, 11, 856, DateTimeKind.Local).AddTicks(136) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 6, 4, 14, 11, 856, DateTimeKind.Local).AddTicks(155), new DateTime(2026, 3, 6, 4, 14, 11, 856, DateTimeKind.Local).AddTicks(156) });

            migrationBuilder.CreateIndex(
                name: "IX_AddonOrderItem_OrderItemsId",
                table: "AddonOrderItem",
                column: "OrderItemsId");
        }
    }
}
