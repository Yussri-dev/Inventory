using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalPackComponents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PackComponents",
                columns: table => new
                {
                    ProductCatalogId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ComponentCatalogId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ComponentName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackComponents", x => new { x.ProductCatalogId, x.ComponentCatalogId });
                    table.ForeignKey(
                        name: "FK_PackComponents_ProductCatalogs_ProductCatalogId",
                        column: x => x.ProductCatalogId,
                        principalTable: "ProductCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackComponents_ComponentCatalogId",
                table: "PackComponents",
                column: "ComponentCatalogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackComponents");
        }
    }
}
