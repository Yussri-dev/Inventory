namespace Inventory.LocalDB.Models
{
    public sealed class ReceiptPrintDocument
    {
        public Guid ReceiptId { get; init; }

        public required ReceiptSnapshot Snapshot { get; init; }

        public bool IsDuplicate { get; init; }

        public int CopyNumber { get; init; }

        public DateTime PrintedAtUtc { get; init; }

        public string? Reason { get; init; }
    }
}
