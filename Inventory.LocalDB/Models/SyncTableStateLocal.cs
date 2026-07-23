using System.ComponentModel.DataAnnotations;

namespace Inventory.LocalDB.Models
{
    public sealed class SyncTableStateLocal
    {
        [Key]
        public Guid Id { get; set; } =
            Guid.NewGuid();

        public Guid TenantId { get; set; }

        [Required]
        [MaxLength(100)]
        public string EntityName { get; set; } =
            string.Empty;

        [Required]
        [MaxLength(50)]
        public string Syncmode { get; set; } =
            SyncMode.FullSync;

        public bool InitialSyncCompleted { get; set; }

        public DateTime? LastSuccessfulSyncAtUtc { get; set; }

        public DateTime? LastServerChangeAtUtc { get; set; }

        public string? ContinuationToken { get; set; }

        public string? LastError { get; set; }
    }
}
