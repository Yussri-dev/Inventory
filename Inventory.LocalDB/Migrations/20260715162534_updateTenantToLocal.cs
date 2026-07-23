using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class updateTenantToLocal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncTableStates_EntityName",
                table: "SyncTableStates");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_IsDeleted",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_Name",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_ServerId",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_ProductBarcode",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_ProductLocalId",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_ProductServerId",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_ServerId",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_LocalReferenceId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ProductLocalId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ProductServerId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ServerId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ServerReferenceId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_SyncStatus",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_Type",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_Sales_LocalInvoiceNumber",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_SaleDateUtc",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_ServerId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_SyncStatus",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_SaleLines_ProductLocalId",
                table: "SaleLines");

            migrationBuilder.DropIndex(
                name: "IX_SaleLines_ProductServerId",
                table: "SaleLines");

            migrationBuilder.DropIndex(
                name: "IX_SaleLines_UnitProductServerId",
                table: "SaleLines");

            migrationBuilder.DropIndex(
                name: "IX_Returns_LocalReturnNumber",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_Returns_LocalSaleId",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_Returns_RefundMethod",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_Returns_ReturnDateUtc",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_Returns_ServerId",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_Returns_ServerReturnNumber",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_Returns_ServerSaleId",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_Returns_SyncStatus",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_ReturnLines_ProductBarcode",
                table: "ReturnLines");

            migrationBuilder.DropIndex(
                name: "IX_ReturnLines_ProductLocalId",
                table: "ReturnLines");

            migrationBuilder.DropIndex(
                name: "IX_ReturnLines_ProductServerId",
                table: "ReturnLines");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_LocalPurchaseNumber",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_PurchaseDateUtc",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_ServerId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_ServerPurchaseNumber",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_Status",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_SupplierLocalId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_SupplierServerId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_SyncStatus",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_PurchasePayments_Method",
                table: "PurchasePayments");

            migrationBuilder.DropIndex(
                name: "IX_PurchasePayments_PaymentDateUtc",
                table: "PurchasePayments");

            migrationBuilder.DropIndex(
                name: "IX_PurchasePayments_ServerId",
                table: "PurchasePayments");

            migrationBuilder.DropIndex(
                name: "IX_PurchasePayments_ServerPurchaseId",
                table: "PurchasePayments");

            migrationBuilder.DropIndex(
                name: "IX_PurchasePayments_SyncStatus",
                table: "PurchasePayments");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseLines_ProductBarcode",
                table: "PurchaseLines");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseLines_ProductLocalId",
                table: "PurchaseLines");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseLines_ProductServerId",
                table: "PurchaseLines");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_CatalogProductId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Method",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ServerId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ServerSaleId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_SyncStatus",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_CashSessions_ServerId",
                table: "CashSessions");

            migrationBuilder.DropIndex(
                name: "IX_CashSessions_SessionNumber",
                table: "CashSessions");

            migrationBuilder.DropIndex(
                name: "IX_CashSessions_Status",
                table: "CashSessions");

            migrationBuilder.DropIndex(
                name: "IX_CashSessions_SyncStatus",
                table: "CashSessions");

            migrationBuilder.DropIndex(
                name: "IX_CashReports_GeneratedAtUtc",
                table: "CashReports");

            migrationBuilder.DropIndex(
                name: "IX_CashReports_ServerCashSessionId",
                table: "CashReports");

            migrationBuilder.DropIndex(
                name: "IX_CashReports_ServerId",
                table: "CashReports");

            migrationBuilder.DropIndex(
                name: "IX_CashReports_SyncStatus",
                table: "CashReports");

            migrationBuilder.DropIndex(
                name: "IX_CashReports_Type",
                table: "CashReports");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_ServerId",
                table: "CashMovements");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_SyncStatus",
                table: "CashMovements");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_Type",
                table: "CashMovements");

            migrationBuilder.DropIndex(
                name: "IX_CashCorrections_CorrectedAtUtc",
                table: "CashCorrections");

            migrationBuilder.DropIndex(
                name: "IX_CashCorrections_OriginalServerCashSessionId",
                table: "CashCorrections");

            migrationBuilder.DropIndex(
                name: "IX_CashCorrections_ServerId",
                table: "CashCorrections");

            migrationBuilder.DropIndex(
                name: "IX_CashCorrections_SyncStatus",
                table: "CashCorrections");

            migrationBuilder.DropColumn(
                name: "LocalVersion",
                table: "SyncTableStates");

            migrationBuilder.RenameColumn(
                name: "ServerVersion",
                table: "SyncTableStates",
                newName: "InitialSyncCompleted");

            migrationBuilder.RenameColumn(
                name: "LastSyncUtc",
                table: "SyncTableStates",
                newName: "LastSuccessfulSyncAtUtc");

            migrationBuilder.RenameColumn(
                name: "LastSyncErrorMessage",
                table: "SyncTableStates",
                newName: "LastServerChangeAtUtc");

            migrationBuilder.AddColumn<string>(
                name: "ContinuationToken",
                table: "SyncTableStates",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "SyncTableStates",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "SyncTableStates",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "Suppliers",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Stocks",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "StockMovements",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Sales",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "SaleLines",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Returns",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ReturnLines",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Purchases",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PurchasePayments",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PurchaseLines",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Payments",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "CashSessions",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "CashReports",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "CashMovements",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "CashCorrections",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_SyncTableStates_TenantId_EntityName",
                table: "SyncTableStates",
                columns: new[] { "TenantId", "EntityName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncTableStates_TenantId_InitialSyncCompleted",
                table: "SyncTableStates",
                columns: new[] { "TenantId", "InitialSyncCompleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_TenantId",
                table: "Suppliers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_TenantId_IsDeleted",
                table: "Suppliers",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_TenantId_Name",
                table: "Suppliers",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_TenantId_ServerId",
                table: "Suppliers",
                columns: new[] { "TenantId", "ServerId" },
                unique: true,
                filter: "ServerId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_TenantId",
                table: "Stocks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_TenantId_ProductBarcode",
                table: "Stocks",
                columns: new[] { "TenantId", "ProductBarcode" });

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_TenantId_ProductLocalId",
                table: "Stocks",
                columns: new[] { "TenantId", "ProductLocalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_TenantId_ProductServerId",
                table: "Stocks",
                columns: new[] { "TenantId", "ProductServerId" },
                unique: true,
                filter: "ProductServerId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_TenantId_ServerId",
                table: "Stocks",
                columns: new[] { "TenantId", "ServerId" },
                unique: true,
                filter: "ServerId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId",
                table: "StockMovements",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_LocalReferenceId",
                table: "StockMovements",
                columns: new[] { "TenantId", "LocalReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_ProductLocalId",
                table: "StockMovements",
                columns: new[] { "TenantId", "ProductLocalId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_ProductServerId",
                table: "StockMovements",
                columns: new[] { "TenantId", "ProductServerId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_ServerId",
                table: "StockMovements",
                columns: new[] { "TenantId", "ServerId" },
                unique: true,
                filter: "ServerId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_ServerReferenceId",
                table: "StockMovements",
                columns: new[] { "TenantId", "ServerReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_SyncStatus",
                table: "StockMovements",
                columns: new[] { "TenantId", "SyncStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_TenantId_Type",
                table: "StockMovements",
                columns: new[] { "TenantId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId",
                table: "Sales",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_LocalInvoiceNumber",
                table: "Sales",
                columns: new[] { "TenantId", "LocalInvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_ServerId",
                table: "Sales",
                columns: new[] { "TenantId", "ServerId" },
                unique: true,
                filter: "ServerId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_SyncStatus_SaleDateUtc",
                table: "Sales",
                columns: new[] { "TenantId", "SyncStatus", "SaleDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleLines_TenantId",
                table: "SaleLines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleLines_TenantId_LocalSaleId",
                table: "SaleLines",
                columns: new[] { "TenantId", "LocalSaleId" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleLines_TenantId_ProductLocalId",
                table: "SaleLines",
                columns: new[] { "TenantId", "ProductLocalId" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleLines_TenantId_ProductServerId",
                table: "SaleLines",
                columns: new[] { "TenantId", "ProductServerId" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleLines_TenantId_UnitProductServerId",
                table: "SaleLines",
                columns: new[] { "TenantId", "UnitProductServerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Returns_TenantId",
                table: "Returns",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Returns_TenantId_LocalReturnNumber",
                table: "Returns",
                columns: new[] { "TenantId", "LocalReturnNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Returns_TenantId_LocalSaleId",
                table: "Returns",
                columns: new[] { "TenantId", "LocalSaleId" });

            migrationBuilder.CreateIndex(
                name: "IX_Returns_TenantId_RefundMethod",
                table: "Returns",
                columns: new[] { "TenantId", "RefundMethod" });

            migrationBuilder.CreateIndex(
                name: "IX_Returns_TenantId_ServerId",
                table: "Returns",
                columns: new[] { "TenantId", "ServerId" },
                unique: true,
                filter: "ServerId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Returns_TenantId_ServerReturnNumber",
                table: "Returns",
                columns: new[] { "TenantId", "ServerReturnNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Returns_TenantId_ServerSaleId",
                table: "Returns",
                columns: new[] { "TenantId", "ServerSaleId" });

            migrationBuilder.CreateIndex(
                name: "IX_Returns_TenantId_SyncStatus_ReturnDateUtc",
                table: "Returns",
                columns: new[] { "TenantId", "SyncStatus", "ReturnDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnLines_TenantId",
                table: "ReturnLines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnLines_TenantId_LocalReturnId",
                table: "ReturnLines",
                columns: new[] { "TenantId", "LocalReturnId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnLines_TenantId_ProductBarcode",
                table: "ReturnLines",
                columns: new[] { "TenantId", "ProductBarcode" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnLines_TenantId_ProductLocalId",
                table: "ReturnLines",
                columns: new[] { "TenantId", "ProductLocalId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnLines_TenantId_ProductServerId",
                table: "ReturnLines",
                columns: new[] { "TenantId", "ProductServerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_TenantId",
                table: "Purchases",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_TenantId_LocalPurchaseNumber",
                table: "Purchases",
                columns: new[] { "TenantId", "LocalPurchaseNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_TenantId_ServerId",
                table: "Purchases",
                columns: new[] { "TenantId", "ServerId" },
                unique: true,
                filter: "ServerId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_TenantId_ServerPurchaseNumber",
                table: "Purchases",
                columns: new[] { "TenantId", "ServerPurchaseNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_TenantId_Status",
                table: "Purchases",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_TenantId_SupplierLocalId",
                table: "Purchases",
                columns: new[] { "TenantId", "SupplierLocalId" });

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_TenantId_SupplierServerId",
                table: "Purchases",
                columns: new[] { "TenantId", "SupplierServerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_TenantId_SyncStatus_PurchaseDateUtc",
                table: "Purchases",
                columns: new[] { "TenantId", "SyncStatus", "PurchaseDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePayments_TenantId",
                table: "PurchasePayments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePayments_TenantId_LocalPurchaseId",
                table: "PurchasePayments",
                columns: new[] { "TenantId", "LocalPurchaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePayments_TenantId_Method",
                table: "PurchasePayments",
                columns: new[] { "TenantId", "Method" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePayments_TenantId_ServerId",
                table: "PurchasePayments",
                columns: new[] { "TenantId", "ServerId" },
                unique: true,
                filter: "ServerId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePayments_TenantId_ServerPurchaseId",
                table: "PurchasePayments",
                columns: new[] { "TenantId", "ServerPurchaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePayments_TenantId_SyncStatus_PaymentDateUtc",
                table: "PurchasePayments",
                columns: new[] { "TenantId", "SyncStatus", "PaymentDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseLines_TenantId",
                table: "PurchaseLines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseLines_TenantId_LocalPurchaseId",
                table: "PurchaseLines",
                columns: new[] { "TenantId", "LocalPurchaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseLines_TenantId_ProductBarcode",
                table: "PurchaseLines",
                columns: new[] { "TenantId", "ProductBarcode" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseLines_TenantId_ProductLocalId",
                table: "PurchaseLines",
                columns: new[] { "TenantId", "ProductLocalId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseLines_TenantId_ProductServerId",
                table: "PurchaseLines",
                columns: new[] { "TenantId", "ProductServerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CatalogProductId",
                table: "Products",
                column: "CatalogProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_CatalogProductId",
                table: "Products",
                columns: new[] { "TenantId", "CatalogProductId" },
                unique: true,
                filter: "CatalogProductId IS NOT NULL AND IsDeletedLocally = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId",
                table: "Payments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_LocalSaleId",
                table: "Payments",
                columns: new[] { "TenantId", "LocalSaleId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_Method",
                table: "Payments",
                columns: new[] { "TenantId", "Method" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_ServerId",
                table: "Payments",
                columns: new[] { "TenantId", "ServerId" },
                unique: true,
                filter: "ServerId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_ServerSaleId",
                table: "Payments",
                columns: new[] { "TenantId", "ServerSaleId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_SyncStatus",
                table: "Payments",
                columns: new[] { "TenantId", "SyncStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId",
                table: "Customers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_IsDeleted",
                table: "Customers",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_ServerId",
                table: "Customers",
                columns: new[] { "TenantId", "ServerId" },
                unique: true,
                filter: "ServerId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_TenantId",
                table: "CashSessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_TenantId_ServerId",
                table: "CashSessions",
                columns: new[] { "TenantId", "ServerId" },
                unique: true,
                filter: "ServerId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_TenantId_SessionNumber",
                table: "CashSessions",
                columns: new[] { "TenantId", "SessionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_TenantId_Status",
                table: "CashSessions",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_TenantId_SyncStatus",
                table: "CashSessions",
                columns: new[] { "TenantId", "SyncStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_CashReports_TenantId",
                table: "CashReports",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CashReports_TenantId_LocalCashSessionId",
                table: "CashReports",
                columns: new[] { "TenantId", "LocalCashSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_CashReports_TenantId_ServerCashSessionId",
                table: "CashReports",
                columns: new[] { "TenantId", "ServerCashSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_CashReports_TenantId_ServerId",
                table: "CashReports",
                columns: new[] { "TenantId", "ServerId" },
                unique: true,
                filter: "ServerId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CashReports_TenantId_SyncStatus_GeneratedAtUtc",
                table: "CashReports",
                columns: new[] { "TenantId", "SyncStatus", "GeneratedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CashReports_TenantId_Type",
                table: "CashReports",
                columns: new[] { "TenantId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_TenantId",
                table: "CashMovements",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_TenantId_LocalCashSessionId",
                table: "CashMovements",
                columns: new[] { "TenantId", "LocalCashSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_TenantId_ServerId",
                table: "CashMovements",
                columns: new[] { "TenantId", "ServerId" },
                unique: true,
                filter: "ServerId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_TenantId_SyncStatus",
                table: "CashMovements",
                columns: new[] { "TenantId", "SyncStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_TenantId_Type",
                table: "CashMovements",
                columns: new[] { "TenantId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_CashCorrections_TenantId",
                table: "CashCorrections",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CashCorrections_TenantId_OriginalLocalCashSessionId",
                table: "CashCorrections",
                columns: new[] { "TenantId", "OriginalLocalCashSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_CashCorrections_TenantId_OriginalServerCashSessionId",
                table: "CashCorrections",
                columns: new[] { "TenantId", "OriginalServerCashSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_CashCorrections_TenantId_ServerId",
                table: "CashCorrections",
                columns: new[] { "TenantId", "ServerId" },
                unique: true,
                filter: "ServerId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CashCorrections_TenantId_SyncStatus_CorrectedAtUtc",
                table: "CashCorrections",
                columns: new[] { "TenantId", "SyncStatus", "CorrectedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ProductCatalogs_CatalogProductId",
                table: "Products",
                column: "CatalogProductId",
                principalTable: "ProductCatalogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_ProductCatalogs_CatalogProductId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_SyncTableStates_TenantId_EntityName",
                table: "SyncTableStates");

            migrationBuilder.DropIndex(
                name: "IX_SyncTableStates_TenantId_InitialSyncCompleted",
                table: "SyncTableStates");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_TenantId",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_TenantId_IsDeleted",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_TenantId_Name",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_TenantId_ServerId",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_TenantId",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_TenantId_ProductBarcode",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_TenantId_ProductLocalId",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_TenantId_ProductServerId",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_TenantId_ServerId",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId_LocalReferenceId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId_ProductLocalId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId_ProductServerId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId_ServerId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId_ServerReferenceId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId_SyncStatus",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_TenantId_Type",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_Sales_TenantId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_TenantId_LocalInvoiceNumber",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_TenantId_ServerId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_TenantId_SyncStatus_SaleDateUtc",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_SaleLines_TenantId",
                table: "SaleLines");

            migrationBuilder.DropIndex(
                name: "IX_SaleLines_TenantId_LocalSaleId",
                table: "SaleLines");

            migrationBuilder.DropIndex(
                name: "IX_SaleLines_TenantId_ProductLocalId",
                table: "SaleLines");

            migrationBuilder.DropIndex(
                name: "IX_SaleLines_TenantId_ProductServerId",
                table: "SaleLines");

            migrationBuilder.DropIndex(
                name: "IX_SaleLines_TenantId_UnitProductServerId",
                table: "SaleLines");

            migrationBuilder.DropIndex(
                name: "IX_Returns_TenantId",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_Returns_TenantId_LocalReturnNumber",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_Returns_TenantId_LocalSaleId",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_Returns_TenantId_RefundMethod",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_Returns_TenantId_ServerId",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_Returns_TenantId_ServerReturnNumber",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_Returns_TenantId_ServerSaleId",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_Returns_TenantId_SyncStatus_ReturnDateUtc",
                table: "Returns");

            migrationBuilder.DropIndex(
                name: "IX_ReturnLines_TenantId",
                table: "ReturnLines");

            migrationBuilder.DropIndex(
                name: "IX_ReturnLines_TenantId_LocalReturnId",
                table: "ReturnLines");

            migrationBuilder.DropIndex(
                name: "IX_ReturnLines_TenantId_ProductBarcode",
                table: "ReturnLines");

            migrationBuilder.DropIndex(
                name: "IX_ReturnLines_TenantId_ProductLocalId",
                table: "ReturnLines");

            migrationBuilder.DropIndex(
                name: "IX_ReturnLines_TenantId_ProductServerId",
                table: "ReturnLines");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_TenantId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_TenantId_LocalPurchaseNumber",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_TenantId_ServerId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_TenantId_ServerPurchaseNumber",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_TenantId_Status",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_TenantId_SupplierLocalId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_TenantId_SupplierServerId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_TenantId_SyncStatus_PurchaseDateUtc",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_PurchasePayments_TenantId",
                table: "PurchasePayments");

            migrationBuilder.DropIndex(
                name: "IX_PurchasePayments_TenantId_LocalPurchaseId",
                table: "PurchasePayments");

            migrationBuilder.DropIndex(
                name: "IX_PurchasePayments_TenantId_Method",
                table: "PurchasePayments");

            migrationBuilder.DropIndex(
                name: "IX_PurchasePayments_TenantId_ServerId",
                table: "PurchasePayments");

            migrationBuilder.DropIndex(
                name: "IX_PurchasePayments_TenantId_ServerPurchaseId",
                table: "PurchasePayments");

            migrationBuilder.DropIndex(
                name: "IX_PurchasePayments_TenantId_SyncStatus_PaymentDateUtc",
                table: "PurchasePayments");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseLines_TenantId",
                table: "PurchaseLines");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseLines_TenantId_LocalPurchaseId",
                table: "PurchaseLines");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseLines_TenantId_ProductBarcode",
                table: "PurchaseLines");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseLines_TenantId_ProductLocalId",
                table: "PurchaseLines");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseLines_TenantId_ProductServerId",
                table: "PurchaseLines");

            migrationBuilder.DropIndex(
                name: "IX_Products_CatalogProductId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_CatalogProductId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TenantId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TenantId_LocalSaleId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TenantId_Method",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TenantId_ServerId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TenantId_ServerSaleId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TenantId_SyncStatus",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TenantId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TenantId_IsDeleted",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TenantId_ServerId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_CashSessions_TenantId",
                table: "CashSessions");

            migrationBuilder.DropIndex(
                name: "IX_CashSessions_TenantId_ServerId",
                table: "CashSessions");

            migrationBuilder.DropIndex(
                name: "IX_CashSessions_TenantId_SessionNumber",
                table: "CashSessions");

            migrationBuilder.DropIndex(
                name: "IX_CashSessions_TenantId_Status",
                table: "CashSessions");

            migrationBuilder.DropIndex(
                name: "IX_CashSessions_TenantId_SyncStatus",
                table: "CashSessions");

            migrationBuilder.DropIndex(
                name: "IX_CashReports_TenantId",
                table: "CashReports");

            migrationBuilder.DropIndex(
                name: "IX_CashReports_TenantId_LocalCashSessionId",
                table: "CashReports");

            migrationBuilder.DropIndex(
                name: "IX_CashReports_TenantId_ServerCashSessionId",
                table: "CashReports");

            migrationBuilder.DropIndex(
                name: "IX_CashReports_TenantId_ServerId",
                table: "CashReports");

            migrationBuilder.DropIndex(
                name: "IX_CashReports_TenantId_SyncStatus_GeneratedAtUtc",
                table: "CashReports");

            migrationBuilder.DropIndex(
                name: "IX_CashReports_TenantId_Type",
                table: "CashReports");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_TenantId",
                table: "CashMovements");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_TenantId_LocalCashSessionId",
                table: "CashMovements");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_TenantId_ServerId",
                table: "CashMovements");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_TenantId_SyncStatus",
                table: "CashMovements");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_TenantId_Type",
                table: "CashMovements");

            migrationBuilder.DropIndex(
                name: "IX_CashCorrections_TenantId",
                table: "CashCorrections");

            migrationBuilder.DropIndex(
                name: "IX_CashCorrections_TenantId_OriginalLocalCashSessionId",
                table: "CashCorrections");

            migrationBuilder.DropIndex(
                name: "IX_CashCorrections_TenantId_OriginalServerCashSessionId",
                table: "CashCorrections");

            migrationBuilder.DropIndex(
                name: "IX_CashCorrections_TenantId_ServerId",
                table: "CashCorrections");

            migrationBuilder.DropIndex(
                name: "IX_CashCorrections_TenantId_SyncStatus_CorrectedAtUtc",
                table: "CashCorrections");

            migrationBuilder.DropColumn(
                name: "ContinuationToken",
                table: "SyncTableStates");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "SyncTableStates");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SyncTableStates");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SaleLines");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Returns");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ReturnLines");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PurchasePayments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PurchaseLines");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CashSessions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CashReports");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CashMovements");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CashCorrections");

            migrationBuilder.RenameColumn(
                name: "LastSuccessfulSyncAtUtc",
                table: "SyncTableStates",
                newName: "LastSyncUtc");

            migrationBuilder.RenameColumn(
                name: "LastServerChangeAtUtc",
                table: "SyncTableStates",
                newName: "LastSyncErrorMessage");

            migrationBuilder.RenameColumn(
                name: "InitialSyncCompleted",
                table: "SyncTableStates",
                newName: "ServerVersion");

            migrationBuilder.AddColumn<long>(
                name: "LocalVersion",
                table: "SyncTableStates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "Suppliers",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_SyncTableStates_EntityName",
                table: "SyncTableStates",
                column: "EntityName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_IsDeleted",
                table: "Suppliers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Name",
                table: "Suppliers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_ServerId",
                table: "Suppliers",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ProductBarcode",
                table: "Stocks",
                column: "ProductBarcode");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ProductLocalId",
                table: "Stocks",
                column: "ProductLocalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ProductServerId",
                table: "Stocks",
                column: "ProductServerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ServerId",
                table: "Stocks",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_LocalReferenceId",
                table: "StockMovements",
                column: "LocalReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductLocalId",
                table: "StockMovements",
                column: "ProductLocalId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductServerId",
                table: "StockMovements",
                column: "ProductServerId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ServerId",
                table: "StockMovements",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ServerReferenceId",
                table: "StockMovements",
                column: "ServerReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_SyncStatus",
                table: "StockMovements",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_Type",
                table: "StockMovements",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_LocalInvoiceNumber",
                table: "Sales",
                column: "LocalInvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_SaleDateUtc",
                table: "Sales",
                column: "SaleDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_ServerId",
                table: "Sales",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_SyncStatus",
                table: "Sales",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_SaleLines_ProductLocalId",
                table: "SaleLines",
                column: "ProductLocalId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleLines_ProductServerId",
                table: "SaleLines",
                column: "ProductServerId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleLines_UnitProductServerId",
                table: "SaleLines",
                column: "UnitProductServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Returns_LocalReturnNumber",
                table: "Returns",
                column: "LocalReturnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Returns_LocalSaleId",
                table: "Returns",
                column: "LocalSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_Returns_RefundMethod",
                table: "Returns",
                column: "RefundMethod");

            migrationBuilder.CreateIndex(
                name: "IX_Returns_ReturnDateUtc",
                table: "Returns",
                column: "ReturnDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Returns_ServerId",
                table: "Returns",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Returns_ServerReturnNumber",
                table: "Returns",
                column: "ServerReturnNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Returns_ServerSaleId",
                table: "Returns",
                column: "ServerSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_Returns_SyncStatus",
                table: "Returns",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnLines_ProductBarcode",
                table: "ReturnLines",
                column: "ProductBarcode");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnLines_ProductLocalId",
                table: "ReturnLines",
                column: "ProductLocalId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnLines_ProductServerId",
                table: "ReturnLines",
                column: "ProductServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_LocalPurchaseNumber",
                table: "Purchases",
                column: "LocalPurchaseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_PurchaseDateUtc",
                table: "Purchases",
                column: "PurchaseDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_ServerId",
                table: "Purchases",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_ServerPurchaseNumber",
                table: "Purchases",
                column: "ServerPurchaseNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_Status",
                table: "Purchases",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_SupplierLocalId",
                table: "Purchases",
                column: "SupplierLocalId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_SupplierServerId",
                table: "Purchases",
                column: "SupplierServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_SyncStatus",
                table: "Purchases",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePayments_Method",
                table: "PurchasePayments",
                column: "Method");

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePayments_PaymentDateUtc",
                table: "PurchasePayments",
                column: "PaymentDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePayments_ServerId",
                table: "PurchasePayments",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePayments_ServerPurchaseId",
                table: "PurchasePayments",
                column: "ServerPurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchasePayments_SyncStatus",
                table: "PurchasePayments",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseLines_ProductBarcode",
                table: "PurchaseLines",
                column: "ProductBarcode");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseLines_ProductLocalId",
                table: "PurchaseLines",
                column: "ProductLocalId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseLines_ProductServerId",
                table: "PurchaseLines",
                column: "ProductServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_CatalogProductId",
                table: "Products",
                columns: new[] { "TenantId", "CatalogProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Method",
                table: "Payments",
                column: "Method");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ServerId",
                table: "Payments",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ServerSaleId",
                table: "Payments",
                column: "ServerSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SyncStatus",
                table: "Payments",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_ServerId",
                table: "CashSessions",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_SessionNumber",
                table: "CashSessions",
                column: "SessionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_Status",
                table: "CashSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_SyncStatus",
                table: "CashSessions",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CashReports_GeneratedAtUtc",
                table: "CashReports",
                column: "GeneratedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CashReports_ServerCashSessionId",
                table: "CashReports",
                column: "ServerCashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CashReports_ServerId",
                table: "CashReports",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_CashReports_SyncStatus",
                table: "CashReports",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CashReports_Type",
                table: "CashReports",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_ServerId",
                table: "CashMovements",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_SyncStatus",
                table: "CashMovements",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_Type",
                table: "CashMovements",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_CashCorrections_CorrectedAtUtc",
                table: "CashCorrections",
                column: "CorrectedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CashCorrections_OriginalServerCashSessionId",
                table: "CashCorrections",
                column: "OriginalServerCashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CashCorrections_ServerId",
                table: "CashCorrections",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_CashCorrections_SyncStatus",
                table: "CashCorrections",
                column: "SyncStatus");
        }
    }
}
