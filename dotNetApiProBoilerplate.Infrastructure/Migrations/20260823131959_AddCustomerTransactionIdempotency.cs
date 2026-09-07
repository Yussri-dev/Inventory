using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerTransactionIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerTransactions_CashSessions_CashSessionId",
                table: "CustomerTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CustomerTransactions_TenantId",
                table: "CustomerTransactions");

            migrationBuilder.Sql(
                """
                UPDATE "CustomerTransactions"
                SET "ClientOperationId" = "Id"
                WHERE "ClientOperationId" =
                    '00000000-0000-0000-0000-000000000000';
            """);


            migrationBuilder.CreateIndex(
                name: "IX_CustomerTransactions_TenantId_CashSessionId",
                table: "CustomerTransactions",
                columns: new[] { "TenantId", "CashSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTransactions_TenantId_ClientOperationId",
                table: "CustomerTransactions",
                columns: new[] { "TenantId", "ClientOperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTransactions_TenantId_CustomerId_TransactionDate",
                table: "CustomerTransactions",
                columns: new[] { "TenantId", "CustomerId", "TransactionDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerTransactions_CashSessions_CashSessionId",
                table: "CustomerTransactions",
                column: "CashSessionId",
                principalTable: "CashSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerTransactions_CashSessions_CashSessionId",
                table: "CustomerTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CustomerTransactions_TenantId_CashSessionId",
                table: "CustomerTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CustomerTransactions_TenantId_ClientOperationId",
                table: "CustomerTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CustomerTransactions_TenantId_CustomerId_TransactionDate",
                table: "CustomerTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTransactions_TenantId",
                table: "CustomerTransactions",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerTransactions_CashSessions_CashSessionId",
                table: "CustomerTransactions",
                column: "CashSessionId",
                principalTable: "CashSessions",
                principalColumn: "Id");
        }
    }
}
