using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class applyTenantIdToProductCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ProductCategory",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "ProductCategory",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ProductCategory",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "ProductCategory",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProductCategory",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "ProductCategory",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByUserId",
                table: "ProductCategory",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ProductCategory",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(@"
        UPDATE ""ProductCategory""
        SET ""TenantId"" = (
            SELECT ""Id"" FROM ""Tenants"" LIMIT 1
        )
    ");

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "ProductCategory",
                type: "uuid",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategory_TenantId",
                table: "ProductCategory",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategory_Tenants_TenantId",
                table: "ProductCategory",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategory_Tenants_TenantId",
                table: "ProductCategory");

            migrationBuilder.DropIndex(
                name: "IX_ProductCategory_TenantId",
                table: "ProductCategory");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ProductCategory");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ProductCategory");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ProductCategory");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ProductCategory");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProductCategory");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "ProductCategory");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "ProductCategory");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ProductCategory");
        }
    }
}
