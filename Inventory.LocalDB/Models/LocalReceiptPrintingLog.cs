
namespace Inventory.LocalDB.Models
{
    public sealed class LocalReceiptPrintLog
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid LocalReceiptId { get; set; }

        public string PrintType { get; set; } = ReceiptPrintType.Original;
        public int CopyNumber { get; set; }
        public DateTime PrintedAtUtc { get; set; }
        public Guid PrintedByUserId { get; set; }
        public string? DeviceName { get; set; }
        public string? Reason { get; set; }
        public bool WasSuccessful { get; set; }
        public string? ErrorMessage { get; set; }

    }
}
