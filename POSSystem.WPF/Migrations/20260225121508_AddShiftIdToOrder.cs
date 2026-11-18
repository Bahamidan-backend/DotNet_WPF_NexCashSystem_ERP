using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftIdToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShiftId",
                table: "Orders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 25, 15, 15, 7, 527, DateTimeKind.Local).AddTicks(9203), new DateTime(2026, 2, 25, 15, 15, 7, 527, DateTimeKind.Local).AddTicks(9203) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 25, 15, 15, 7, 527, DateTimeKind.Local).AddTicks(9224), new DateTime(2026, 2, 25, 15, 15, 7, 527, DateTimeKind.Local).AddTicks(9224) });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ShiftId",
                table: "Orders",
                column: "ShiftId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Shifts_ShiftId",
                table: "Orders",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Shifts_ShiftId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ShiftId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "Orders");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 25, 0, 36, 0, 767, DateTimeKind.Local).AddTicks(2026), new DateTime(2026, 2, 25, 0, 36, 0, 767, DateTimeKind.Local).AddTicks(2027) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 25, 0, 36, 0, 767, DateTimeKind.Local).AddTicks(2046), new DateTime(2026, 2, 25, 0, 36, 0, 767, DateTimeKind.Local).AddTicks(2046) });
        }
    }
}
