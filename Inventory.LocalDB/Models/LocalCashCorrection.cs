using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.LocalDB.Models
{
    public class LocalCashCorrection : ILocalTenantEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }

        public Guid? ServerId { get; set; }

        public Guid ClientOperationId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid OriginalLocalCashSessionId { get; set; }

        public LocalCashSession OriginalLocalCashSession { get; set; } = null!;

        public Guid? OriginalServerCashSessionId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required, MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;

        public DateTime CorrectedAtUtc { get; set; } = DateTime.UtcNow;

        public Guid? CorrectedByUserId { get; set; }

        public Guid? ApprovedByUserId { get; set; }

        public DateTime? ApprovedAtUtc { get; set; }

        [MaxLength(500)]
        public string? ApprovalNotes { get; set; }

        [Required, MaxLength(50)]
        public string SyncStatus { get; set; } = SyncQueueStatus.Pending;

        public DateTime? LastSyncedAtUtc { get; set; }
    }
}