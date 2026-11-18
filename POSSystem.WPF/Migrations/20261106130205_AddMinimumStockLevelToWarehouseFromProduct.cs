using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POSSystem.WPF.Migrations
{
    /// <inheritdoc />
    public partial class AddMinimumStockLevelToWarehouseFromProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MinimumStockLevel",
                table: "warehouseFromProducts",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 5m);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "PBKDF2$100000$zFmLyi2zdTTwPhhpiKTavw==$F21D5vnJlg37zIZtlM94a4515TwJIjpIWvs3nbbn2SE=");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "Password",
                value: "PBKDF2$100000$3lmxXw3yxfA85Uf7pdKRWg==$Ldd50V+w++dWMQkxK/SX6H5URHo0gNcEfqwROb3Usiw=");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinimumStockLevel",
                table: "warehouseFromProducts");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "PBKDF2$100000$e3SRx+dk4Q53b7CpsYNFSQ==$y1bgH+Dw4JYbY03C2r0Kz5GHTFO9cTD+dF2IWgyviGY=");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "Password",
                value: "PBKDF2$100000$NTBUC84r4uE/5wKW83TTLw==$k8x/kXANmtZZahStIFC2bgKV61m1cpPL+CY2jl0vMF8=");
        }
    }
}
