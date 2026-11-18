using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityFieldsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntil",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "FailedLoginAttempts", "LockedUntil", "MustChangePassword", "Password", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, null, true, "PBKDF2$100000$2kDa5F5kgNcyEnHxHjWcjg==$TnQRr5/YPzg9UeVF904Pu29WWz3/k6+9qh29gLL8zZ8=", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "FailedLoginAttempts", "LockedUntil", "MustChangePassword", "Password", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, null, true, "PBKDF2$100000$gkv31wT8QuCjcyswVJeFbw==$LxxRgBkGu0NQrpg9Pn0syd4yHdu/aa3ppyEgOaov+ig=", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "Password", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 8, 16, 22, 43, 583, DateTimeKind.Local).AddTicks(4835), "JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=", new DateTime(2026, 6, 8, 16, 22, 43, 583, DateTimeKind.Local).AddTicks(4837) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "Password", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 8, 16, 22, 43, 583, DateTimeKind.Local).AddTicks(4897), "5gbjiw2MGbJM8O44CBgxYup81j/3kS27IrXoAyhrREY=", new DateTime(2026, 6, 8, 16, 22, 43, 583, DateTimeKind.Local).AddTicks(4898) });
        }
    }
}
