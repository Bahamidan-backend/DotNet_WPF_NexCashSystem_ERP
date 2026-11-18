using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddProductAnimalComponent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Products",
                newName: "IsAnimalComponent");

            migrationBuilder.AddColumn<int>(
                name: "AnimalComponentType",
                table: "Products",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 26, 23, 50, 53, 866, DateTimeKind.Local).AddTicks(8735), new DateTime(2026, 3, 26, 23, 50, 53, 866, DateTimeKind.Local).AddTicks(8737) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 26, 23, 50, 53, 866, DateTimeKind.Local).AddTicks(8803), new DateTime(2026, 3, 26, 23, 50, 53, 866, DateTimeKind.Local).AddTicks(8805) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnimalComponentType",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "IsAnimalComponent",
                table: "Products",
                newName: "Type");

            migrationBuilder.AddColumn<int>(
                name: "IngredientId",
                table: "Recipes",
                type: "INTEGER",
                nullable: true);

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
    }
}
