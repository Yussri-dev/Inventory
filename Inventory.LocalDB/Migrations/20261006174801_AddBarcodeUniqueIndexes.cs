using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class AddBarcodeUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // 1. REMOVE OLD INDEXES
            // ============================================================

            migrationBuilder.DropIndex(
                name: "IX_Products_Barcode",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_CatalogProductId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_ServerId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductCatalogs_Barcode",
                table: "ProductCatalogs");


            // ============================================================
            // 2. REPAIR LEGACY INTERNAL PRODUCT CATALOG BARCODES
            // ============================================================
            //
            // Internal barcode rule:
            //
            // Barcode =
            //     "10000000"
            //     + InternalCode padded to 5 digits
            //
            // Examples:
            //
            // 2780  -> 1000000002780
            // 89    -> 1000000000089
            // 11526 -> 1000000011526
            //
            // IMPORTANT:
            // Only existing internal barcodes beginning with "100000"
            // are repaired.
            //
            // Manufacturer EAN/UPC barcodes are not modified.
            // ============================================================

            migrationBuilder.Sql(
                """
                UPDATE ProductCatalogs
                SET Barcode =
                    '10000000' ||
                    printf(
                        '%05d',
                        CAST(TRIM(InternalCode) AS INTEGER)
                    )
                WHERE IsDeleted = 0
                  AND Barcode IS NOT NULL
                  AND TRIM(Barcode) LIKE '100000%'
                  AND InternalCode IS NOT NULL
                  AND TRIM(InternalCode) <> ''
                  AND TRIM(InternalCode) NOT GLOB '*[^0-9]*'
                  AND LENGTH(TRIM(InternalCode)) <= 5
                  AND TRIM(Barcode) <>
                      '10000000' ||
                      printf(
                          '%05d',
                          CAST(TRIM(InternalCode) AS INTEGER)
                      );
                """);


            // ============================================================
            // 3. NORMALIZE PRODUCT CATALOG BARCODES
            // ============================================================
            //
            // Remove leading/trailing whitespace before installing
            // unique indexes.
            // ============================================================

            migrationBuilder.Sql(
                """
                UPDATE ProductCatalogs
                SET Barcode = TRIM(Barcode)
                WHERE Barcode IS NOT NULL
                  AND Barcode <> TRIM(Barcode);
                """);


            // ============================================================
            // 4. REALIGN LINKED PRODUCTS FROM PRODUCT CATALOG
            // ============================================================
            //
            // For a Product linked to ProductCatalog:
            //
            //     ProductCatalog.Barcode
            //
            // is the canonical barcode.
            //
            // Custom Products are not affected because their
            // CatalogProductId is NULL.
            // ============================================================

            migrationBuilder.Sql(
                """
                UPDATE Products
                SET Barcode =
                (
                    SELECT pc.Barcode
                    FROM ProductCatalogs pc
                    WHERE pc.Id = Products.CatalogProductId
                      AND pc.IsDeleted = 0
                )
                WHERE IsDeletedLocally = 0
                  AND CatalogProductId IS NOT NULL
                  AND EXISTS
                  (
                      SELECT 1
                      FROM ProductCatalogs pc
                      WHERE pc.Id = Products.CatalogProductId
                        AND pc.IsDeleted = 0
                        AND COALESCE(TRIM(Products.Barcode), '') <>
                            COALESCE(TRIM(pc.Barcode), '')
                  );
                """);


            // ============================================================
            // 5. NORMALIZE PRODUCT BARCODES
            // ============================================================

            migrationBuilder.Sql(
                """
                UPDATE Products
                SET Barcode = TRIM(Barcode)
                WHERE Barcode IS NOT NULL
                  AND Barcode <> TRIM(Barcode);
                """);


            // ============================================================
            // 6. UNIQUE ACTIVE PRODUCT BARCODE PER TENANT
            // ============================================================
            //
            // Business rule:
            //
            // One active barcode
            //      =
            // one active Product
            // inside one Tenant.
            //
            // Deleted or inactive Products are excluded.
            // ============================================================

            migrationBuilder.CreateIndex(
                name: "UX_Products_Tenant_Barcode",
                table: "Products",
                columns: new[]
                {
                    "TenantId",
                    "Barcode"
                },
                unique: true,
                filter:
                    "\"Barcode\" IS NOT NULL " +
                    "AND trim(\"Barcode\") <> '' " +
                    "AND \"IsDeletedLocally\" = 0 " +
                    "AND \"IsActive\" = 1");


            // ============================================================
            // 7. UNIQUE ACTIVE CATALOG PRODUCT PER TENANT
            // ============================================================

            migrationBuilder.CreateIndex(
                name: "UX_Products_Tenant_CatalogProductId",
                table: "Products",
                columns: new[]
                {
                    "TenantId",
                    "CatalogProductId"
                },
                unique: true,
                filter:
                    "\"CatalogProductId\" IS NOT NULL " +
                    "AND \"IsDeletedLocally\" = 0");


            // ============================================================
            // 8. UNIQUE SERVER PRODUCT PER TENANT
            // ============================================================

            migrationBuilder.CreateIndex(
                name: "UX_Products_Tenant_ServerId",
                table: "Products",
                columns: new[]
                {
                    "TenantId",
                    "ServerId"
                },
                unique: true,
                filter:
                    "\"ServerId\" IS NOT NULL");


            // ============================================================
            // 9. UNIQUE ACTIVE PRODUCT CATALOG BARCODE
            // ============================================================
            //
            // ProductCatalog is global, therefore TenantId is not part
            // of this index.
            //
            // Deleted catalogs are excluded.
            // ============================================================

            migrationBuilder.CreateIndex(
                name: "UX_ProductCatalogs_Active_Barcode",
                table: "ProductCatalogs",
                column: "Barcode",
                unique: true,
                filter:
                    "\"Barcode\" IS NOT NULL " +
                    "AND trim(\"Barcode\") <> '' " +
                    "AND \"IsDeleted\" = 0");
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // REMOVE NEW INDEXES
            // ============================================================

            migrationBuilder.DropIndex(
                name: "UX_Products_Tenant_Barcode",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "UX_Products_Tenant_CatalogProductId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "UX_Products_Tenant_ServerId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "UX_ProductCatalogs_Active_Barcode",
                table: "ProductCatalogs");


            // ============================================================
            // RESTORE PREVIOUS INDEXES
            // ============================================================

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_CatalogProductId",
                table: "Products",
                columns: new[]
                {
                    "TenantId",
                    "CatalogProductId"
                },
                unique: true,
                filter:
                    "CatalogProductId IS NOT NULL " +
                    "AND IsDeletedLocally = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_ServerId",
                table: "Products",
                columns: new[]
                {
                    "TenantId",
                    "ServerId"
                },
                unique: true,
                filter:
                    "ServerId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCatalogs_Barcode",
                table: "ProductCatalogs",
                column: "Barcode");
        }
    }
}