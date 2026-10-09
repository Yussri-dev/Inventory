using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogApprovalStatusToLocal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CatalogApprovalStatus",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CatalogApprovalStatus",
                table: "Products");
        }
    }
}
