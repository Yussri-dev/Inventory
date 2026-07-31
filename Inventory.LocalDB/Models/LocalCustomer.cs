using System.ComponentModel.DataAnnotations;

namespace Inventory.LocalDB.Models;

public sealed class LocalCustomer : ILocalTenantEntity
{
    [Key]
    public Guid Id { get; set; } =
        Guid.NewGuid();

    public Guid? ServerId { get; set; }

    public Guid TenantId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } =
        string.Empty;

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? TaxNumber { get; set; }

    public decimal CreditLimit { get; set; }

    public decimal CurrentBalance { get; set; }

    public bool IsActive { get; set; } =
        true;
    public bool AllowCredit { get; set; }

    public bool HasUnlimitedCredit { get; set; }
    public bool IsDeleted { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(50)]
    public string SyncStatus { get; set; } =
        SyncQueueStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } =
        DateTime.UtcNow;

    public DateTime? ModifiedAtUtc { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public DateTime? LastSyncedAtUtc { get; set; }
}