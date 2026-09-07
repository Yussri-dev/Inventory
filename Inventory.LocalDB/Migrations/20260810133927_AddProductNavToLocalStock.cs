using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class AddProductNavToLocalStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stocks_TenantId_ProductServerId",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_TenantId_ServerId",
                table: "Stocks");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ProductLocalId",
                table: "Stocks",
                column: "ProductLocalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_TenantId_ProductServerId",
                table: "Stocks",
                columns: new[] { "TenantId", "ProductServerId" },
                unique: true,
                filter: "\"ProductServerId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_TenantId_ServerId",
                table: "Stocks",
                columns: new[] { "TenantId", "ServerId" },
                unique: true,
                filter: "\"ServerId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Stocks_Products_ProductLocalId",
                table: "Stocks",
                column: "ProductLocalId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stocks_Products_ProductLocalId",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_ProductLocalId",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_TenantId_ProductServerId",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_TenantId_ServerId",
                table: "Stocks");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_TenantId_ProductServerId",
                table: "Stocks",
                columns: new[] { "TenantId", "ProductServerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_TenantId_ServerId",
                table: "Stocks",
                columns: new[] { "TenantId", "ServerId" },
                unique: true,
                filter: "ServerId IS NOT NULL");
        }
    }
}
