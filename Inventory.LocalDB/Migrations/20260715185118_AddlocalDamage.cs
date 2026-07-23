using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class AddlocalDamage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stocks_TenantId",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_TenantId_ProductBarcode",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_TenantId_ProductServerId",
                table: "Stocks");

            migrationBuilder.CreateTable(
                name: "Damages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DamageNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ProductLocalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    EstimatedValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DamageDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValidatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LocalStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ServerStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    SyncStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Damages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Damages_Products_ProductLocalId",
                        column: x => x.ProductLocalId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_TenantId_ProductServerId",
                table: "Stocks",
                columns: new[] { "TenantId", "ProductServerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Damages_ProductLocalId",
                table: "Damages",
                column: "ProductLocalId");

            migrationBuilder.CreateIndex(
                name: "IX_Damages_TenantId_DamageNumber",
                table: "Damages",
                columns: new[] { "TenantId", "DamageNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Damages_TenantId_LocalStatus",
                table: "Damages",
                columns: new[] { "TenantId", "LocalStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Damages_TenantId_ProductLocalId",
                table: "Damages",
                columns: new[] { "TenantId", "ProductLocalId" });

            migrationBuilder.CreateIndex(
                name: "IX_Damages_TenantId_ServerId",
                table: "Damages",
                columns: new[] { "TenantId", "ServerId" },
                unique: true,
                filter: "ServerId IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Damages");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_TenantId_ProductServerId",
                table: "Stocks");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_TenantId",
                table: "Stocks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_TenantId_ProductBarcode",
                table: "Stocks",
                columns: new[] { "TenantId", "ProductBarcode" });

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_TenantId_ProductServerId",
                table: "Stocks",
                columns: new[] { "TenantId", "ProductServerId" },
                unique: true,
                filter: "ProductServerId IS NOT NULL");
        }
    }
}
