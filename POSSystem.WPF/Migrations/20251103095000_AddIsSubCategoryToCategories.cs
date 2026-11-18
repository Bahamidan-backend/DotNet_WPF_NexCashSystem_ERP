using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    public partial class AddIsSubCategoryToCategories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSubCategory",
                table: "Categories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Update seed data timestamps if present (safe no-op if different)
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "UpdatedAt" },
                values: new object[] { DateTime.Now });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSubCategory",
                table: "Categories");
        }
    }
}
