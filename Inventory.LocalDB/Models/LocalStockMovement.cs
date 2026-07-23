using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.LocalDB.Models
{
    public class LocalStockMovement : ILocalTenantEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TenantId { get; set; }
        public Guid? ServerId { get; set; }

        public Guid ClientOperationId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProductLocalId { get; set; }

        public Guid ProductServerId { get; set; }

        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ProductBarcode { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal QuantityChange { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal QuantityBefore { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal QuantityAfter { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;
        // Sale, Purchase, Return, Adjustment, Transfer, InitialStock

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCost { get; set; }

        [NotMapped]
        public decimal TotalCost => Math.Abs(QuantityChange) * UnitCost;

        public Guid? LocalReferenceId { get; set; }

        public Guid? ServerReferenceId { get; set; }

        [MaxLength(200)]
        public string? ReferenceNumber { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime MovementDateUtc { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string SyncStatus { get; set; } = SyncQueueStatus.Pending;

        public DateTime? LastSyncedAtUtc { get; set; }
    }
}
