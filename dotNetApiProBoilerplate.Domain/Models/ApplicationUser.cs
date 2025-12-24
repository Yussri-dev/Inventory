using Inventory.Domain.Entities;
using Inventory.Dto.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Models
{
    public class ApplicationUser : IdentityUser<Guid>

    {
        // Note: IdentityUser<Guid> fournit déjà :
        // - Id (Guid)
        // - UserName (string)
        // - Email (string)
        // - EmailConfirmed (bool)
        // - PasswordHash (string)
        // - PhoneNumber (string)
        // - PhoneNumberConfirmed (bool)
        // - TwoFactorEnabled (bool)
        // - LockoutEnd (DateTimeOffset?)
        // - LockoutEnabled (bool)
        // - AccessFailedCount (int)
        // - SecurityStamp (string)
        // - ConcurrencyStamp (string)

        [MaxLength(200)]
        public string? FullName { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        [Required]
        public UserRole Role { get; set; }

        [MaxLength(500)]
        public string? ProfilePictureUrl { get; set; }

        [Required]
        public Guid TenantId { get; set; }

        [ForeignKey(nameof(TenantId))]
        public Tenant? Tenant { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastLoginAt { get; set; }

        [MaxLength(100)]
        public string? LastLoginIp { get; set; }
        public DateTime? PasswordChangedAt { get; set; }
        public bool MustChangePassword { get; set; } = false;
        public DateTime? DeactivatedAt { get; set; }

        [MaxLength(500)]
        public string? DeactivationReason { get; set; }

        [MaxLength(10)]
        public string PreferredLanguage { get; set; } = "en";

        [MaxLength(50)]
        public string PreferredTheme { get; set; } = "light"; // light, dark

        public bool ReceiveEmailNotifications { get; set; } = true;

        public bool ReceiveSmsNotifications { get; set; } = false;

        // ===============================
        // AUDIT
        // ===============================

        public DateTime CreatedAt { get; set; }

        public Guid? CreatedByUserId { get; set; }

        public DateTime? ModifiedAt { get; set; }

        public Guid? ModifiedByUserId { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        public Guid? DeletedByUserId { get; set; }

        // ===============================
        // MÉTADONNÉES
        // ===============================

        [MaxLength(2000)]
        public string? Notes { get; set; }

        [MaxLength(4000)]
        public string? CustomSettings { get; set; } // JSON

        public ICollection<CashSession> OpenedCashSessions { get; set; } = new List<CashSession>();

        public ICollection<CashSession> ClosedCashSessions { get; set; } = new List<CashSession>();

        public ICollection<InventorySession> InventorySessions { get; set; } = new List<InventorySession>();

        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        public ICollection<CashReport> GeneratedReports { get; set; } = new List<CashReport>();

        public ICollection<CashCorrection> CashCorrections { get; set; } = new List<CashCorrection>();



        public string GetDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(FullName))
                return FullName;

            if (!string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(LastName))
                return $"{FirstName} {LastName}";

            return UserName ?? Email ?? "Unknown User";
        }

        public bool HasRole(UserRole role)
        {
            return Role == role;
        }

        public bool HasAnyRole(params UserRole[] roles)
        {
            foreach (var role in roles)
            {
                if (Role == role)
                    return true;
            }
            return false;
        }

        public bool IsAdmin()
        {
            return Role == UserRole.SuperAdmin || Role == UserRole.Admin;
        }

        public bool CanAccessTenant(Guid tenantId)
        {
            return TenantId == tenantId || Role == UserRole.SuperAdmin;
        }


    }
}
