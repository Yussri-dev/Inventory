using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    public partial class UpdateCashSessionAndMovementsServer
        : Migration
    {
        protected override void Up(
            MigrationBuilder migrationBuilder)
        {
            /*
             * Ajouter la colonne temporairement nullable.
             */
            migrationBuilder.AddColumn<Guid>(
                name: "ClientOperationId",
                table: "Sales",
                type: "uuid",
                nullable: true);

            /*
             * Donner une valeur unique à chaque vente existante.
             */
            migrationBuilder.Sql(
                """
                UPDATE "Sales"
                SET "ClientOperationId" = gen_random_uuid()
                WHERE "ClientOperationId" IS NULL
                   OR "ClientOperationId" =
                      '00000000-0000-0000-0000-000000000000';
                """);

            /*
             * Rendre ensuite la colonne obligatoire.
             */
            migrationBuilder.AlterColumn<Guid>(
                name: "ClientOperationId",
                table: "Sales",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowCredit",
                table: "Customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasUnlimitedCredit",
                table: "Customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name:
                    "IX_Sales_TenantId_ClientOperationId",
                table: "Sales",
                columns: new[]
                {
                    "TenantId",
                    "ClientOperationId"
                },
                unique: true);
        }

        protected override void Down(
            MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name:
                    "IX_Sales_TenantId_ClientOperationId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ClientOperationId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "AllowCredit",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "HasUnlimitedCredit",
                table: "Customers");
        }
    }
}