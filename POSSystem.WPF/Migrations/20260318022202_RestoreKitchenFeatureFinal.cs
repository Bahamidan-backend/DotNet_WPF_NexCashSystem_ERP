using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class RestoreKitchenFeatureFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KitchenId",
                table: "Products",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KitchenId",
                table: "Categories",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Kitchens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PrinterName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsPrinterEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kitchens", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 18, 5, 22, 1, 210, DateTimeKind.Local).AddTicks(306), new DateTime(2026, 3, 18, 5, 22, 1, 210, DateTimeKind.Local).AddTicks(307) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 18, 5, 22, 1, 210, DateTimeKind.Local).AddTicks(360), new DateTime(2026, 3, 18, 5, 22, 1, 210, DateTimeKind.Local).AddTicks(361) });

            migrationBuilder.CreateIndex(
                name: "IX_Products_KitchenId",
                table: "Products",
                column: "KitchenId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_KitchenId",
                table: "Categories",
                column: "KitchenId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Kitchens_KitchenId",
                table: "Categories",
                column: "KitchenId",
                principalTable: "Kitchens",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Kitchens_KitchenId",
                table: "Products",
                column: "KitchenId",
                principalTable: "Kitchens",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Kitchens_KitchenId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Kitchens_KitchenId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Kitchens");

            migrationBuilder.DropIndex(
                name: "IX_Products_KitchenId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Categories_KitchenId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "KitchenId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "KitchenId",
                table: "Categories");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 15, 14, 14, 9, 209, DateTimeKind.Local).AddTicks(2989), new DateTime(2026, 3, 15, 14, 14, 9, 209, DateTimeKind.Local).AddTicks(2990) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 15, 14, 14, 9, 209, DateTimeKind.Local).AddTicks(3009), new DateTime(2026, 3, 15, 14, 14, 9, 209, DateTimeKind.Local).AddTicks(3010) });
        }
    }
}
