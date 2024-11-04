using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineShoppingSite.Migrations
{
    /// <inheritdoc />
    public partial class AddChargeIdToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChargeId",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChargeId",
                table: "Orders");
        }
    }
}
