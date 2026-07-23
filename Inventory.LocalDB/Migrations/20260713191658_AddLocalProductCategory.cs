using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalProductCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LocalProductCategoryId",
                table: "ProductCatalogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: true),
                    Icon = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductCatalogs_LocalProductCategoryId",
                table: "ProductCatalogs",
                column: "LocalProductCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_Color",
                table: "ProductCategories",
                column: "Color");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_DisplayOrder",
                table: "ProductCategories",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_Icon",
                table: "ProductCategories",
                column: "Icon");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_Name",
                table: "ProductCategories",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCatalogs_ProductCategories_LocalProductCategoryId",
                table: "ProductCatalogs",
                column: "LocalProductCategoryId",
                principalTable: "ProductCategories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductCatalogs_ProductCategories_LocalProductCategoryId",
                table: "ProductCatalogs");

            migrationBuilder.DropTable(
                name: "ProductCategories");

            migrationBuilder.DropIndex(
                name: "IX_ProductCatalogs_LocalProductCategoryId",
                table: "ProductCatalogs");

            migrationBuilder.DropColumn(
                name: "LocalProductCategoryId",
                table: "ProductCatalogs");
        }
    }
}
