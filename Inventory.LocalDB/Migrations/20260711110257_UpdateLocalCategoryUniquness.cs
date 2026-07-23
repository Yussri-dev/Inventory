using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLocalCategoryUniquness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductCatalogs_InternalCode",
                table: "ProductCatalogs");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCatalogs_InternalCode",
                table: "ProductCatalogs",
                column: "InternalCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductCatalogs_InternalCode",
                table: "ProductCatalogs");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCatalogs_InternalCode",
                table: "ProductCatalogs",
                column: "InternalCode",
                unique: true);
        }
    }
}
