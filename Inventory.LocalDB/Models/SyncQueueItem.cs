namespace Inventory.LocalDB.Models
{
    public class SyncQueueItem : ILocalTenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string EntityName { get; set; } = string.Empty;
        // Sale, Return, Customer, Product, CashMovement, StockMovement

        public Guid LocalEntityId { get; set; }

        public Guid? ServerEntityId { get; set; }

        public string Operation { get; set; } = SyncOperation.Create;
        // Create, Update, Delete, Submit

        public string PayloadJson { get; set; } = string.Empty;

        public string Status { get; set; } = SyncQueueStatus.Pending;
        // Pending, Processing, Done, Failed, Conflict

        public int Attempts { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? LastAttemptAtUtc { get; set; }

        public DateTime? ProcessedAtUtc { get; set; }

        public Guid ClientOperationId { get; set; } = Guid.NewGuid();
    }
}
