using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalReceipt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceiptPrintLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocalReceiptId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PrintType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CopyNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    PrintedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PrintedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    WasSuccessful = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptPrintLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Receipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocalSaleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerSaleId = table.Column<Guid>(type: "TEXT", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SnapshotJson = table.Column<string>(type: "TEXT", nullable: false),
                    SnapshotHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SyncStatus = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receipts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptPrintLogs_TenantId_LocalReceiptId_PrintedAtUtc",
                table: "ReceiptPrintLogs",
                columns: new[] { "TenantId", "LocalReceiptId", "PrintedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_TenantId_InvoiceNumber",
                table: "Receipts",
                columns: new[] { "TenantId", "InvoiceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_TenantId_LocalSaleId",
                table: "Receipts",
                columns: new[] { "TenantId", "LocalSaleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceiptPrintLogs");

            migrationBuilder.DropTable(
                name: "Receipts");
        }
    }
}
