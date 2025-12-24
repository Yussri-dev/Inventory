using Inventory.Domain.Entities;
using Inventory.Domain.Models;
using Inventory.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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

            // ============================
            // CASH CORRECTION
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
            // CASH SESSION
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

            // ============================
            // CASH REPORT
            // ============================
            builder.Entity<CashReport>()
                .HasOne(x => x.GeneratedByUser)
                .WithMany(u => u.GeneratedReports)
                .HasForeignKey(x => x.GeneratedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ============================
            // INVENTORY SESSION
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
            // AUDIT LOG
            // ============================
            builder.Entity<AuditLog>()
                .HasOne(x => x.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public DbSet<Product> Products => Set<Product>();
    }
}
