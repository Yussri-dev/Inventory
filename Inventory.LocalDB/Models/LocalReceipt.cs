
namespace Inventory.LocalDB.Models
{
    public sealed class LocalReceipt
    {
        public Guid Id  { get; set; }
        public Guid TenantId { get; set; }

        public Guid LocalSaleId { get; set; }
        public Guid? ServerSaleId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string SnapshotJson { get; set; } = string.Empty;
        public string? SnapshotHash { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public string SyncStatus { get; set; } = SyncQueueStatus.Pending;
    }
}
