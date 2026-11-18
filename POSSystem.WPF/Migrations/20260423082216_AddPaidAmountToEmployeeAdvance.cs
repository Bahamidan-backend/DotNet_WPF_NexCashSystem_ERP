using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddPaidAmountToEmployeeAdvance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBatchManaged",
                table: "Products");

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "EmployeeAdvances",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 23, 11, 22, 15, 966, DateTimeKind.Local).AddTicks(4538), new DateTime(2026, 4, 23, 11, 22, 15, 966, DateTimeKind.Local).AddTicks(4539) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 23, 11, 22, 15, 966, DateTimeKind.Local).AddTicks(4561), new DateTime(2026, 4, 23, 11, 22, 15, 966, DateTimeKind.Local).AddTicks(4561) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "EmployeeAdvances");

            migrationBuilder.AddColumn<bool>(
                name: "IsBatchManaged",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 17, 22, 33, 26, 997, DateTimeKind.Local).AddTicks(8149), new DateTime(2026, 4, 17, 22, 33, 26, 997, DateTimeKind.Local).AddTicks(8150) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 17, 22, 33, 26, 997, DateTimeKind.Local).AddTicks(8170), new DateTime(2026, 4, 17, 22, 33, 26, 997, DateTimeKind.Local).AddTicks(8170) });
        }
    }
}
