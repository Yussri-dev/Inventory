using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultPricesToProductCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DefaultPurchasePrice",
                table: "ProductCatalogs",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultSalePrice",
                table: "ProductCatalogs",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultSalePrice2",
                table: "ProductCatalogs",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultSalePrice3",
                table: "ProductCatalogs",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultVatRate",
                table: "ProductCatalogs",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultPurchasePrice",
                table: "ProductCatalogs");

            migrationBuilder.DropColumn(
                name: "DefaultSalePrice",
                table: "ProductCatalogs");

            migrationBuilder.DropColumn(
                name: "DefaultSalePrice2",
                table: "ProductCatalogs");

            migrationBuilder.DropColumn(
                name: "DefaultSalePrice3",
                table: "ProductCatalogs");

            migrationBuilder.DropColumn(
                name: "DefaultVatRate",
                table: "ProductCatalogs");
        }
    }
}
