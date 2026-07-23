using System.ComponentModel.DataAnnotations;

namespace Inventory.LocalDB.Models;

public sealed class LocalDamage : ILocalTenantEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    public Guid? ServerId { get; set; }

    [Required]
    [MaxLength(100)]
    public string DamageNumber { get; set; } = string.Empty;

    public Guid ProductLocalId { get; set; }

    public Guid? ProductServerId { get; set; }

    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal EstimatedValue { get; set; }

    [MaxLength(1000)]
    public string? Reason { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    public DateTime DamageDateUtc { get; set; } =
        DateTime.UtcNow;

    public DateTime? ValidatedAtUtc { get; set; }

    [Required]
    [MaxLength(50)]
    public string LocalStatus { get; set; } =
        LocalDamageStatus.Draft;

    [MaxLength(50)]
    public string? ServerStatus { get; set; }

    public bool IsDeleted { get; set; }

    [Required]
    [MaxLength(50)]
    public string SyncStatus { get; set; } =
        SyncQueueStatus.Done;

    public DateTime CreatedAtUtc { get; set; } =
        DateTime.UtcNow;

    public DateTime DeletedAtUtc { get; set; } =
       DateTime.UtcNow;

    public DateTime? ModifiedAtUtc { get; set; }

    public DateTime? LastSyncedAtUtc { get; set; }

    public LocalProduct Product { get; set; } = null!;
}