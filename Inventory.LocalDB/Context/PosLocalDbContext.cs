using Inventory.LocalDB.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace Inventory.LocalDB.Context
{
    public class PosLocalDbContext : DbContext
    {
        public PosLocalDbContext()
        {
        }

        public PosLocalDbContext(DbContextOptions<PosLocalDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured)
                return;

            var dbFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Inventory"
            );

            Directory.CreateDirectory(dbFolder);

            var dbPath = Path.Combine(dbFolder, "inventory-pos-design-time.db");

            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        public DbSet<LocalProduct> Products => Set<LocalProduct>();
        public DbSet<LocalProductCatalog> ProductCatalogs => Set<LocalProductCatalog>();
        public DbSet<LocalProductCategory> ProductCategories => Set<LocalProductCategory>();
        public DbSet<LocalCustomer> Customers => Set<LocalCustomer>();
        public DbSet<LocalSupplier> Suppliers => Set<LocalSupplier>();
        public DbSet<LocalSale> Sales => Set<LocalSale>();
        public DbSet<LocalSaleLine> SaleLines => Set<LocalSaleLine>();
        public DbSet<LocalPayment> Payments => Set<LocalPayment>();
        public DbSet<LocalCashSession> CashSessions => Set<LocalCashSession>();
        public DbSet<LocalCashMovement> CashMovements => Set<LocalCashMovement>();
        public DbSet<LocalStock> Stocks => Set<LocalStock>();
        public DbSet<LocalStockMovement> StockMovements => Set<LocalStockMovement>();
        public DbSet<SyncQueueItem> SyncQueueItems => Set<SyncQueueItem>();
        public DbSet<SyncTableStateLocal> SyncTableStates => Set<SyncTableStateLocal>();
        public DbSet<LocalPurchase> Purchases => Set<LocalPurchase>();
        public DbSet<LocalPurchaseLine> PurchaseLines => Set<LocalPurchaseLine>();
        public DbSet<LocalPurchasePayment> PurchasePayments => Set<LocalPurchasePayment>();
        public DbSet<LocalReturn> Returns => Set<LocalReturn>();
        public DbSet<LocalReturnLine> ReturnLines => Set<LocalReturnLine>();
        public DbSet<LocalCashCorrection> CashCorrections => Set<LocalCashCorrection>();
        public DbSet<LocalCashReport> CashReports => Set<LocalCashReport>();
        public DbSet<LocalUserSession> UserSessions => Set<LocalUserSession>();
        public DbSet<LocalPackComponent> PackComponents => Set<LocalPackComponent>();
        public DbSet<LocalDamage> Damages => Set<LocalDamage>();
        public DbSet<LocalReceipt> Receipts => Set<LocalReceipt>();
        public DbSet<LocalStoreProfile> StoreProfiles => Set<LocalStoreProfile>();
        public DbSet<LocalReceiptPrintLog> ReceiptPrintLogs => Set<LocalReceiptPrintLog>();
        public DbSet<LocalCustomerTransaction> CustomerTransactions =>
            Set<LocalCustomerTransaction>();

        public DbSet<LocalPurchaseDraftAdjustment> PurchaseDraftAdjustments => 
            Set<LocalPurchaseDraftAdjustment>();

        public DbSet<LocalPurchaseDraft> PurchaseDrafts => Set<LocalPurchaseDraft>();

        public DbSet<LocalPurchaseDraftLine> PurchaseDraftLines =>
            Set<LocalPurchaseDraftLine>();

        public DbSet<LocalInventorySession> InventorySessions =>
    Set<LocalInventorySession>();

        public DbSet<LocalInventoryLine> InventoryLines =>
            Set<LocalInventoryLine>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            ConfigureLocalProduct(builder);
            ConfigureLocalProductCatalog(builder);
            ConfigureLocalProductCategory(builder);
            ConfigureLocalCustomer(builder);
            ConfigureLocalSupplier(builder);
            ConfigureLocalSale(builder);
            ConfigureLocalSaleLine(builder);
            ConfigureLocalPayment(builder);
            ConfigureLocalCashSession(builder);
            ConfigureLocalCashMovement(builder);
            ConfigureLocalStock(builder);
            ConfigureLocalStockMovement(builder);
            ConfigureSyncQueueItem(builder);
            ConfigureSyncTableStateLocal(builder);
            ConfigureLocalPurchase(builder);
            ConfigureLocalPurchaseLine(builder);
            ConfigureLocalPurchasePayment(builder);
            ConfigureLocalReturn(builder);
            ConfigureLocalReturnLine(builder);
            ConfigureLocalCashCorrection(builder);
            ConfigureLocalCashReport(builder);
            ConfigureLocalUserSession(builder);
            ConfigureLocalPackComponent(builder);
            ConfigureLocalDamage(builder);
            ConfigureLocalCustomerTransaction(builder);
            ConfigureLocalReceipt(builder);
            ConfigureLocalReceiptPrintLog(builder);
            ConfigureLocalStoreProfile(builder);
            ConfigureLocalPurchaseDraft(builder);

            ConfigureLocalInventory(builder);
            ConfigureLocalInventoryLine(builder);
        }

        private static void ConfigureLocalInventory(ModelBuilder builder)
        {
            builder.Entity<LocalInventorySession>(
     entity =>
     {
         entity.ToTable(
             "InventorySessions");

         entity.HasKey(
             session =>
                 session.Id);

         entity.Property(
                 session =>
                     session.SessionNumber)
             .HasMaxLength(100)
             .IsRequired();

         entity.Property(
                 session =>
                     session.Status)
             .HasMaxLength(50)
             .IsRequired();

         entity.Property(
                 session =>
                     session.SyncStatus)
             .HasMaxLength(50)
             .IsRequired();

         entity.Property(
                 session =>
                     session.Notes)
             .HasMaxLength(1000);

         entity.HasIndex(
             session =>
                 new
                 {
                     session.TenantId,
                     session.SessionNumber
                 })
             .IsUnique();

         entity.HasIndex(
             session =>
                 new
                 {
                     session.TenantId,
                     session.Status
                 });

         entity.HasMany(
                 session =>
                     session.Lines)
             .WithOne(
                 line =>
                     line.Session)
             .HasForeignKey(
                 line =>
                     line.LocalInventorySessionId)
             .OnDelete(
                 DeleteBehavior.Cascade);
     });
        }

        private static void ConfigureLocalInventoryLine(ModelBuilder builder)
        {
            builder.Entity<LocalInventoryLine>(
               entity =>
               {
                   entity.ToTable(
                       "InventoryLines");

                   entity.HasKey(
                       line =>
                           line.Id);

                   entity.Property(
                           line =>
                               line.ProductName)
                       .HasMaxLength(300)
                       .IsRequired();

                   entity.Property(
                           line =>
                               line.ProductBarcode)
                       .HasMaxLength(100);

                   entity.Property(
                           line =>
                               line.Notes)
                       .HasMaxLength(1000);

                   entity.Property(
                           line =>
                               line.ExpectedQuantity)
                       .HasPrecision(
                           18,
                           3);

                   entity.Property(
                           line =>
                               line.CountedQuantity)
                       .HasPrecision(
                           18,
                           3);

                   entity.HasIndex(
                       line =>
                           new
                           {
                               line.LocalInventorySessionId,
                               line.ProductLocalId
                           })
                       .IsUnique();

                   entity.HasIndex(
                       line =>
                           new
                           {
                               line.TenantId,
                               line.ProductLocalId
                           });
               });

        }
        private static void ConfigureLocalPurchaseDraft(
    ModelBuilder builder)
        {
            var draft =
                builder.Entity<LocalPurchaseDraft>();

            draft.ToTable(
                "PurchaseDrafts");

            draft.HasKey(item =>
                item.Id);

            draft.Property(item =>
                    item.Status)
                .HasConversion<string>()
                .IsRequired();

            draft.Property(item =>
                    item.CreatedAtUtc)
                .IsRequired();

            draft.Property(item =>
                    item.UpdatedAtUtc)
                .IsRequired();

            draft.HasIndex(item =>
                new
                {
                    item.TenantId,
                    item.Status
                });

            draft.HasMany(item =>
                    item.Lines)
                .WithOne(line =>
                    line.PurchaseDraft)
                .HasForeignKey(line =>
                    line.PurchaseDraftId)
                .OnDelete(
                    DeleteBehavior.Cascade);


            var draftLine =
                builder.Entity<LocalPurchaseDraftLine>();

            draftLine.ToTable(
                "PurchaseDraftLines");

            draftLine.HasKey(line =>
                line.Id);

            draftLine.Property(line =>
                    line.Quantity)
                .HasPrecision(18, 3)
                .IsRequired();

            draftLine.Property(line =>
                    line.BasePurchasePrice)
                .HasPrecision(18, 2)
                .IsRequired();

            draftLine.Property(line =>
                    line.EffectiveUnitPrice)
                .HasPrecision(18, 2)
                .IsRequired();

            draftLine.Property(line =>
                    line.VatRate)
                .HasPrecision(5, 2)
                .IsRequired();

            draftLine.Property(line =>
                    line.DisplayOrder)
                .IsRequired();

            draftLine.HasIndex(line =>
                new
                {
                    line.PurchaseDraftId,
                    line.DisplayOrder
                });

            draftLine.HasMany(line =>
                    line.Adjustments)
                .WithOne(adjustment =>
                    adjustment.PurchaseDraftLine)
                .HasForeignKey(adjustment =>
                    adjustment.PurchaseDraftLineId)
                .OnDelete(
                    DeleteBehavior.Cascade);


            var adjustment =
                builder.Entity<LocalPurchaseDraftAdjustment>();

            adjustment.ToTable(
                "PurchaseDraftAdjustments");

            adjustment.HasKey(item =>
                item.Id);

            adjustment.Property(item =>
                    item.Type)
                .HasConversion<string>()
                .IsRequired();

            adjustment.Property(item =>
                    item.Value)
                .HasPrecision(18, 2)
                .IsRequired();

            adjustment.Property(item =>
                    item.DisplayOrder)
                .IsRequired();

            adjustment.HasIndex(item =>
                new
                {
                    item.PurchaseDraftLineId,
                    item.DisplayOrder
                });
        }

        private static void ConfigureLocalStoreProfile(
    ModelBuilder modelBuilder)
        {
            var entity =
                modelBuilder.Entity<LocalStoreProfile>();

            entity.ToTable(
                "StoreProfiles");

            /*
             * Un seul profil magasin par tenant.
             */
            entity.HasKey(
                item =>
                    item.TenantId);

            entity.Property(
                    item =>
                        item.TenantId)
                .ValueGeneratedNever();

            /*
             * Informations générales du magasin.
             */
            entity.Property(
                    item =>
                        item.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(
                    item =>
                        item.LegalName)
                .HasMaxLength(200);

            entity.Property(
                    item =>
                        item.TradeName)
                .HasMaxLength(100);

            entity.Property(
                    item =>
                        item.TaxNumber)
                .HasMaxLength(50);

            entity.Property(
                    item =>
                        item.RegistrationNumber)
                .HasMaxLength(50);

            entity.Property(
                    item =>
                        item.Address)
                .HasMaxLength(500);

            entity.Property(
                    item =>
                        item.City)
                .HasMaxLength(100);

            entity.Property(
                    item =>
                        item.State)
                .HasMaxLength(100);

            entity.Property(
                    item =>
                        item.PostalCode)
                .HasMaxLength(20);

            entity.Property(
                    item =>
                        item.Country)
                .HasMaxLength(100);

            entity.Property(
                    item =>
                        item.Phone)
                .HasMaxLength(50);

            entity.Property(
                    item =>
                        item.Mobile)
                .HasMaxLength(50);

            entity.Property(
                    item =>
                        item.Email)
                .HasMaxLength(100);

            entity.Property(
                    item =>
                        item.Website)
                .HasMaxLength(200);

            entity.Property(
                    item =>
                        item.LogoUrl)
                .HasMaxLength(500);

            /*
             * Textes généraux du ticket.
             */
            entity.Property(
                    item =>
                        item.ReceiptHeader)
                .HasMaxLength(2000);

            entity.Property(
                    item =>
                        item.ReceiptFooter)
                .HasMaxLength(2000);

            /*
             * Paramètres régionaux généraux.
             */
            entity.Property(
                    item =>
                        item.Currency)
                .IsRequired()
                .HasMaxLength(3)
                .HasDefaultValue("EUR");

            entity.Property(
                    item =>
                        item.CurrencySymbol)
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue("€");

            entity.Property(
                    item =>
                        item.Locale)
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue("fr-BE");

            /*
             * Configuration personnalisée du ticket par tenant.
             */
            entity.Property(
                    item =>
                        item.ReceiptCurrencyCode)
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue("EUR");

            entity.Property(
                    item =>
                        item.ReceiptHeaderTagLine)
                .HasMaxLength(200);

            entity.Property(
                    item =>
                        item.ReceiptSocialLine)
                .HasMaxLength(200);

            entity.Property(
                    item =>
                        item.ReceiptExtraAddressLine)
                .HasMaxLength(300);

            entity.Property(
                    item =>
                        item.ReceiptDefaultCashierName)
                .HasMaxLength(100);

            entity.Property(
                    item =>
                        item.ReceiptLogoFileName)
                .HasMaxLength(200);

            entity.Property(
                    item =>
                        item.ReceiptLogoContentType)
                .HasMaxLength(100);

            /*
             * SQLite stocke le logo comme BLOB.
             */
            entity.Property(
                    item =>
                        item.ReceiptLogoBytes)
                .HasColumnType("BLOB");

            entity.Property(
                item =>
                    item.ReceiptConfigurationUpdatedAtUtc);

            entity.Property(
                item =>
                    item.LastSyncedAtUtc);
        }


        private static void ConfigureLocalReceipt(
    ModelBuilder modelBuilder)
        {
            var entity =
                modelBuilder.Entity<LocalReceipt>();

            entity.ToTable(
                "Receipts");

            entity.HasKey(
                receipt =>
                    receipt.Id);

            entity.Property(
                    receipt =>
                        receipt.Id)
                .ValueGeneratedNever();

            entity.Property(
                    receipt =>
                        receipt.InvoiceNumber)
                .IsRequired()
                .HasMaxLength(
                    100);

            entity.Property(
                    receipt =>
                        receipt.SnapshotJson)
                .IsRequired();

            entity.Property(
                    receipt =>
                        receipt.SnapshotHash)
                .HasMaxLength(
                    128);

            entity.Property(
                    receipt =>
                        receipt.SyncStatus)
                .IsRequired()
                .HasMaxLength(
                    30);

            /*
             * Une seule archive de ticket par vente locale.
             */
            entity.HasIndex(
                    receipt =>
                        new
                        {
                            receipt.TenantId,
                            receipt.LocalSaleId
                        })
                .IsUnique();

            entity.HasIndex(
                receipt =>
                    new
                    {
                        receipt.TenantId,
                        receipt.InvoiceNumber
                    });
        }

        private static void ConfigureLocalReceiptPrintLog(
            ModelBuilder modelBuilder)
        {
            var entity =
                modelBuilder.Entity<LocalReceiptPrintLog>();

            entity.ToTable("ReceiptPrintLogs");

            entity.HasKey(log => log.Id);

            entity.Property(log => log.Id).ValueGeneratedNever();

            entity.Property(log => log.PrintType)
                .IsRequired()
                .HasMaxLength(
                    30);

            entity.Property(
                    log =>
                        log.DeviceName)
                .HasMaxLength(
                    150);

            entity.Property(
                    log =>
                        log.Reason)
                .HasMaxLength(
                    250);

            entity.Property(
                    log =>
                        log.ErrorMessage)
                .HasMaxLength(
                    1000);

            entity.HasIndex(
                log =>
                    new
                    {
                        log.TenantId,
                        log.LocalReceiptId,
                        log.PrintedAtUtc
                    });
        }

        private static void ConfigureLocalCustomerTransaction(ModelBuilder builder)
        {
            builder.Entity<LocalCustomerTransaction>(entity =>
            {
                entity.HasKey(item => item.Id);

                entity.HasIndex(item => item.TenantId);

                entity.HasIndex(item => new
                {
                    item.TenantId,
                    item.ServerId
                })
                .IsUnique()
                .HasFilter("ServerId IS NOT NULL");

                entity.HasIndex(item => new
                {
                    item.TenantId,
                    item.ClientOperationId
                })
                .IsUnique();

                entity.HasIndex(item => new
                {
                    item.TenantId,
                    item.CustomerLocalId,
                    item.TransactionDateUtc
                });

                entity.HasIndex(item => new
                {
                    item.TenantId,
                    item.SyncStatus
                });

                entity.HasIndex(item => new
                {
                    item.TenantId,
                    item.Origin,
                    item.SaleLocalId
                });

                entity.Property(item => item.Type)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(item => item.Origin)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(item => item.SyncStatus)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(item => item.Description)
                    .HasMaxLength(500);

                entity.Property(item => item.Amount)
                    .HasPrecision(18, 2);

                entity.Property(item => item.BalanceBefore)
                    .HasPrecision(18, 2);

                entity.Property(item => item.BalanceAfter)
                    .HasPrecision(18, 2);

                entity.HasOne(item => item.Customer)
                    .WithMany()
                    .HasForeignKey(item => item.CustomerLocalId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }


        private static void ConfigureLocalDamage(
    ModelBuilder modelBuilder)
        {
            var entity =
                modelBuilder.Entity<LocalDamage>();

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Quantity)
                .HasPrecision(18, 3);

            entity.Property(x => x.EstimatedValue)
                .HasPrecision(18, 2);

            entity.HasIndex(x => new
            {
                x.TenantId,
                x.ServerId
            })
            .IsUnique()
            .HasFilter("ServerId IS NOT NULL");

            entity.HasIndex(x => new
            {
                x.TenantId,
                x.DamageNumber
            })
            .IsUnique();

            entity.HasIndex(x => new
            {
                x.TenantId,
                x.LocalStatus
            });

            entity.HasIndex(x => new
            {
                x.TenantId,
                x.ProductLocalId
            });

            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductLocalId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private static void ConfigureLocalProduct(
     ModelBuilder builder)
        {
            builder.Entity<LocalProduct>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.TenantId);

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ServerId
                })
                .IsUnique()
                .HasFilter("ServerId IS NOT NULL");

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.CatalogProductId
                })
                .IsUnique()
                .HasFilter(
                    "CatalogProductId IS NOT NULL " +
                    "AND IsDeletedLocally = 0");

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.IsDeletedLocally
                });

                entity.HasIndex(x => x.Barcode);
                entity.HasIndex(x => x.Name);
                entity.HasIndex(x => x.UnitProductLocalId);
                entity.HasIndex(x => x.UnitProductServerId);
                entity.HasIndex(x => x.SyncStatus);

                entity.Property(x => x.Name)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Sku)
                    .HasMaxLength(100);

                entity.Property(x => x.Barcode)
                    .HasMaxLength(100);

                entity.Property(x => x.Category)
                    .HasMaxLength(100);

                entity.Property(x => x.Brand)
                    .HasMaxLength(100);

                entity.Property(x => x.Unit)
                    .HasMaxLength(50);

                entity.Property(x => x.SalePrice)
                    .HasPrecision(18, 2);

                entity.Property(x => x.SalePrice2)
                    .HasPrecision(18, 2);

                entity.Property(x => x.SalePrice3)
                    .HasPrecision(18, 2);

                entity.Property(x => x.PurchasePrice)
                    .HasPrecision(18, 2);

                entity.Property(x => x.VatRate)
                    .HasPrecision(5, 2);

                entity.Property(x => x.MinStockLevel)
                    .HasPrecision(18, 3);

                entity.Property(x => x.MaxStockLevel)
                    .HasPrecision(18, 3);

                entity.Property(x => x.LocalStockQuantity)
                    .HasPrecision(18, 3);

                entity.Property(x => x.UnitsPerPack)
                    .HasPrecision(18, 3);

                entity.Property(x => x.SyncStatus)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .HasConversion<string>();

                entity.HasOne<LocalProductCatalog>()
                    .WithMany()
                    .HasForeignKey(x => x.CatalogProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureLocalProductCatalog(ModelBuilder builder)
        {
            builder.Entity<LocalProductCatalog>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.Barcode);
                entity.HasIndex(x => x.InternalCode);
                entity.HasIndex(x => x.Name);
                entity.HasIndex(x => x.CategoryId);
                entity.HasIndex(x => x.IsDeleted);

                entity.Property(x => x.Barcode)
                    .HasMaxLength(100);

                entity.Property(x => x.InternalCode)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Name)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Brand)
                    .HasMaxLength(100);

                entity.Property(x => x.Manufacturer)
                    .HasMaxLength(100);

                entity.Property(x => x.Description)
                    .HasMaxLength(1000);

                entity.Property(x => x.UnitOfMeasure)
                    .HasMaxLength(20)
                    .IsRequired();
            });
        }

        private static void ConfigureLocalProductCategory(ModelBuilder builder)
        {
            builder.Entity<LocalProductCategory>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.Name);
                entity.HasIndex(x => x.DisplayOrder);
                entity.HasIndex(x => x.IsDeleted);
                entity.HasIndex(x => x.LastSyncedAtUtc);

                entity.Property(x => x.Name)
                    .HasMaxLength(100)
                    .UseCollation("NOCASE")
                    .IsRequired();

                entity.Property(x => x.Color)
                    .HasMaxLength(50);

                entity.Property(x => x.Icon)
                    .HasMaxLength(100);
            });
        }

        private static void ConfigureLocalCustomer(ModelBuilder builder)
        {
            builder.Entity<LocalCustomer>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.TenantId);
                entity.HasIndex(x => x.ServerId);
                entity.HasIndex(x => x.Name);
                entity.HasIndex(x => x.Email);
                entity.HasIndex(x => x.IsDeleted);
                entity.HasIndex(x => x.SyncStatus);

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ServerId
                }).IsUnique()
                .HasFilter("ServerId IS NOT NULL");

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.IsDeleted
                });

                entity.Property(x => x.Name)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Email)
                    .HasMaxLength(200);

                entity.Property(x => x.Phone)
                    .HasMaxLength(50);

                entity.Property(x => x.Address)
                    .HasMaxLength(500);

                entity.Property(x => x.TaxNumber)
                    .HasMaxLength(100);

                entity.Property(x => x.Notes)
                    .HasMaxLength(1000);

                entity.Property(x => x.SyncStatus)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.CreditLimit)
                    .HasColumnType("decimal(18,2)");

                entity.Property(x => x.CurrentBalance)
                    .HasColumnType("decimal(18,2)");
            });
        }

        private static void ConfigureLocalSupplier(ModelBuilder builder)
        {
            builder.Entity<LocalSupplier>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.TenantId);

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ServerId
                })
                .IsUnique()
                .HasFilter("ServerId IS NOT NULL");

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.Name
                });

                entity.HasIndex(x => x.Email);
                entity.HasIndex(x => x.Phone);

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.IsDeleted
                });

                entity.HasIndex(x => x.SyncStatus);

                entity.Property(x => x.Name)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.ContactPerson)
                    .HasMaxLength(100);

                entity.Property(x => x.Email)
                    .HasMaxLength(100);

                entity.Property(x => x.Phone)
                    .HasMaxLength(50);

                entity.Property(x => x.Address)
                    .HasMaxLength(500);

                entity.Property(x => x.City)
                    .HasMaxLength(100);

                entity.Property(x => x.PostalCode)
                    .HasMaxLength(20);

                entity.Property(x => x.Country)
                    .HasMaxLength(100);

                entity.Property(x => x.TaxNumber)
                    .HasMaxLength(50);

                entity.Property(x => x.BankAccount)
                    .HasMaxLength(50);

                entity.Property(x => x.Notes)
                    .HasMaxLength(1000);

                entity.Property(x => x.SyncStatus)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.CurrentBalance)
                    .HasColumnType("decimal(18,2)");
            });
        }

        private static void ConfigureLocalSale(
     ModelBuilder builder)
        {
            builder.Entity<LocalSale>(entity =>
            {
                entity.ToTable("Sales");

                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.TenantId);

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ServerId
                })
                .IsUnique()
                .HasFilter(
                    "\"ServerId\" IS NOT NULL");

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.LocalInvoiceNumber
                })
                .IsUnique();

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ReceiptBarcodeValue
                })
                .IsUnique()
                .HasFilter(
                    "\"ReceiptBarcodeValue\" IS NOT NULL " +
                    "AND trim(\"ReceiptBarcodeValue\") <> ''");

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.SyncStatus,
                    x.SaleDateUtc
                });

                entity.HasIndex(x => x.ClientOperationId)
                    .IsUnique();

                entity.HasIndex(x => x.LocalCashSessionId);

                entity.HasIndex(x => x.CashSessionServerId);

                entity.HasIndex(x => x.CustomerLocalId);

                entity.HasIndex(x => x.CustomerServerId);

                entity.Property(x => x.LocalInvoiceNumber)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.ReceiptBarcodeValue)
                    .HasMaxLength(32)
                    .IsRequired(false);

                entity.Property(x => x.ServerInvoiceNumber)
                    .HasMaxLength(100);

                entity.Property(x => x.Status)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.PaymentStatus)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.SyncStatus)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Notes)
                    .HasMaxLength(1000);

                entity.Property(x => x.SubtotalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.DiscountAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.VatAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.TotalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.PaidAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.ChangeAmount)
                    .HasPrecision(18, 2);

                entity.HasOne(x => x.LocalCashSession)
                    .WithMany(x => x.Sales)
                    .HasForeignKey(x => x.LocalCashSessionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureLocalSaleLine(
    ModelBuilder builder)
        {
            builder.Entity<LocalSaleLine>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id).ValueGeneratedNever();

                entity.HasIndex(x => x.TenantId);

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.LocalSaleId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ProductLocalId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ProductServerId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.UnitProductServerId
                });

                entity.Property(x => x.ProductName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.ProductBarcode)
                    .HasMaxLength(100);

                entity.Property(x => x.Notes)
                    .HasMaxLength(500);

                entity.Property(x => x.Quantity)
                    .HasPrecision(18, 3);

                entity.Property(x => x.UnitQuantity)
                    .HasPrecision(18, 3);

                entity.Property(x => x.UnitsPerPack)
                    .HasPrecision(18, 3);

                entity.Property(x => x.UnitPrice)
                    .HasPrecision(18, 2);

                entity.Property(x => x.VatRate)
                    .HasPrecision(5, 2);

                entity.Property(x => x.DiscountPercent)
                    .HasPrecision(5, 2);

                entity.Property(x => x.DiscountAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.UnitCostPrice)
                    .HasPrecision(18, 2);

                entity.HasOne(x => x.LocalSale)
                    .WithMany(x => x.Lines)
                    .HasForeignKey(x => x.LocalSaleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureLocalPayment(
    ModelBuilder builder)
        {
            builder.Entity<LocalPayment>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id).ValueGeneratedNever();

                entity.HasIndex(x => x.TenantId);

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ServerId
                })
                .IsUnique()
                .HasFilter("ServerId IS NOT NULL");

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.LocalSaleId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ServerSaleId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.Method
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.SyncStatus
                });

                entity.Property(x => x.Method)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.TransactionRef)
                    .HasMaxLength(200);

                entity.Property(x => x.CardLastFourDigits)
                    .HasMaxLength(100);

                entity.Property(x => x.Notes)
                    .HasMaxLength(500);

                entity.Property(x => x.SyncStatus)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Amount)
                    .HasPrecision(18, 2);

                entity.HasOne(x => x.LocalSale)
                    .WithMany(x => x.Payments)
                    .HasForeignKey(x => x.LocalSaleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureLocalCashSession(
    ModelBuilder builder)
        {
            builder.Entity<LocalCashSession>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.TenantId);

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ServerId
                })
                .IsUnique()
                .HasFilter("ServerId IS NOT NULL");

                entity.HasIndex(x => x.ClientOperationId)
                    .IsUnique();

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.SessionNumber
                })
                .IsUnique();

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.Status
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.SyncStatus
                });

                entity.Property(x => x.SessionNumber)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.SyncStatus)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.OpeningNotes)
                    .HasMaxLength(1000);

                entity.Property(x => x.ClosingNotes)
                    .HasMaxLength(1000);

                entity.Property(x => x.OpeningAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.ClosingAmountExpected)
                    .HasPrecision(18, 2);

                entity.Property(x => x.ClosingAmountCounted)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Difference)
                    .HasPrecision(18, 2);
            });
        }

        private static void ConfigureLocalCashMovement(
    ModelBuilder builder)
        {
            builder.Entity<LocalCashMovement>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.TenantId);

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ServerId
                })
                .IsUnique()
                .HasFilter("ServerId IS NOT NULL");

                entity.HasIndex(x => x.ClientOperationId)
                    .IsUnique();

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.LocalCashSessionId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.Type
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.SyncStatus
                });

                entity.Property(x => x.Type)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.ReferenceNumber)
                    .HasMaxLength(200);

                entity.Property(x => x.Notes)
                    .HasMaxLength(500);

                entity.Property(x => x.SyncStatus)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Amount)
                    .HasPrecision(18, 2);

                entity.HasOne(x => x.LocalCashSession)
                    .WithMany(x => x.CashMovements)
                    .HasForeignKey(x => x.LocalCashSessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureLocalStock(
    ModelBuilder modelBuilder)
        {
            var entity =
                modelBuilder.Entity<LocalStock>();

            entity.HasKey(stock =>
                stock.Id);

            entity.Property(stock =>
                    stock.ProductLocalId)
                .IsRequired();

            entity.Property(stock =>
                    stock.Quantity)
                .HasPrecision(18, 3);

            entity.Property(stock =>
                    stock.ReservedQuantity)
                .HasPrecision(18, 3);

            entity.HasIndex(stock => new
            {
                stock.TenantId,
                stock.ServerId
            })
            .IsUnique()
            .HasFilter("ServerId IS NOT NULL");

            entity.HasIndex(stock => new
            {
                stock.TenantId,
                stock.ProductLocalId
            })
            .IsUnique();

            entity.HasIndex(stock => new
            {
                stock.TenantId,
                stock.ProductServerId
            });
        }

        private static void ConfigureLocalStockMovement(
    ModelBuilder builder)
        {
            builder.Entity<LocalStockMovement>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.TenantId);

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ServerId
                })
                .IsUnique()
                .HasFilter("ServerId IS NOT NULL");

                entity.HasIndex(x => x.ClientOperationId)
                    .IsUnique();

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ProductLocalId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ProductServerId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.Type
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.SyncStatus
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.LocalReferenceId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ServerReferenceId
                });

                entity.Property(x => x.ProductName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.ProductBarcode)
                    .HasMaxLength(100);

                entity.Property(x => x.Type)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.ReferenceNumber)
                    .HasMaxLength(200);

                entity.Property(x => x.Notes)
                    .HasMaxLength(500);

                entity.Property(x => x.SyncStatus)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.QuantityChange)
                    .HasPrecision(18, 3);

                entity.Property(x => x.QuantityBefore)
                    .HasPrecision(18, 3);

                entity.Property(x => x.QuantityAfter)
                    .HasPrecision(18, 3);

                entity.Property(x => x.UnitCost)
                    .HasPrecision(18, 2);
            });
        }

        private static void ConfigureSyncQueueItem(
    ModelBuilder builder)
        {
            builder.Entity<SyncQueueItem>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.TenantId);

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.Status,
                    x.CreatedAtUtc
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.EntityName,
                    x.LocalEntityId
                });

                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => x.EntityName);
                entity.HasIndex(x => x.LocalEntityId);

                entity.HasIndex(x => x.ClientOperationId)
                    .IsUnique();

                entity.Property(x => x.EntityName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Operation)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.ErrorMessage)
                    .HasMaxLength(2000);

                entity.Property(x => x.PayloadJson)
                    .IsRequired(false);
            });
        }

        private static void ConfigureSyncTableStateLocal(
    ModelBuilder builder)
        {
            builder.Entity<SyncTableStateLocal>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.EntityName
                })
                .IsUnique();

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.InitialSyncCompleted
                });

                entity.Property(x => x.EntityName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Syncmode)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.ContinuationToken)
                    .HasMaxLength(1000);

                entity.Property(x => x.LastError)
                    .HasMaxLength(2000);
            });
        }

        private static void ConfigureLocalPurchase(
     ModelBuilder builder)
        {
            builder.Entity<LocalPurchase>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.TenantId);

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ServerId
                })
                .IsUnique()
                .HasFilter("ServerId IS NOT NULL");

                entity.HasIndex(x => x.ClientOperationId)
                    .IsUnique();

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.LocalPurchaseNumber
                })
                .IsUnique();

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ServerPurchaseNumber
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.SupplierLocalId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.SupplierServerId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.Status
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.SyncStatus,
                    x.PurchaseDateUtc
                });

                entity.Property(x => x.LocalPurchaseNumber)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.ServerPurchaseNumber)
                    .HasMaxLength(100);

                entity.Property(x => x.SupplierInvoiceNumber)
                    .HasMaxLength(100);

                entity.Property(x => x.Status)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.SyncStatus)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Notes)
                    .HasMaxLength(1000);

                entity.Property(x => x.TotalAmountExclVat)
                    .HasPrecision(18, 2);

                entity.Property(x => x.TotalVatAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.TotalAmountInclVat)
                    .HasPrecision(18, 2);
            });
        }
        private static void ConfigureLocalPurchaseLine(
    ModelBuilder builder)
        {
            builder.Entity<LocalPurchaseLine>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.TenantId);

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.LocalPurchaseId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ProductLocalId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ProductServerId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ProductBarcode
                });

                entity.Property(x => x.ProductName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.ProductBarcode)
                    .HasMaxLength(100);

                entity.Property(x => x.QuantityOrdered)
                    .HasPrecision(18, 3);

                entity.Property(x => x.QuantityReceived)
                    .HasPrecision(18, 3);

                entity.Property(x => x.UnitPurchasePrice)
                    .HasPrecision(18, 2);

                entity.Property(x => x.VatRate)
                    .HasPrecision(5, 2);

                entity.HasOne(x => x.LocalPurchase)
                    .WithMany(x => x.Lines)
                    .HasForeignKey(x => x.LocalPurchaseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureLocalPurchasePayment(
    ModelBuilder builder)
        {
            builder.Entity<LocalPurchasePayment>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.TenantId);

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ServerId
                })
                .IsUnique()
                .HasFilter("ServerId IS NOT NULL");

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.LocalPurchaseId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ServerPurchaseId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.Method
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.SyncStatus,
                    x.PaymentDateUtc
                });

                entity.Property(x => x.Method)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.TransactionRef)
                    .HasMaxLength(200);

                entity.Property(x => x.Notes)
                    .HasMaxLength(500);

                entity.Property(x => x.SyncStatus)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Amount)
                    .HasPrecision(18, 2);

                entity.HasOne(x => x.LocalPurchase)
                    .WithMany(x => x.Payments)
                    .HasForeignKey(x => x.LocalPurchaseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        // Replace only ConfigureLocalReturn and ConfigureLocalReturnLine
        // in PosLocalDbContext with these methods.

        private static void ConfigureLocalReturn(
            ModelBuilder builder)
        {
            builder.Entity<LocalReturn>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.TenantId);

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ServerId
                })
                .IsUnique()
                .HasFilter("ServerId IS NOT NULL");

                entity.HasIndex(x => x.ClientOperationId)
                    .IsUnique();

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.LocalReturnNumber
                })
                .IsUnique();

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ServerReturnNumber
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.LocalSaleId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ServerSaleId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.LocalCashSessionId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.CustomerLocalId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.RefundMethod
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.SyncStatus,
                    x.ReturnDateUtc
                });

                entity.Property(x => x.LocalReturnNumber)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.ServerReturnNumber)
                    .HasMaxLength(100);

                entity.Property(x => x.OriginalLocalInvoiceNumber)
                    .HasMaxLength(100);

                entity.Property(x => x.OriginalServerInvoiceNumber)
                    .HasMaxLength(100);

                entity.Property(x => x.RefundMethod)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Reason)
                    .HasMaxLength(1000);

                entity.Property(x => x.SyncStatus)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.TotalAmount)
                    .HasPrecision(18, 2);
            });
        }

        private static void ConfigureLocalReturnLine(
            ModelBuilder builder)
        {
            builder.Entity<LocalReturnLine>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.TenantId);

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.LocalReturnId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.LocalSaleLineId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ProductLocalId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ProductServerId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.UnitProductLocalId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.UnitProductServerId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ProductBarcode
                });

                entity.Property(x => x.ProductName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.ProductBarcode)
                    .HasMaxLength(100);

                entity.Property(x => x.Reason)
                    .HasMaxLength(500);

                entity.Property(x => x.Quantity)
                    .HasPrecision(18, 3);

                entity.Property(x => x.UnitQuantity)
                    .HasPrecision(18, 3);

                entity.Property(x => x.UnitsPerPack)
                    .HasPrecision(18, 3);

                entity.Property(x => x.UnitPrice)
                    .HasPrecision(18, 2);

                entity.Property(x => x.VatRate)
                    .HasPrecision(5, 2);

                entity.Property(x => x.UnitCostPrice)
                    .HasPrecision(18, 2);

                entity.HasOne(x => x.LocalReturn)
                    .WithMany(x => x.Lines)
                    .HasForeignKey(x => x.LocalReturnId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }


        private static void ConfigureLocalCashCorrection(
     ModelBuilder builder)
        {
            builder.Entity<LocalCashCorrection>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.TenantId);

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ServerId
                })
                .IsUnique()
                .HasFilter("ServerId IS NOT NULL");

                entity.HasIndex(x => x.ClientOperationId)
                    .IsUnique();

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.OriginalLocalCashSessionId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.OriginalServerCashSessionId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.SyncStatus,
                    x.CorrectedAtUtc
                });

                entity.Property(x => x.Reason)
                    .HasMaxLength(1000)
                    .IsRequired();

                entity.Property(x => x.ApprovalNotes)
                    .HasMaxLength(500);

                entity.Property(x => x.SyncStatus)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Amount)
                    .HasPrecision(18, 2);

                entity.HasOne(x => x.OriginalLocalCashSession)
                    .WithMany()
                    .HasForeignKey(x => x.OriginalLocalCashSessionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureLocalCashReport(
    ModelBuilder builder)
        {
            builder.Entity<LocalCashReport>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.TenantId);

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ServerId
                })
                .IsUnique()
                .HasFilter("ServerId IS NOT NULL");

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.LocalCashSessionId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ServerCashSessionId
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.Type
                });

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.SyncStatus,
                    x.GeneratedAtUtc
                });

                entity.Property(x => x.Type)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Notes)
                    .HasMaxLength(2000);

                entity.Property(x => x.SyncStatus)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.ExpectedAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.CountedAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Difference)
                    .HasPrecision(18, 2);

                entity.Property(x => x.CashSales)
                    .HasPrecision(18, 2);

                entity.Property(x => x.CardSales)
                    .HasPrecision(18, 2);

                entity.Property(x => x.OtherPayments)
                    .HasPrecision(18, 2);

                entity.HasOne(x => x.LocalCashSession)
                    .WithMany()
                    .HasForeignKey(x => x.LocalCashSessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureLocalUserSession(ModelBuilder builder)
        {
            builder.Entity<LocalUserSession>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.UserId)
                    .IsUnique();

                entity.HasIndex(x => x.Email);
                entity.HasIndex(x => x.TenantId);
                entity.HasIndex(x => x.IsActive);

                entity.Property(x => x.Email)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.FullName)
                    .HasMaxLength(200);

                entity.Property(x => x.Role)
                    .HasMaxLength(100)
                    .IsRequired();
            });
        }

        private static void ConfigureLocalPackComponent(ModelBuilder builder)
        {
            builder.Entity<LocalPackComponent>(entity =>
            {
                entity.HasKey(x => new
                {
                    x.ProductCatalogId,
                    x.ComponentCatalogId
                });

                entity.HasIndex(x => x.ComponentCatalogId);

                entity.Property(x => x.ComponentName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Quantity)
                    .HasPrecision(18, 3);

                entity.HasOne(x => x.ProductCatalog)
                    .WithMany(x => x.PackComponents)
                    .HasForeignKey(x => x.ProductCatalogId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}