using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPackComponeent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPack",
                table: "ProductCatalogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PackComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackCatalaogId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComponentCatalogId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackComponents_ProductCatalogs_ComponentCatalogId",
                        column: x => x.ComponentCatalogId,
                        principalTable: "ProductCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackComponents_ProductCatalogs_PackCatalaogId",
                        column: x => x.PackCatalaogId,
                        principalTable: "ProductCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackComponents_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackComponents_ComponentCatalogId",
                table: "PackComponents",
                column: "ComponentCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_PackComponents_PackCatalaogId",
                table: "PackComponents",
                column: "PackCatalaogId");

            migrationBuilder.CreateIndex(
                name: "IX_PackComponents_TenantId",
                table: "PackComponents",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackComponents");

            migrationBuilder.DropColumn(
                name: "IsPack",
                table: "ProductCatalogs");
        }
    }
}
