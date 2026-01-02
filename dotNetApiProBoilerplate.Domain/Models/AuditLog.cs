using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Models
{
    // ===============================
    // 12. AUDIT LOG
    // ===============================

    public class AuditLog
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string EntityType { get; set; } = null!;

        [Required]
        public Guid EntityId { get; set; }

        [Required, MaxLength(50)]
        public string Action { get; set; } = null!; // Create, Update, Delete

        public string? OldValues { get; set; } // JSON

        public string? NewValues { get; set; } // JSON

        [MaxLength(500)]
        public string? ChangeSummary { get; set; }

        public DateTime CreatedAt { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        [Required]
        public Guid TenantId { get; set; }

        [ForeignKey(nameof(TenantId))]
        public Tenant Tenant { get; set; } = null!;

        [MaxLength(100)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }
    }

}
