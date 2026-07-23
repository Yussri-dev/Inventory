using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLocalProductCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductCatalogs_ProductCategories_LocalProductCategoryId",
                table: "ProductCatalogs");

            migrationBuilder.DropIndex(
                name: "IX_ProductCategories_Color",
                table: "ProductCategories");

            migrationBuilder.DropIndex(
                name: "IX_ProductCategories_Icon",
                table: "ProductCategories");

            migrationBuilder.DropIndex(
                name: "IX_ProductCatalogs_LocalProductCategoryId",
                table: "ProductCatalogs");

            migrationBuilder.DropColumn(
                name: "LocalProductCategoryId",
                table: "ProductCatalogs");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ProductCategories",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProductCategories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAtUtc",
                table: "ProductCategories",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_IsDeleted",
                table: "ProductCategories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_LastSyncedAtUtc",
                table: "ProductCategories",
                column: "LastSyncedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductCategories_IsDeleted",
                table: "ProductCategories");

            migrationBuilder.DropIndex(
                name: "IX_ProductCategories_LastSyncedAtUtc",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "LastSyncedAtUtc",
                table: "ProductCategories");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ProductCategories",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100,
                oldCollation: "NOCASE");

            migrationBuilder.AddColumn<Guid>(
                name: "LocalProductCategoryId",
                table: "ProductCatalogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_Color",
                table: "ProductCategories",
                column: "Color");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_Icon",
                table: "ProductCategories",
                column: "Icon");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCatalogs_LocalProductCategoryId",
                table: "ProductCatalogs",
                column: "LocalProductCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCatalogs_ProductCategories_LocalProductCategoryId",
                table: "ProductCatalogs",
                column: "LocalProductCategoryId",
                principalTable: "ProductCategories",
                principalColumn: "Id");
        }
    }
}
