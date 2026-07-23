using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class addTenantToLocal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_ServerId",
                table: "Products");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "SyncQueueItems",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Products",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<DateTime>(
                name: "ServerModifiedAtUtc",
                table: "Products",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UnitProductLocalId",
                table: "Products",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncQueueItems_TenantId",
                table: "SyncQueueItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncQueueItems_TenantId_EntityName_LocalEntityId",
                table: "SyncQueueItems",
                columns: new[] { "TenantId", "EntityName", "LocalEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncQueueItems_TenantId_Status_CreatedAtUtc",
                table: "SyncQueueItems",
                columns: new[] { "TenantId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId",
                table: "Products",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_CatalogProductId",
                table: "Products",
                columns: new[] { "TenantId", "CatalogProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_IsDeletedLocally",
                table: "Products",
                columns: new[] { "TenantId", "IsDeletedLocally" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_ServerId",
                table: "Products",
                columns: new[] { "TenantId", "ServerId" },
                unique: true,
                filter: "ServerId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_UnitProductLocalId",
                table: "Products",
                column: "UnitProductLocalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncQueueItems_TenantId",
                table: "SyncQueueItems");

            migrationBuilder.DropIndex(
                name: "IX_SyncQueueItems_TenantId_EntityName_LocalEntityId",
                table: "SyncQueueItems");

            migrationBuilder.DropIndex(
                name: "IX_SyncQueueItems_TenantId_Status_CreatedAtUtc",
                table: "SyncQueueItems");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_CatalogProductId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_IsDeletedLocally",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_ServerId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_UnitProductLocalId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SyncQueueItems");

            migrationBuilder.DropColumn(
                name: "ServerModifiedAtUtc",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UnitProductLocalId",
                table: "Products");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ServerId",
                table: "Products",
                column: "ServerId",
                unique: true,
                filter: "ServerId IS NOT NULL");
        }
    }
}
