using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class MakeAddonCategoryOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "Addons",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "Addons",
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
                values: new object[] { new DateTime(2026, 3, 13, 22, 54, 4, 57, DateTimeKind.Local).AddTicks(8779), new DateTime(2026, 3, 13, 22, 54, 4, 57, DateTimeKind.Local).AddTicks(8784) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 13, 22, 54, 4, 57, DateTimeKind.Local).AddTicks(8838), new DateTime(2026, 3, 13, 22, 54, 4, 57, DateTimeKind.Local).AddTicks(8839) });
        }
    }
}
