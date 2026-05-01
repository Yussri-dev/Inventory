using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SaleId",
                table: "CashMovements",
                newName: "ReferenceId");

            migrationBuilder.AddColumn<Guid>(
                name: "UnitProductId",
                table: "SaleLines",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitQuantity",
                table: "SaleLines",
                type: "numeric(18,3)",
                nullable: false,
                defaultValue: 0m);

            // FIX DATA AVANT FK
            migrationBuilder.Sql(@"
        UPDATE ""SaleLines""
        SET ""UnitProductId"" = ""ProductId""
        WHERE ""UnitProductId"" IS NULL
    ");

            // ENSUITE rendre NOT NULL
            migrationBuilder.AlterColumn<Guid>(
                name: "UnitProductId",
                table: "SaleLines",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BarcodeType",
                table: "ProductCatalogs",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "ProductCatalogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "InternalCode",
                table: "ProductCatalogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReferenceType",
                table: "CashMovements",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleLines_UnitProductId",
                table: "SaleLines",
                column: "UnitProductId");

            // FIX: pas Cascade
            migrationBuilder.AddForeignKey(
                name: "FK_SaleLines_Products_UnitProductId",
                table: "SaleLines",
                column: "UnitProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SaleLines_Products_UnitProductId",
                table: "SaleLines");

            migrationBuilder.DropIndex(
                name: "IX_SaleLines_UnitProductId",
                table: "SaleLines");

            migrationBuilder.DropColumn(
                name: "UnitProductId",
                table: "SaleLines");

            migrationBuilder.DropColumn(
                name: "UnitQuantity",
                table: "SaleLines");

            migrationBuilder.DropColumn(
                name: "InternalCode",
                table: "ProductCatalogs");

            migrationBuilder.DropColumn(
                name: "ReferenceType",
                table: "CashMovements");

            migrationBuilder.RenameColumn(
                name: "ReferenceId",
                table: "CashMovements",
                newName: "SaleId");

            migrationBuilder.AlterColumn<int>(
                name: "BarcodeType",
                table: "ProductCatalogs",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "ProductCatalogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
