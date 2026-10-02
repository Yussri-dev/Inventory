START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917140549_AddSaleCustomerCorrections') THEN
    CREATE TABLE "SaleCustomerCorrections" (
        "Id" uuid NOT NULL,
        "SaleId" uuid NOT NULL,
        "PreviousCustomerId" uuid,
        "CustomerId" uuid NOT NULL,
        "ActorUserId" uuid NOT NULL,
        "Reason" character varying(500) NOT NULL,
        "TransferredDebt" numeric(18,2) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedByUserId" uuid,
        "IsDeleted" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DeletedByUserId" uuid,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_SaleCustomerCorrections" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SaleCustomerCorrections_Sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES "Sales" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_SaleCustomerCorrections_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917140549_AddSaleCustomerCorrections') THEN
    CREATE INDEX "IX_SaleCustomerCorrections_SaleId" ON "SaleCustomerCorrections" ("SaleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917140549_AddSaleCustomerCorrections') THEN
    CREATE INDEX "IX_SaleCustomerCorrections_TenantId_SaleId" ON "SaleCustomerCorrections" ("TenantId", "SaleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917140549_AddSaleCustomerCorrections') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260917140549_AddSaleCustomerCorrections', '9.0.10');
    END IF;
END $EF$;
COMMIT;

