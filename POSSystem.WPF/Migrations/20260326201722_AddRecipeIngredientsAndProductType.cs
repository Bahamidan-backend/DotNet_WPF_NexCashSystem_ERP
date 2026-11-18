using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeIngredientsAndProductType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IngredientId",
                table: "Recipes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 26, 23, 17, 19, 979, DateTimeKind.Local).AddTicks(7629), new DateTime(2026, 3, 26, 23, 17, 19, 979, DateTimeKind.Local).AddTicks(7631) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 26, 23, 17, 19, 979, DateTimeKind.Local).AddTicks(7695), new DateTime(2026, 3, 26, 23, 17, 19, 979, DateTimeKind.Local).AddTicks(7697) });

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_IngredientId",
                table: "Recipes",
                column: "IngredientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_Products_IngredientId",
                table: "Recipes",
                column: "IngredientId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_Products_IngredientId",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_IngredientId",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "IngredientId",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Products");

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
        }
    }
}
