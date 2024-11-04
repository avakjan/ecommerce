using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineShoppingSite.Migrations
{
    /// <inheritdoc />
    public partial class AddImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Items",
                keyColumn: "ItemId",
                keyValue: 1,
                column: "ImageUrl",
                value: "https://pngimg.com/uploads/tshirt/tshirt_PNG5435.png");

            migrationBuilder.UpdateData(
                table: "Items",
                keyColumn: "ItemId",
                keyValue: 2,
                column: "ImageUrl",
                value: "https://static.vecteezy.com/system/resources/previews/034/969/304/large_2x/ai-generated-t-shirt-mockup-clip-art-free-png.png");

            migrationBuilder.UpdateData(
                table: "Items",
                keyColumn: "ItemId",
                keyValue: 3,
                column: "ImageUrl",
                value: "https://pics.clipartpng.com/Green_T_Shirt_PNG_Clip_Art-3106.png");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Items",
                keyColumn: "ItemId",
                keyValue: 1,
                column: "ImageUrl",
                value: "https://example.com/image1.jpg");

            migrationBuilder.UpdateData(
                table: "Items",
                keyColumn: "ItemId",
                keyValue: 2,
                column: "ImageUrl",
                value: "https://example.com/image2.jpg");

            migrationBuilder.UpdateData(
                table: "Items",
                keyColumn: "ItemId",
                keyValue: 3,
                column: "ImageUrl",
                value: "https://example.com/image3.jpg");
        }
    }
}
