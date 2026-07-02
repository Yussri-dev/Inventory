using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueProductCatalogPerTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGlobal",
                table: "ProductCatalogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_CatalogProductId",
                table: "Products",
                columns: new[] { "TenantId", "CatalogProductId" },
                unique: true,
                filter: "\"CatalogProductId\" IS NOT NULL AND \"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_CatalogProductId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsGlobal",
                table: "ProductCatalogs");
        }
    }
}
