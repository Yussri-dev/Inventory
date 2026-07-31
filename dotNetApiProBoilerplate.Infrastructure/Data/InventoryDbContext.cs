using Inventory.Domain;
using Inventory.Domain.Entities;
using Inventory.Domain.Models;
using Inventory.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace Inventory.Infrastructure.Data
{
    public class InventoryDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            builder.Entity<ApplicationUser>(
     entity =>
     {
         entity.HasOne(user =>
                 user.Tenant)
             .WithMany(tenant =>
                 tenant.Users)
             .HasForeignKey(user =>
                 user.TenantId)
             .IsRequired(false)
             .OnDelete(
                 DeleteBehavior.Restrict);

         entity.HasIndex(user =>
             user.TenantId);
     });

            // ============================
            // INDEXES FOR PERFORMANCE
            // ============================

            // Add indexes for TenantId on all tenant entities
            builder.Entity<Product>().HasIndex(e => e.TenantId);
            builder.Entity<Customer>().HasIndex(e => e.TenantId);
            builder.Entity<Supplier>().HasIndex(e => e.TenantId);
            builder.Entity<Sale>().HasIndex(e => e.TenantId);
            builder.Entity<Sale>()
            .HasIndex(sale =>
                new
                {
                    sale.TenantId,
                    sale.ClientOperationId
                })
            .IsUnique();
            builder.Entity<Purchase>()
            .HasIndex(purchase => new
            {
                purchase.TenantId,
                purchase.ClientOperationId
            })
            .IsUnique();

            builder.Entity<Stock>().HasIndex(e => e.TenantId);
            builder.Entity<StockMovement>().HasIndex(e => e.TenantId);
            builder.Entity<CashSession>().HasIndex(e => e.TenantId);
            builder.Entity<Payment>().HasIndex(e => e.TenantId);

            builder.Entity<Return>(entity =>
            {
                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.ClientOperationId
                })
                .IsUnique();

                entity.HasIndex(x => new
                {
                    x.TenantId,
                    x.CashSessionId
                });

                entity.HasOne(x => x.Sale)
                    .WithMany()
                    .HasForeignKey(x => x.SaleId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.CashSession)
                    .WithMany()
                    .HasForeignKey(x => x.CashSessionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(x => x.ReturnNumber)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.TotalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Reason)
                    .HasMaxLength(1000);

                entity.Property(x => x.RefundMethod)
                    .HasConversion<string>()
                    .HasMaxLength(50);
            });


            builder.Entity<SupplierReturn>().HasIndex(e => e.TenantId);
            builder.Entity<Damage>().HasIndex(e => e.TenantId);
            builder.Entity<InventorySession>().HasIndex(e => e.TenantId);
            builder.Entity<LoyaltyCard>().HasIndex(e => e.TenantId);
            builder.Entity<DocumentNumber>().HasIndex(e => e.TenantId);
            builder.Entity<AuditLog>().HasIndex(e => e.TenantId);
            builder.Entity<Promotion>().HasIndex(e => e.TenantId);
            builder.Entity<SystemConfiguration>().HasIndex(e => e.TenantId);
            //builder.Entity<PackComponent>().HasIndex(e => e.TenantId);

            // Additional useful indexes
            builder.Entity<Product>().HasIndex(e => e.Barcode);
            builder.Entity<Product>().HasIndex(e => e.Sku);
            builder.Entity<Product>()
            .HasIndex(product => new
            {
                product.TenantId,
                product.CatalogProductId
            })
            .IsUnique()
            .HasFilter(
                "\"CatalogProductId\" IS NOT NULL AND \"IsDeleted\" = FALSE");
            builder.Entity<Sale>().HasIndex(e => e.SaleDate);
            builder.Entity<Purchase>().HasIndex(e => e.PurchaseDate);
            builder.Entity<Customer>().HasIndex(e => e.Email);
            builder.Entity<Supplier>().HasIndex(e => e.Email);
            builder.Entity<PackComponent>().HasIndex(e => e.PackCatalaogId);
            builder.Entity<PackComponent>().HasIndex(e => e.ComponentCatalogId);
            builder.Entity<ProductCatalog>().HasIndex(e => e.Barcode);
            builder.Entity<ProductCatalog>().HasIndex(e => e.InternalCode);
            builder.Entity<ProductCategory>().HasIndex(e => e.Name);

            // ============================
            // PRODUCT RELATIONSHIPS
            // ============================

            // Product -> Stock (1:1)
            builder.Entity<Product>()
                .HasOne(p => p.Stock)
                .WithOne(s => s.Product)  // FIXED: Added navigation property
                .HasForeignKey<Stock>(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product -> ProductCatalog (Many:1)
            builder.Entity<Product>()
                .HasOne(p => p.CatalogProduct)
                .WithMany(pc => pc.TenantProducts)
                .HasForeignKey(p => p.CatalogProductId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<PackComponent>()
                .HasOne(p => p.PackCatalog)
                .WithMany(c => c.PackComponents)
                .HasForeignKey(p => p.PackCatalaogId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PackComponent>()
                .HasOne(p=> p.ComponentCatalog)
                .WithMany(c => c.UsedInPacks)
                .HasForeignKey(p => p.ComponentCatalogId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProductCatalog>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // ============================
            // SALE RELATIONSHIPS
            // ============================

            // Sale -> Customer (Many:1)
            builder.Entity<Sale>()
                .HasOne(s => s.Customer)
                .WithMany(c => c.Sales)
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Sale -> CashSession (Many:1)
            builder.Entity<Sale>()
                .HasOne(s => s.CashSession)
                .WithMany(cs => cs.Sales)
                .HasForeignKey(s => s.CashSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Sale -> Lines (1:Many)
            builder.Entity<Sale>()
                .HasMany(s => s.Lines)
                .WithOne(sl => sl.Sale)  // FIXED: Added navigation property
                .HasForeignKey(sl => sl.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Sale -> Payments (1:Many)
            builder.Entity<Sale>()
                .HasMany(s => s.Payments)
                .WithOne(p => p.Sale)  // FIXED: Added navigation property
                .HasForeignKey(p => p.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Sale -> Returns (1:Many)
            builder.Entity<Sale>()
                .HasMany(s => s.Returns)
                .WithOne(r => r.Sale)  // FIXED: Added navigation property
                .HasForeignKey(r => r.SaleId)
                .OnDelete(DeleteBehavior.Restrict);

            // SaleLine -> Product (Many:1)
            builder.Entity<SaleLine>()
                .HasOne(sl => sl.Product)
                .WithMany(p => p.SaleLines)
                .HasForeignKey(sl => sl.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // ============================
            // PURCHASE RELATIONSHIPS
            // ============================

            // Purchase -> Supplier (Many:1)
            builder.Entity<Purchase>()
                .HasOne(p => p.Supplier)
                .WithMany(s => s.Purchases)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // Purchase -> Lines (1:Many)
            builder.Entity<Purchase>()
                .HasMany(p => p.Lines)
                .WithOne(pl => pl.Purchase)  // FIXED: Added navigation property
                .HasForeignKey(pl => pl.PurchaseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Purchase -> Payments (1:Many)
            builder.Entity<Purchase>()
                .HasMany(p => p.Payments)
                .WithOne(pp => pp.Purchase)  // FIXED: Added navigation property
                .HasForeignKey(pp => pp.PurchaseId)
                .OnDelete(DeleteBehavior.Cascade);

            // PurchaseLine -> Product (Many:1)
            builder.Entity<PurchaseLine>()
                .HasOne(pl => pl.Product)
                .WithMany(p => p.PurchaseLines)
                .HasForeignKey(pl => pl.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // ============================
            // SUPPLIER RETURN RELATIONSHIPS
            // ============================

            // SupplierReturn -> Supplier (Many:1)
            builder.Entity<SupplierReturn>()
                .HasOne(sr => sr.Supplier)
                .WithMany(s => s.SupplierReturns)
                .HasForeignKey(sr => sr.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // SupplierReturn -> Lines (1:Many)
            builder.Entity<SupplierReturn>()
                .HasMany(sr => sr.Lines)
                .WithOne(srl => srl.SupplierReturn)  // FIXED: Added navigation property
                .HasForeignKey(srl => srl.SupplierReturnId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============================
            // RETURN RELATIONSHIPS
            // ============================

            // Return -> Lines (1:Many)
            builder.Entity<Return>()
                .HasMany(r => r.Lines)
                .WithOne(rl => rl.Return)  // FIXED: Added navigation property
                .HasForeignKey(rl => rl.ReturnId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============================
            // CUSTOMER RELATIONSHIPS
            // ============================

            // Customer -> LoyaltyCards (1:Many)
            builder.Entity<Customer>()
                .HasMany(c => c.LoyaltyCards)
                .WithOne(lc => lc.Customer)  // FIXED: Added navigation property
                .HasForeignKey(lc => lc.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Customer -> Transactions (1:Many)
            builder.Entity<Customer>()
                .HasMany(c => c.Transactions)
                .WithOne(ct => ct.Customer)  // FIXED: Added navigation property
                .HasForeignKey(ct => ct.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============================
            // LOYALTY CARD RELATIONSHIPS
            // ============================

            // LoyaltyCard -> Transactions (1:Many)
            builder.Entity<LoyaltyCard>()
                .HasMany(lc => lc.Transactions)
                .WithOne(lt => lt.LoyaltyCard)  // FIXED: Added navigation property
                .HasForeignKey(lt => lt.LoyaltyCardId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============================
            // STOCK MOVEMENT RELATIONSHIPS
            // ============================

            // StockMovement -> Product (Many:1)
            builder.Entity<StockMovement>()
                .HasOne(sm => sm.Product)
                .WithMany(p => p.StockMovements)
                .HasForeignKey(sm => sm.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // ============================
            // INVENTORY SESSION RELATIONSHIPS
            // ============================

            // InventorySession -> Lines (1:Many)
            builder.Entity<InventorySession>()
                .HasMany(i => i.Lines)
                .WithOne(il => il.InventorySession)  // FIXED: Added navigation property
                .HasForeignKey(il => il.InventorySessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============================
            // CASH SESSION RELATIONSHIPS
            // ============================

            builder.Entity<CashSession>()
                .HasOne(x => x.OpenedByUser)
                .WithMany(u => u.OpenedCashSessions)
                .HasForeignKey(x => x.OpenedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CashSession>()
                .HasOne(x => x.ClosedByUser)
                .WithMany(u => u.ClosedCashSessions)
                .HasForeignKey(x => x.ClosedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // CashSession -> CashMovements (1:Many)
            builder.Entity<CashSession>()
                .HasMany(cs => cs.CashMovements)
                .WithOne(cm => cm.CashSession)  // FIXED: Added navigation property
                .HasForeignKey(cm => cm.CashSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // CashSession -> CashReports (1:Many)
            builder.Entity<CashSession>()
                .HasMany(cs => cs.CashReports)
                .WithOne(cr => cr.CashSession)  // FIXED: Added navigation property
                .HasForeignKey(cr => cr.CashSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============================
            // CASH CORRECTION RELATIONSHIPS
            // ============================

            builder.Entity<CashCorrection>()
                .HasOne(x => x.CorrectedByUser)
                .WithMany(u => u.CashCorrections)
                .HasForeignKey(x => x.CorrectedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CashCorrection>()
                .HasOne(x => x.ApprovedByUser)
                .WithMany()
                .HasForeignKey(x => x.ApprovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ============================
            // CASH REPORT RELATIONSHIPS
            // ============================

            builder.Entity<CashReport>()
                .HasOne(x => x.GeneratedByUser)
                .WithMany(u => u.GeneratedReports)
                .HasForeignKey(x => x.GeneratedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ============================
            // INVENTORY SESSION USER RELATIONSHIPS
            // ============================

            builder.Entity<InventorySession>()
                .HasOne(x => x.User)
                .WithMany(u => u.InventorySessions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<InventorySession>()
                .HasOne(x => x.ValidatedByUser)
                .WithMany()
                .HasForeignKey(x => x.ValidatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ============================
            // AUDIT LOG RELATIONSHIPS
            // ============================

            builder.Entity<AuditLog>()
                .HasOne(x => x.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
        // ============================
        // DBSETS
        // ============================

        // Core Tenant Management
        public DbSet<Tenant> Tenants => Set<Tenant>();

        // Products & Catalog
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductCatalog> ProductCatalogs => Set<ProductCatalog>();

        // Sales & Transactions
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<SaleLine> SaleLines => Set<SaleLine>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<CustomerTransaction> CustomerTransactions => Set<CustomerTransaction>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Return> Returns => Set<Return>();
        public DbSet<ReturnLine> ReturnLines => Set<ReturnLine>();
        public DbSet<SaleReceipt> SaleReceipts => Set<SaleReceipt>();
        public DbSet<SalesSummaryDaily> SalesSummariesDaily => Set<SalesSummaryDaily>();

        // Purchases
        public DbSet<Purchase> Purchases => Set<Purchase>();
        public DbSet<PurchaseLine> PurchaseLines => Set<PurchaseLine>();
        public DbSet<PurchasePayment> PurchasePayments => Set<PurchasePayment>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<SupplierTransaction> SupplierTransactions => Set<SupplierTransaction>();
        public DbSet<SupplierReturn> SupplierReturns => Set<SupplierReturn>();
        public DbSet<SupplierReturnLine> SupplierReturnLines => Set<SupplierReturnLine>();

        // Inventory Management
        public DbSet<Stock> Stocks => Set<Stock>();
        public DbSet<StockMovement> StockMovements => Set<StockMovement>();
        public DbSet<InventorySession> InventorySessions => Set<InventorySession>();
        public DbSet<InventoryLine> InventoryLines => Set<InventoryLine>();
        public DbSet<Damage> Damages => Set<Damage>();

        // Cash Management
        public DbSet<CashSession> CashSessions => Set<CashSession>();
        public DbSet<CashMovement> CashMovements => Set<CashMovement>();
        public DbSet<CashReport> CashReports => Set<CashReport>();
        public DbSet<CashCorrection> CashCorrections => Set<CashCorrection>();

        // Loyalty & Promotions
        public DbSet<LoyaltyCard> LoyaltyCards => Set<LoyaltyCard>();
        public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();
        public DbSet<Promotion> Promotions => Set<Promotion>();

        // System Configuration
        public DbSet<DocumentNumber> DocumentNumbers => Set<DocumentNumber>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();
        public DbSet<PackComponent> PackComponents => Set<PackComponent>();
    }
}
