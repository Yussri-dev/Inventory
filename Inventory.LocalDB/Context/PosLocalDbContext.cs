using Inventory.LocalDB.Models;
using Microsoft.EntityFrameworkCore;

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
        public DbSet<LocalCustomer> Customers => Set<LocalCustomer>();
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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            ConfigureLocalProduct(builder);
            ConfigureLocalCustomer(builder);
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
        }

        private static void ConfigureLocalProduct(ModelBuilder builder)
        {
            builder.Entity<LocalProduct>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.ServerId)
                    .IsUnique();

                entity.HasIndex(x => x.Barcode);
                entity.HasIndex(x => x.Name);
                entity.HasIndex(x => x.UnitProductServerId);

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

                entity.Property(x => x.LocalStockQuantity)
                    .HasPrecision(18, 3);

                entity.Property(x => x.UnitsPerPack)
                    .HasPrecision(18, 3);
            });
        }

        private static void ConfigureLocalCustomer(ModelBuilder builder)
        {
            builder.Entity<LocalCustomer>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.ServerId);
                entity.HasIndex(x => x.Name);
                entity.HasIndex(x => x.Phone);

                entity.Property(x => x.Name)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Email)
                    .HasMaxLength(100);

                entity.Property(x => x.Phone)
                    .HasMaxLength(50);

                entity.Property(x => x.Address)
                    .HasMaxLength(500);

                entity.Property(x => x.TaxNumber)
                    .HasMaxLength(50);

                entity.Property(x => x.Notes)
                    .HasMaxLength(1000);

                entity.Property(x => x.CreditLimit)
                    .HasPrecision(18, 2);

                entity.Property(x => x.CurrentBalance)
                    .HasPrecision(18, 2);
            });
        }

        private static void ConfigureLocalSale(ModelBuilder builder)
        {
            builder.Entity<LocalSale>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.ServerId);

                entity.HasIndex(x => x.ClientOperationId)
                    .IsUnique();

                entity.HasIndex(x => x.LocalInvoiceNumber)
                    .IsUnique();

                entity.HasIndex(x => x.SyncStatus);
                entity.HasIndex(x => x.SaleDateUtc);
                entity.HasIndex(x => x.LocalCashSessionId);
                entity.HasIndex(x => x.CashSessionServerId);
                entity.HasIndex(x => x.CustomerLocalId);
                entity.HasIndex(x => x.CustomerServerId);

                entity.Property(x => x.LocalInvoiceNumber)
                    .HasMaxLength(100)
                    .IsRequired();

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

        private static void ConfigureLocalSaleLine(ModelBuilder builder)
        {
            builder.Entity<LocalSaleLine>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.LocalSaleId);
                entity.HasIndex(x => x.ProductLocalId);
                entity.HasIndex(x => x.ProductServerId);
                entity.HasIndex(x => x.UnitProductServerId);

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

        private static void ConfigureLocalPayment(ModelBuilder builder)
        {
            builder.Entity<LocalPayment>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.LocalSaleId);
                entity.HasIndex(x => x.ServerId);
                entity.HasIndex(x => x.ServerSaleId);
                entity.HasIndex(x => x.Method);
                entity.HasIndex(x => x.SyncStatus);

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

        private static void ConfigureLocalCashSession(ModelBuilder builder)
        {
            builder.Entity<LocalCashSession>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.ServerId);

                entity.HasIndex(x => x.ClientOperationId)
                    .IsUnique();

                entity.HasIndex(x => x.SessionNumber)
                    .IsUnique();

                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => x.SyncStatus);

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

        private static void ConfigureLocalCashMovement(ModelBuilder builder)
        {
            builder.Entity<LocalCashMovement>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.ServerId);

                entity.HasIndex(x => x.ClientOperationId)
                    .IsUnique();

                entity.HasIndex(x => x.LocalCashSessionId);
                entity.HasIndex(x => x.Type);
                entity.HasIndex(x => x.SyncStatus);

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

        private static void ConfigureLocalStock(ModelBuilder builder)
        {
            builder.Entity<LocalStock>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.ServerId);

                entity.HasIndex(x => x.ProductLocalId)
                    .IsUnique();

                entity.HasIndex(x => x.ProductServerId)
                    .IsUnique();

                entity.HasIndex(x => x.ProductBarcode);

                entity.Property(x => x.ProductName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.ProductBarcode)
                    .HasMaxLength(100);

                entity.Property(x => x.Quantity)
                    .HasPrecision(18, 3);

                entity.Property(x => x.ReservedQuantity)
                    .HasPrecision(18, 3);
            });
        }

        private static void ConfigureLocalStockMovement(ModelBuilder builder)
        {
            builder.Entity<LocalStockMovement>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.ServerId);

                entity.HasIndex(x => x.ClientOperationId)
                    .IsUnique();

                entity.HasIndex(x => x.ProductLocalId);
                entity.HasIndex(x => x.ProductServerId);
                entity.HasIndex(x => x.Type);
                entity.HasIndex(x => x.SyncStatus);
                entity.HasIndex(x => x.LocalReferenceId);
                entity.HasIndex(x => x.ServerReferenceId);

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

        private static void ConfigureSyncQueueItem(ModelBuilder builder)
        {
            builder.Entity<SyncQueueItem>(entity =>
            {
                entity.HasKey(x => x.Id);

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

                entity.Property(x => x.PayloadJson)
                    .IsRequired();
            });
        }

        private static void ConfigureSyncTableStateLocal(ModelBuilder builder)
        {
            builder.Entity<SyncTableStateLocal>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.EntityName)
                    .IsUnique();

                entity.Property(x => x.EntityName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Syncmode)
                    .HasMaxLength(50)
                    .IsRequired();
            });
        }
        private static void ConfigureLocalPurchase(ModelBuilder builder)
        {
            builder.Entity<LocalPurchase>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.ServerId);

                entity.HasIndex(x => x.ClientOperationId)
                    .IsUnique();

                entity.HasIndex(x => x.LocalPurchaseNumber)
                    .IsUnique();

                entity.HasIndex(x => x.ServerPurchaseNumber);
                entity.HasIndex(x => x.SupplierLocalId);
                entity.HasIndex(x => x.SupplierServerId);
                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => x.SyncStatus);
                entity.HasIndex(x => x.PurchaseDateUtc);

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

        private static void ConfigureLocalPurchaseLine(ModelBuilder builder)
        {
            builder.Entity<LocalPurchaseLine>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.LocalPurchaseId);
                entity.HasIndex(x => x.ProductLocalId);
                entity.HasIndex(x => x.ProductServerId);
                entity.HasIndex(x => x.ProductBarcode);

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

        private static void ConfigureLocalPurchasePayment(ModelBuilder builder)
        {
            builder.Entity<LocalPurchasePayment>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.ServerId);
                entity.HasIndex(x => x.LocalPurchaseId);
                entity.HasIndex(x => x.ServerPurchaseId);
                entity.HasIndex(x => x.Method);
                entity.HasIndex(x => x.SyncStatus);
                entity.HasIndex(x => x.PaymentDateUtc);

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

        private static void ConfigureLocalReturn(ModelBuilder builder)
        {
            builder.Entity<LocalReturn>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.ServerId);

                entity.HasIndex(x => x.ClientOperationId)
                    .IsUnique();

                entity.HasIndex(x => x.LocalReturnNumber)
                    .IsUnique();

                entity.HasIndex(x => x.ServerReturnNumber);
                entity.HasIndex(x => x.LocalSaleId);
                entity.HasIndex(x => x.ServerSaleId);
                entity.HasIndex(x => x.RefundMethod);
                entity.HasIndex(x => x.SyncStatus);
                entity.HasIndex(x => x.ReturnDateUtc);

                entity.Property(x => x.LocalReturnNumber)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.ServerReturnNumber)
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

        private static void ConfigureLocalReturnLine(ModelBuilder builder)
        {
            builder.Entity<LocalReturnLine>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.LocalReturnId);
                entity.HasIndex(x => x.ProductLocalId);
                entity.HasIndex(x => x.ProductServerId);
                entity.HasIndex(x => x.ProductBarcode);

                entity.Property(x => x.ProductName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.ProductBarcode)
                    .HasMaxLength(100);

                entity.Property(x => x.Reason)
                    .HasMaxLength(500);

                entity.Property(x => x.Quantity)
                    .HasPrecision(18, 3);

                entity.Property(x => x.UnitPrice)
                    .HasPrecision(18, 2);

                entity.Property(x => x.VatRate)
                    .HasPrecision(5, 2);

                entity.HasOne(x => x.LocalReturn)
                    .WithMany(x => x.Lines)
                    .HasForeignKey(x => x.LocalReturnId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureLocalCashCorrection(ModelBuilder builder)
        {
            builder.Entity<LocalCashCorrection>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.ServerId);

                entity.HasIndex(x => x.ClientOperationId)
                    .IsUnique();

                entity.HasIndex(x => x.OriginalLocalCashSessionId);
                entity.HasIndex(x => x.OriginalServerCashSessionId);
                entity.HasIndex(x => x.SyncStatus);
                entity.HasIndex(x => x.CorrectedAtUtc);

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

        private static void ConfigureLocalCashReport(ModelBuilder builder)
        {
            builder.Entity<LocalCashReport>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.ServerId);
                entity.HasIndex(x => x.LocalCashSessionId);
                entity.HasIndex(x => x.ServerCashSessionId);
                entity.HasIndex(x => x.Type);
                entity.HasIndex(x => x.SyncStatus);
                entity.HasIndex(x => x.GeneratedAtUtc);

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
    }
}