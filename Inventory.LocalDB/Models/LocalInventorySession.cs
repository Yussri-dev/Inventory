using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.LocalDB.Models
{
    public sealed class LocalInventorySession
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public Guid ClientOperationId { get; set; }

        public Guid? ServerId { get; set; }

        public string SessionNumber { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            LocalInventoryStatus.InProgress;

        public string? Notes { get; set; }

        public DateTime StartedAtUtc { get; set; }

        public DateTime? ClosedAtUtc { get; set; }

        public DateTime? ValidatedAtUtc { get; set; }

        public string SyncStatus { get; set; } =
            SyncQueueStatus.Pending;

        public DateTime CreatedAtUtc { get; set; }

        public List<LocalInventoryLine> Lines { get; set; } =
            new();
    }

    public sealed class LocalInventoryLine
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public Guid LocalInventorySessionId { get; set; }

        public Guid ProductLocalId { get; set; }

        public Guid? ProductServerId { get; set; }

        public string ProductName { get; set; } =
            string.Empty;

        public string? ProductBarcode { get; set; }

        public decimal ExpectedQuantity { get; set; }

        public decimal CountedQuantity { get; set; }

        public bool IsAdjusted { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public LocalInventorySession Session { get; set; } =
            null!;
    }

    public static class LocalInventoryStatus
    {
        public const string InProgress =
            "InProgress";

        public const string Completed =
            "Completed";

        public const string Validated =
            "Validated";
    }
}
