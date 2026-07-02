using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.LocalDB.Migrations
{
    /// <inheritdoc />
    public partial class InitialLocalDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClientOperationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OpenedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OpeningAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosingAmountExpected = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosingAmountCounted = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Difference = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OpenedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OpeningNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ClosingNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SyncStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TaxNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeletedLocally = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CatalogProductId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Sku = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Barcode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Brand = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SalePrice2 = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SalePrice3 = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsTracked = table.Column<bool>(type: "INTEGER", nullable: false),
                    LocalStockQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    IsPack = table.Column<bool>(type: "INTEGER", nullable: false),
                    UnitProductServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UnitsPerPack = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeletedLocally = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Purchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClientOperationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SupplierLocalId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SupplierServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LocalPurchaseNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ServerPurchaseNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SupplierInvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TotalAmountExclVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalVatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmountInclVat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PurchaseDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpectedDeliveryDateUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeliveryDateUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PaymentDueDateUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PaymentDateUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SyncStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purchases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Returns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClientOperationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocalReturnNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ServerReturnNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LocalSaleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerSaleId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ReturnDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsProcessed = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SyncStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Returns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClientOperationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductLocalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductServerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProductBarcode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    QuantityChange = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    QuantityBefore = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    QuantityAfter = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LocalReferenceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ServerReferenceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    MovementDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SyncStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProductLocalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductServerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProductBarcode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    LastUpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncQueueItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LocalEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerEntityId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Operation = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Attempts = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClientOperationId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncQueueItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncTableStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LocalVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    ServerVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    LastSyncUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Syncmode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LastSyncErrorMessage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncTableStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CashCorrections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClientOperationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OriginalLocalCashSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OriginalServerCashSessionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CorrectedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CorrectedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ApprovalNotes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SyncStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashCorrections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashCorrections_CashSessions_OriginalLocalCashSessionId",
                        column: x => x.OriginalLocalCashSessionId,
                        principalTable: "CashSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClientOperationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocalCashSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerCashSessionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    LocalReferenceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ServerReferenceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    MovementDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SyncStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashMovements_CashSessions_LocalCashSessionId",
                        column: x => x.LocalCashSessionId,
                        principalTable: "CashSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CashReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LocalCashSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerCashSessionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ExpectedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CountedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Difference = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CashSales = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CardSales = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OtherPayments = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalTransactions = table.Column<int>(type: "INTEGER", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GeneratedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    SyncStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashReports_CashSessions_LocalCashSessionId",
                        column: x => x.LocalCashSessionId,
                        principalTable: "CashSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClientOperationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocalInvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ServerInvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CustomerLocalId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CustomerServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LocalCashSessionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CashSessionServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SubtotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ChangeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PaymentStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SyncStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SaleDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sales_CashSessions_LocalCashSessionId",
                        column: x => x.LocalCashSessionId,
                        principalTable: "CashSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocalPurchaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductLocalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProductBarcode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    QuantityOrdered = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    QuantityReceived = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPurchasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseLines_Purchases_LocalPurchaseId",
                        column: x => x.LocalPurchaseId,
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchasePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LocalPurchaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerPurchaseId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Method = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TransactionRef = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PaymentDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SyncStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchasePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchasePayments_Purchases_LocalPurchaseId",
                        column: x => x.LocalPurchaseId,
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReturnLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocalReturnId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductLocalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProductBarcode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RestockItem = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnLines_Returns_LocalReturnId",
                        column: x => x.LocalReturnId,
                        principalTable: "Returns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LocalSaleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerSaleId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Method = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionRef = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CardLastFourDigits = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsRefunded = table.Column<bool>(type: "INTEGER", nullable: false),
                    RefundedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SyncStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Sales_LocalSaleId",
                        column: x => x.LocalSaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SaleLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocalSaleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductLocalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductServerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProductBarcode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitProductLocalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UnitProductServerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UnitQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    IsPack = table.Column<bool>(type: "INTEGER", nullable: false),
                    UnitsPerPack = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitCostPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleLines_Sales_LocalSaleId",
                        column: x => x.LocalSaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashCorrections_ClientOperationId",
                table: "CashCorrections",
                column: "ClientOperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashCorrections_CorrectedAtUtc",
                table: "CashCorrections",
                column: "CorrectedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CashCorrections_OriginalLocalCashSessionId",
                table: "CashCorrections",
                column: "OriginalLocalCashSessionId");

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

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_ClientOperationId",
                table: "CashMovements",
                column: "ClientOperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_LocalCashSessionId",
                table: "CashMovements",
                column: "LocalCashSessionId");

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
                name: "IX_CashReports_GeneratedAtUtc",
                table: "CashReports",
                column: "GeneratedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CashReports_LocalCashSessionId",
                table: "CashReports",
                column: "LocalCashSessionId");

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
                name: "IX_CashSessions_ClientOperationId",
                table: "CashSessions",
                column: "ClientOperationId",
                unique: true);

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
                name: "IX_Customers_Name",
                table: "Customers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Phone",
                table: "Customers",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_ServerId",
                table: "Customers",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_LocalSaleId",
                table: "Payments",
                column: "LocalSaleId");

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
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ServerId",
                table: "Products",
                column: "ServerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_UnitProductServerId",
                table: "Products",
                column: "UnitProductServerId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseLines_LocalPurchaseId",
                table: "PurchaseLines",
                column: "LocalPurchaseId");

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
                name: "IX_PurchasePayments_LocalPurchaseId",
                table: "PurchasePayments",
                column: "LocalPurchaseId");

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
                name: "IX_Purchases_ClientOperationId",
                table: "Purchases",
                column: "ClientOperationId",
                unique: true);

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
                name: "IX_ReturnLines_LocalReturnId",
                table: "ReturnLines",
                column: "LocalReturnId");

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
                name: "IX_Returns_ClientOperationId",
                table: "Returns",
                column: "ClientOperationId",
                unique: true);

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
                name: "IX_SaleLines_LocalSaleId",
                table: "SaleLines",
                column: "LocalSaleId");

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
                name: "IX_Sales_CashSessionServerId",
                table: "Sales",
                column: "CashSessionServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_ClientOperationId",
                table: "Sales",
                column: "ClientOperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CustomerLocalId",
                table: "Sales",
                column: "CustomerLocalId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CustomerServerId",
                table: "Sales",
                column: "CustomerServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_LocalCashSessionId",
                table: "Sales",
                column: "LocalCashSessionId");

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
                name: "IX_StockMovements_ClientOperationId",
                table: "StockMovements",
                column: "ClientOperationId",
                unique: true);

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
                name: "IX_SyncQueueItems_ClientOperationId",
                table: "SyncQueueItems",
                column: "ClientOperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncQueueItems_EntityName",
                table: "SyncQueueItems",
                column: "EntityName");

            migrationBuilder.CreateIndex(
                name: "IX_SyncQueueItems_LocalEntityId",
                table: "SyncQueueItems",
                column: "LocalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncQueueItems_Status",
                table: "SyncQueueItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SyncTableStates_EntityName",
                table: "SyncTableStates",
                column: "EntityName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashCorrections");

            migrationBuilder.DropTable(
                name: "CashMovements");

            migrationBuilder.DropTable(
                name: "CashReports");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "PurchaseLines");

            migrationBuilder.DropTable(
                name: "PurchasePayments");

            migrationBuilder.DropTable(
                name: "ReturnLines");

            migrationBuilder.DropTable(
                name: "SaleLines");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "Stocks");

            migrationBuilder.DropTable(
                name: "SyncQueueItems");

            migrationBuilder.DropTable(
                name: "SyncTableStates");

            migrationBuilder.DropTable(
                name: "Purchases");

            migrationBuilder.DropTable(
                name: "Returns");

            migrationBuilder.DropTable(
                name: "Sales");

            migrationBuilder.DropTable(
                name: "CashSessions");
        }
    }
}
