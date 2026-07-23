using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLocalReturn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CashSessionServerId",
                table: "Returns",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerLocalId",
                table: "Returns",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerServerId",
                table: "Returns",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocalCashSessionId",
                table: "Returns",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalLocalInvoiceNumber",
                table: "Returns",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalServerInvoiceNumber",
                table: "Returns",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPack",
                table: "ReturnLines",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LocalSaleLineId",
                table: "ReturnLines",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ServerSaleLineId",
                table: "ReturnLines",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCostPrice",
                table: "ReturnLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "UnitProductLocalId",
                table: "ReturnLines",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UnitProductServerId",
                table: "ReturnLines",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitQuantity",
                table: "ReturnLines",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitsPerPack",
                table: "ReturnLines",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Returns_TenantId_CustomerLocalId",
                table: "Returns",
                columns: new[] { "TenantId", "CustomerLocalId" });

            migrationBuilder.CreateIndex(
                name: "IX_Returns_TenantId_LocalCashSessionId",
                table: "Returns",
                columns: new[] { "TenantId", "LocalCashSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnLines_TenantId_LocalSaleLineId",
                table: "ReturnLines",
                columns: new[] { "TenantId", "LocalSaleLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnLines_TenantId_UnitProductLocalId",
                table: "ReturnLines",
                columns: new[] { "TenantId", "UnitProductLocalId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnLines_TenantId_UnitProductServerId",
                table: "ReturnLines",
                columns: new[] { "TenantId", "UnitProductServerId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Returns_TenantId_CustomerLocalId",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_Returns_TenantId_LocalCashSessionId",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_ReturnLines_TenantId_LocalSaleLineId",
                table: "ReturnLines");

            migrationBuilder.DropIndex(
                name: "IX_ReturnLines_TenantId_UnitProductLocalId",
                table: "ReturnLines");

            migrationBuilder.DropIndex(
                name: "IX_ReturnLines_TenantId_UnitProductServerId",
                table: "ReturnLines");

            migrationBuilder.DropColumn(
                name: "CashSessionServerId",
                table: "Returns");

            migrationBuilder.DropColumn(
                name: "CustomerLocalId",
                table: "Returns");

            migrationBuilder.DropColumn(
                name: "CustomerServerId",
                table: "Returns");

            migrationBuilder.DropColumn(
                name: "LocalCashSessionId",
                table: "Returns");

            migrationBuilder.DropColumn(
                name: "OriginalLocalInvoiceNumber",
                table: "Returns");

            migrationBuilder.DropColumn(
                name: "OriginalServerInvoiceNumber",
                table: "Returns");

            migrationBuilder.DropColumn(
                name: "IsPack",
                table: "ReturnLines");

            migrationBuilder.DropColumn(
                name: "LocalSaleLineId",
                table: "ReturnLines");

            migrationBuilder.DropColumn(
                name: "ServerSaleLineId",
                table: "ReturnLines");

            migrationBuilder.DropColumn(
                name: "UnitCostPrice",
                table: "ReturnLines");

            migrationBuilder.DropColumn(
                name: "UnitProductLocalId",
                table: "ReturnLines");

            migrationBuilder.DropColumn(
                name: "UnitProductServerId",
                table: "ReturnLines");

            migrationBuilder.DropColumn(
                name: "UnitQuantity",
                table: "ReturnLines");

            migrationBuilder.DropColumn(
                name: "UnitsPerPack",
                table: "ReturnLines");
        }
    }
}
