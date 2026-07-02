using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.LocalDB.Models
{
    public class LocalCashSession
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? ServerId { get; set; }

        public Guid ClientOperationId { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string SessionNumber { get; set; } = string.Empty;

        public DateTime OpenedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? ClosedAtUtc { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OpeningAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ClosingAmountExpected { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ClosingAmountCounted { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Difference { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = LocalCashSessionStatus.Open;

        public Guid? OpenedByUserId { get; set; }

        public Guid? ClosedByUserId { get; set; }

        [MaxLength(1000)]
        public string? OpeningNotes { get; set; }

        [MaxLength(1000)]
        public string? ClosingNotes { get; set; }

        [MaxLength(50)]
        public string SyncStatus { get; set; } = SyncQueueStatus.Pending;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? LastSyncedAtUtc { get; set; }

        public ICollection<LocalSale> Sales { get; set; } = new List<LocalSale>();

        public ICollection<LocalCashMovement> CashMovements { get; set; } = new List<LocalCashMovement>();
    }
}
