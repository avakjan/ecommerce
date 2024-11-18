using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineShoppingSite.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Items",
                keyColumn: "ItemId",
                keyValue: 2,
                column: "ImageUrl",
                value: "https://www.racerworldwide.net/cdn/shop/files/front_white_1_31a53b32-c70b-48ef-8612-d869fc6d5877_750x.jpg?v=1723733410");

            migrationBuilder.UpdateData(
                table: "Items",
                keyColumn: "ItemId",
                keyValue: 3,
                column: "ImageUrl",
                value: "https://www.racerworldwide.net/cdn/shop/files/FW24_Glitch_Beanie_Camo_LB_FF_1_750x.jpg?v=1726757323");

            migrationBuilder.UpdateData(
                table: "Items",
                keyColumn: "ItemId",
                keyValue: 4,
                column: "ImageUrl",
                value: "https://www.racerworldwide.net/cdn/shop/files/SuedeRightSideFFFFFF_1_750x.jpg?v=1723730360");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Items",
                keyColumn: "ItemId",
                keyValue: 2,
                column: "ImageUrl",
                value: "https://www.racerworldwide.net/cdn/shop/files/TrackHoodie_750x.jpg?v=1723727728");

            migrationBuilder.UpdateData(
                table: "Items",
                keyColumn: "ItemId",
                keyValue: 3,
                column: "ImageUrl",
                value: "https://www.racerworldwide.net/cdn/shop/files/GlitchLeoBeanie_750x.jpg?v=1723727728");

            migrationBuilder.UpdateData(
                table: "Items",
                keyColumn: "ItemId",
                keyValue: 4,
                column: "ImageUrl",
                value: "https://www.racerworldwide.net/cdn/shop/files/VibramDesertBoots_750x.jpg?v=1723727728");
        }
    }
}
