using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalPurchaseDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PurchaseDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SupplierLocalId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseDrafts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseDraftLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PurchaseDraftId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductLocalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    BasePurchasePrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    EffectiveUnitPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseDraftLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseDraftLines_PurchaseDrafts_PurchaseDraftId",
                        column: x => x.PurchaseDraftId,
                        principalTable: "PurchaseDrafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseDraftAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PurchaseDraftLineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseDraftAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseDraftAdjustments_PurchaseDraftLines_PurchaseDraftLineId",
                        column: x => x.PurchaseDraftLineId,
                        principalTable: "PurchaseDraftLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseDraftAdjustments_PurchaseDraftLineId_DisplayOrder",
                table: "PurchaseDraftAdjustments",
                columns: new[] { "PurchaseDraftLineId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseDraftLines_PurchaseDraftId_DisplayOrder",
                table: "PurchaseDraftLines",
                columns: new[] { "PurchaseDraftId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseDrafts_TenantId_Status",
                table: "PurchaseDrafts",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseDraftAdjustments");

            migrationBuilder.DropTable(
                name: "PurchaseDraftLines");

            migrationBuilder.DropTable(
                name: "PurchaseDrafts");
        }
    }
}
