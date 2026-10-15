using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class AddSellingModeToLocalProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SellingMode",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UnitOfMeasure",
                table: "Products",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "pcs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SellingMode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UnitOfMeasure",
                table: "Products");
        }
    }
}
