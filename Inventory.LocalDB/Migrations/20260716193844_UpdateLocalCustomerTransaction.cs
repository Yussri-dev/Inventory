using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLocalCustomerTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClientOperationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerLocalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LocalCashSessionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ServerCashSessionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SaleLocalId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SaleServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Origin = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UploadRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCash = table.Column<bool>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TransactionDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SyncStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerTransactions_Customers_CustomerLocalId",
                        column: x => x.CustomerLocalId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTransactions_CustomerLocalId",
                table: "CustomerTransactions",
                column: "CustomerLocalId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTransactions_TenantId",
                table: "CustomerTransactions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTransactions_TenantId_ClientOperationId",
                table: "CustomerTransactions",
                columns: new[] { "TenantId", "ClientOperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTransactions_TenantId_CustomerLocalId_TransactionDateUtc",
                table: "CustomerTransactions",
                columns: new[] { "TenantId", "CustomerLocalId", "TransactionDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTransactions_TenantId_Origin_SaleLocalId",
                table: "CustomerTransactions",
                columns: new[] { "TenantId", "Origin", "SaleLocalId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTransactions_TenantId_ServerId",
                table: "CustomerTransactions",
                columns: new[] { "TenantId", "ServerId" },
                unique: true,
                filter: "ServerId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTransactions_TenantId_SyncStatus",
                table: "CustomerTransactions",
                columns: new[] { "TenantId", "SyncStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerTransactions");
        }
    }
}
