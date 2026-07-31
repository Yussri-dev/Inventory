using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    public partial class RemoveEmptyClientOperationDefaults
        : Migration
    {
        protected override void Up(
            MigrationBuilder migrationBuilder)
        {
            /*
             * Remplacer les GUID vides éventuellement déjà présents.
             */
            migrationBuilder.Sql(
                """
                UPDATE "Sales"
                SET "ClientOperationId" = gen_random_uuid()
                WHERE "ClientOperationId" =
                    '00000000-0000-0000-0000-000000000000';

                UPDATE "Purchases"
                SET "ClientOperationId" = gen_random_uuid()
                WHERE "ClientOperationId" =
                    '00000000-0000-0000-0000-000000000000';

                UPDATE "Returns"
                SET "ClientOperationId" = gen_random_uuid()
                WHERE "ClientOperationId" =
                    '00000000-0000-0000-0000-000000000000';

                UPDATE "CustomerTransactions"
                SET "ClientOperationId" = gen_random_uuid()
                WHERE "ClientOperationId" =
                    '00000000-0000-0000-0000-000000000000';

                UPDATE "CashSessions"
                SET "ClientOperationId" = gen_random_uuid()
                WHERE "ClientOperationId" =
                    '00000000-0000-0000-0000-000000000000';
                """);

            /*
             * Supprimer le GUID vide utilisé comme valeur automatique.
             */
            migrationBuilder.Sql(
                """
                ALTER TABLE "Sales"
                ALTER COLUMN "ClientOperationId"
                DROP DEFAULT;

                ALTER TABLE "Purchases"
                ALTER COLUMN "ClientOperationId"
                DROP DEFAULT;

                ALTER TABLE "Returns"
                ALTER COLUMN "ClientOperationId"
                DROP DEFAULT;

                ALTER TABLE "CustomerTransactions"
                ALTER COLUMN "ClientOperationId"
                DROP DEFAULT;

                ALTER TABLE "CashSessions"
                ALTER COLUMN "ClientOperationId"
                DROP DEFAULT;
                """);
        }

        protected override void Down(
            MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Sales"
                ALTER COLUMN "ClientOperationId"
                SET DEFAULT
                    '00000000-0000-0000-0000-000000000000';

                ALTER TABLE "Purchases"
                ALTER COLUMN "ClientOperationId"
                SET DEFAULT
                    '00000000-0000-0000-0000-000000000000';

                ALTER TABLE "Returns"
                ALTER COLUMN "ClientOperationId"
                SET DEFAULT
                    '00000000-0000-0000-0000-000000000000';

                ALTER TABLE "CustomerTransactions"
                ALTER COLUMN "ClientOperationId"
                SET DEFAULT
                    '00000000-0000-0000-0000-000000000000';

                ALTER TABLE "CashSessions"
                ALTER COLUMN "ClientOperationId"
                SET DEFAULT
                    '00000000-0000-0000-0000-000000000000';
                """);
        }
    }
}