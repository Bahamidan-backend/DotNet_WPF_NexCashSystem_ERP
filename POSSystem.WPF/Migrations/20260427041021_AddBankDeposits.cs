using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddBankDeposits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerDebts_Orders_OrderId",
                table: "CustomerDebts");

            migrationBuilder.CreateTable(
                name: "BankDeposits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DepositDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DepositorName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    AttachmentFileName = table.Column<string>(type: "TEXT", nullable: true),
                    AttachmentPath = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankDeposits", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 27, 7, 10, 19, 694, DateTimeKind.Local).AddTicks(5976), new DateTime(2026, 4, 27, 7, 10, 19, 694, DateTimeKind.Local).AddTicks(5978) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 27, 7, 10, 19, 694, DateTimeKind.Local).AddTicks(6041), new DateTime(2026, 4, 27, 7, 10, 19, 694, DateTimeKind.Local).AddTicks(6043) });

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerDebts_Orders_OrderId",
                table: "CustomerDebts",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerDebts_Orders_OrderId",
                table: "CustomerDebts");

            migrationBuilder.DropTable(
                name: "BankDeposits");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 23, 19, 39, 32, 562, DateTimeKind.Local).AddTicks(9736), new DateTime(2026, 4, 23, 19, 39, 32, 562, DateTimeKind.Local).AddTicks(9737) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 23, 19, 39, 32, 562, DateTimeKind.Local).AddTicks(9757), new DateTime(2026, 4, 23, 19, 39, 32, 562, DateTimeKind.Local).AddTicks(9758) });

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerDebts_Orders_OrderId",
                table: "CustomerDebts",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id");
        }
    }
}
