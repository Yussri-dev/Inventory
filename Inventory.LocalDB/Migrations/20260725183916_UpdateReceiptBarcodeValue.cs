using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class UpdateReceiptBarcodeValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sales_TenantId_ReceiptBarcodeValue",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_TenantId_ServerId",
                table: "Sales");

            migrationBuilder.AlterColumn<string>(
                name: "ReceiptBarcodeValue",
                table: "Sales",
                type: "TEXT",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_ReceiptBarcodeValue",
                table: "Sales",
                columns: new[] { "TenantId", "ReceiptBarcodeValue" },
                unique: true,
                filter: "\"ReceiptBarcodeValue\" IS NOT NULL AND trim(\"ReceiptBarcodeValue\") <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_ServerId",
                table: "Sales",
                columns: new[] { "TenantId", "ServerId" },
                unique: true,
                filter: "\"ServerId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sales_TenantId_ReceiptBarcodeValue",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_TenantId_ServerId",
                table: "Sales");

            migrationBuilder.AlterColumn<string>(
                name: "ReceiptBarcodeValue",
                table: "Sales",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_ReceiptBarcodeValue",
                table: "Sales",
                columns: new[] { "TenantId", "ReceiptBarcodeValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_ServerId",
                table: "Sales",
                columns: new[] { "TenantId", "ServerId" },
                unique: true,
                filter: "ServerId IS NOT NULL");
        }
    }
}
