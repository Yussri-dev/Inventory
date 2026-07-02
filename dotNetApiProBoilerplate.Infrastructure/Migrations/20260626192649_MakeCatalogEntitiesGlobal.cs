using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeCatalogEntitiesGlobal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PackComponents_Tenants_TenantId",
                table: "PackComponents");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductCatalogs_Tenants_TenantId",
                table: "ProductCatalogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategory_Tenants_TenantId",
                table: "ProductCategory");

            migrationBuilder.DropIndex(
                name: "IX_ProductCategory_TenantId",
                table: "ProductCategory");

            migrationBuilder.DropIndex(
                name: "IX_ProductCatalogs_TenantId",
                table: "ProductCatalogs");

            migrationBuilder.DropIndex(
                name: "IX_PackComponents_TenantId",
                table: "PackComponents");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ProductCategory");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ProductCatalogs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PackComponents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ProductCategory",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ProductCatalogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PackComponents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategory_TenantId",
                table: "ProductCategory",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCatalogs_TenantId",
                table: "ProductCatalogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PackComponents_TenantId",
                table: "PackComponents",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_PackComponents_Tenants_TenantId",
                table: "PackComponents",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCatalogs_Tenants_TenantId",
                table: "ProductCatalogs",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategory_Tenants_TenantId",
                table: "ProductCategory",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
