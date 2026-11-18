using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToSupplier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Suppliers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "PBKDF2$100000$54tbZfqTF8VARSv9ATnoOQ==$zpcRoOx/XdH+OCxyXf/naqLTXsAaJl/x3hH17y17f+s=");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "Password",
                value: "PBKDF2$100000$W7NTW3TxGitn1KYN6zoUtw==$u/yuTZw6sLHGT08OSZkmm8s1ZKW/MwBueleXfXV8t/8=");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Suppliers");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "PBKDF2$100000$c7r2gB5yxHQfTpPkMtIDZw==$0NKjgUxOigFZPKibIBnfPDW15DFkobZtzmy4WPnxtRM=");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "Password",
                value: "PBKDF2$100000$91TtXdJI7bmpROBEueVE7Q==$qwnhOw1VJc6Q+grfA3XJPuO372yb2TlV0MI6wtfF2jQ=");
        }
    }
}
