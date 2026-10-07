using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeProductCatalogOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Barcode",
                table: "Products");

            migrationBuilder.AlterColumn<Guid>(
                name: "CatalogProductId",
                table: "Products",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_Barcode",
                table: "Products",
                columns: new[] { "TenantId", "Barcode" },
                unique: true,
                filter: "\"CatalogProductId\" IS NULL AND \"Barcode\" IS NOT NULL AND BTRIM(\"Barcode\") <> '' AND \"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_Barcode",
                table: "Products");

            migrationBuilder.AlterColumn<Guid>(
                name: "CatalogProductId",
                table: "Products",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode");
        }
    }
}
