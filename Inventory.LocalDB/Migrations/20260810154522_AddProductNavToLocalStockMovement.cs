using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class AddProductNavToLocalStockMovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ClientOperationId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId_LocalReferenceId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId_ProductLocalId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId_ProductServerId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId_ServerId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId_ServerReferenceId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId_SyncStatus",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId_Type",
                table: "StockMovements");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductLocalId",
                table: "StockMovements",
                column: "ProductLocalId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_ClientOperationId",
                table: "StockMovements",
                columns: new[] { "TenantId", "ClientOperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_LocalReferenceId_Type",
                table: "StockMovements",
                columns: new[] { "TenantId", "LocalReferenceId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_ProductLocalId_SyncStatus",
                table: "StockMovements",
                columns: new[] { "TenantId", "ProductLocalId", "SyncStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_ServerId",
                table: "StockMovements",
                columns: new[] { "TenantId", "ServerId" },
                unique: true,
                filter: "\"ServerId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Products_ProductLocalId",
                table: "StockMovements",
                column: "ProductLocalId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Products_ProductLocalId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ProductLocalId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId_ClientOperationId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId_LocalReferenceId_Type",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId_ProductLocalId_SyncStatus",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId_ServerId",
                table: "StockMovements");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ClientOperationId",
                table: "StockMovements",
                column: "ClientOperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId",
                table: "StockMovements",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_LocalReferenceId",
                table: "StockMovements",
                columns: new[] { "TenantId", "LocalReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_ProductLocalId",
                table: "StockMovements",
                columns: new[] { "TenantId", "ProductLocalId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_ProductServerId",
                table: "StockMovements",
                columns: new[] { "TenantId", "ProductServerId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_ServerId",
                table: "StockMovements",
                columns: new[] { "TenantId", "ServerId" },
                unique: true,
                filter: "ServerId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_ServerReferenceId",
                table: "StockMovements",
                columns: new[] { "TenantId", "ServerReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_SyncStatus",
                table: "StockMovements",
                columns: new[] { "TenantId", "SyncStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_Type",
                table: "StockMovements",
                columns: new[] { "TenantId", "Type" });
        }
    }
}
