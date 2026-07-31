using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptBarcodeValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(
            MigrationBuilder migrationBuilder)
        {
           
            migrationBuilder.AddColumn<string>(
                name: "ReceiptBarcodeValue",
                table: "Sales",
                type: "TEXT",
                maxLength: 32,
                nullable: true);

           
            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_ReceiptBarcodeValue",
                table: "Sales",
                columns: new[]
                {
                    "TenantId",
                    "ReceiptBarcodeValue"
                },
                unique: true,
                filter:
                    "\"ReceiptBarcodeValue\" IS NOT NULL " +
                    "AND trim(\"ReceiptBarcodeValue\") <> ''");
        }

        /// <inheritdoc />
        protected override void Down(
            MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sales_TenantId_ReceiptBarcodeValue",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ReceiptBarcodeValue",
                table: "Sales");
        }
    }
}