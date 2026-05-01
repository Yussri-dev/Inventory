using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addCategoryModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductCategory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Color = table.Column<string>(type: "text", nullable: true),
                    Icon = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategory", x => x.Id);
                });

            var defaultCategoryId = Guid.NewGuid();

            migrationBuilder.InsertData(
                table: "ProductCategory",
                columns: new[] { "Id", "Name", "DisplayOrder" },
                values: new object[] { defaultCategoryId, "General", 0 }
            );

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "ProductCatalogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql($@"
                UPDATE ""ProductCatalogs""
                SET ""CategoryId"" = '{defaultCategoryId}'
            ");

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "ProductCatalogs",
                type: "uuid",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_ProductCatalogs_CategoryId",
                table: "ProductCatalogs",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCatalogs_ProductCategory_CategoryId",
                table: "ProductCatalogs",
                column: "CategoryId",
                principalTable: "ProductCategory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductCatalogs_ProductCategory_CategoryId",
                table: "ProductCatalogs");

            migrationBuilder.DropTable(
                name: "ProductCategory");

            migrationBuilder.DropIndex(
                name: "IX_ProductCatalogs_Barcode",
                table: "ProductCatalogs");

            migrationBuilder.DropIndex(
                name: "IX_ProductCatalogs_CategoryId",
                table: "ProductCatalogs");

            migrationBuilder.DropIndex(
                name: "IX_ProductCatalogs_InternalCode",
                table: "ProductCatalogs");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "ProductCatalogs");
        }
    }
}
