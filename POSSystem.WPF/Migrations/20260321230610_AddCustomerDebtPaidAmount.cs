using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerDebtPaidAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "ReceiptVouchers");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "CustomerDebts");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 22, 0, 2, 57, 319, DateTimeKind.Local).AddTicks(4717), new DateTime(2026, 3, 22, 0, 2, 57, 319, DateTimeKind.Local).AddTicks(4719) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 22, 0, 2, 57, 319, DateTimeKind.Local).AddTicks(4782), new DateTime(2026, 3, 22, 0, 2, 57, 319, DateTimeKind.Local).AddTicks(4784) });
        }
    }
}
