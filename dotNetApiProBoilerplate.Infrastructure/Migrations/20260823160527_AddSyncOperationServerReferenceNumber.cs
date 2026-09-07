using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    public partial class AddSyncOperationServerReferenceNumber
        : Migration
    {
        protected override void Up(
            MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ServerReferenceNumber",
                table: "SyncOperationRecords",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            /*
             * Backfill previously synchronized Sale operations
             * using their linked server Sale.
             */
            migrationBuilder.Sql(
                """
                UPDATE "SyncOperationRecords" AS operation
                SET "ServerReferenceNumber" = sale."InvoiceNumber"
                FROM "Sales" AS sale
                WHERE operation."EntityName" = 'Sale'
                  AND operation."ServerEntityId" = sale."Id"
                  AND operation."ServerReferenceNumber" IS NULL;
                """);
        }

        protected override void Down(
            MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServerReferenceNumber",
                table: "SyncOperationRecords");
        }
    }
}