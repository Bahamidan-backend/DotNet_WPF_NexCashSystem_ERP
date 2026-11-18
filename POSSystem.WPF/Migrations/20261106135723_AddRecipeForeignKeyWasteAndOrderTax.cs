using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeForeignKeyWasteAndOrderTax : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WarehouseFromProductId",
                table: "Recipes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "Orders",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "Orders",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0.15m);

            migrationBuilder.CreateTable(
                name: "StockWastes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WarehouseFromProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    WarehouseId = table.Column<int>(type: "INTEGER", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PortionUnit = table.Column<int>(type: "INTEGER", nullable: true),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Reason = table.Column<int>(type: "INTEGER", nullable: false),
                    WasteDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReportedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockWastes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockWastes_Users_ReportedByUserId",
                        column: x => x.ReportedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockWastes_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockWastes_warehouseFromProducts_WarehouseFromProductId",
                        column: x => x.WarehouseFromProductId,
                        principalTable: "warehouseFromProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "PBKDF2$100000$45Wy2GocbnjfExYfKmrz2A==$X9zusTypmJzjsMQ6/40AS+0JIZrXD0bW93sUoVZJiWI=");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "Password",
                value: "PBKDF2$100000$1Ex9jKL12zDcBDtLAT7wkg==$/o6+OyvyGrtwWuJ5iauPHIlMeGKQIwsduh/hN3czupw=");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_WarehouseFromProductId",
                table: "Recipes",
                column: "WarehouseFromProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockWastes_ReportedByUserId",
                table: "StockWastes",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockWastes_WarehouseFromProductId",
                table: "StockWastes",
                column: "WarehouseFromProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockWastes_WarehouseId",
                table: "StockWastes",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockWastes_WasteDate",
                table: "StockWastes",
                column: "WasteDate");

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_warehouseFromProducts_WarehouseFromProductId",
                table: "Recipes",
                column: "WarehouseFromProductId",
                principalTable: "warehouseFromProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_warehouseFromProducts_WarehouseFromProductId",
                table: "Recipes");

            migrationBuilder.DropTable(
                name: "StockWastes");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_WarehouseFromProductId",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "WarehouseFromProductId",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "Orders");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "PBKDF2$100000$zFmLyi2zdTTwPhhpiKTavw==$F21D5vnJlg37zIZtlM94a4515TwJIjpIWvs3nbbn2SE=");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "Password",
                value: "PBKDF2$100000$3lmxXw3yxfA85Uf7pdKRWg==$Ldd50V+w++dWMQkxK/SX6H5URHo0gNcEfqwROb3Usiw=");
        }
    }
}
