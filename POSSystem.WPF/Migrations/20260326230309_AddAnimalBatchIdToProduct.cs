using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddAnimalBatchIdToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnimalBatchId",
                table: "Products",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 27, 2, 3, 8, 709, DateTimeKind.Local).AddTicks(9504), new DateTime(2026, 3, 27, 2, 3, 8, 709, DateTimeKind.Local).AddTicks(9505) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 27, 2, 3, 8, 709, DateTimeKind.Local).AddTicks(9531), new DateTime(2026, 3, 27, 2, 3, 8, 709, DateTimeKind.Local).AddTicks(9532) });

            migrationBuilder.CreateIndex(
                name: "IX_Products_AnimalBatchId",
                table: "Products",
                column: "AnimalBatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_AnimalBatches_AnimalBatchId",
                table: "Products",
                column: "AnimalBatchId",
                principalTable: "AnimalBatches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_AnimalBatches_AnimalBatchId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_AnimalBatchId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AnimalBatchId",
                table: "Products");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 27, 0, 50, 51, 584, DateTimeKind.Local).AddTicks(6759), new DateTime(2026, 3, 27, 0, 50, 51, 584, DateTimeKind.Local).AddTicks(6760) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 27, 0, 50, 51, 584, DateTimeKind.Local).AddTicks(6783), new DateTime(2026, 3, 27, 0, 50, 51, 584, DateTimeKind.Local).AddTicks(6783) });
        }
    }
}
