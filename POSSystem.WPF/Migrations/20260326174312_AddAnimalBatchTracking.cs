using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddAnimalBatchTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "ReceiptVouchers");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "CustomerDebts");

            migrationBuilder.CreateTable(
                name: "AnimalBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemainingWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HeadCount = table.Column<int>(type: "INTEGER", nullable: false),
                    BackCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LegsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    BatchDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WarehouseId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnimalBatches_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BatchComponents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AnimalBatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    ComponentType = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OriginalQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatchComponents_AnimalBatches_AnimalBatchId",
                        column: x => x.AnimalBatchId,
                        principalTable: "AnimalBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 26, 20, 43, 11, 626, DateTimeKind.Local).AddTicks(9906), new DateTime(2026, 3, 26, 20, 43, 11, 626, DateTimeKind.Local).AddTicks(9907) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 26, 20, 43, 11, 626, DateTimeKind.Local).AddTicks(9941), new DateTime(2026, 3, 26, 20, 43, 11, 626, DateTimeKind.Local).AddTicks(9941) });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalBatches_WarehouseId",
                table: "AnimalBatches",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchComponents_AnimalBatchId",
                table: "BatchComponents",
                column: "AnimalBatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BatchComponents");

            migrationBuilder.DropTable(
                name: "AnimalBatches");

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "ReceiptVouchers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "CustomerDebts",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 22, 2, 6, 9, 980, DateTimeKind.Local).AddTicks(2037), new DateTime(2026, 3, 22, 2, 6, 9, 980, DateTimeKind.Local).AddTicks(2039) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 22, 2, 6, 9, 980, DateTimeKind.Local).AddTicks(2062), new DateTime(2026, 3, 22, 2, 6, 9, 980, DateTimeKind.Local).AddTicks(2063) });
        }
    }
}
