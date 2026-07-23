using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalProductCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductCatalogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Barcode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    InternalCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Brand = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Manufacturer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SellingMode = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsPack = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ServerCreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ServerModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCatalogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductCatalogs_Barcode",
                table: "ProductCatalogs",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCatalogs_CategoryId",
                table: "ProductCatalogs",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCatalogs_InternalCode",
                table: "ProductCatalogs",
                column: "InternalCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductCatalogs_IsDeleted",
                table: "ProductCatalogs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCatalogs_Name",
                table: "ProductCatalogs",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductCatalogs");
        }
    }
}
