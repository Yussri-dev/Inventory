using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalInventorySessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventorySessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClientOperationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SessionNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ValidatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SyncStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventorySessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocalInventorySessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductLocalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    ProductBarcode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ExpectedQuantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    CountedQuantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    IsAdjusted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryLines_InventorySessions_LocalInventorySessionId",
                        column: x => x.LocalInventorySessionId,
                        principalTable: "InventorySessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLines_LocalInventorySessionId_ProductLocalId",
                table: "InventoryLines",
                columns: new[] { "LocalInventorySessionId", "ProductLocalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLines_TenantId_ProductLocalId",
                table: "InventoryLines",
                columns: new[] { "TenantId", "ProductLocalId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventorySessions_TenantId_SessionNumber",
                table: "InventorySessions",
                columns: new[] { "TenantId", "SessionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventorySessions_TenantId_Status",
                table: "InventorySessions",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryLines");

            migrationBuilder.DropTable(
                name: "InventorySessions");
        }
    }
}
