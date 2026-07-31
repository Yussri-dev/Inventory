
namespace Inventory.LocalDB.Models
{
    public class LocalPurchaseDraft
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public Guid? SupplierLocalId { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        public PurchaseDraftStatus Status { get; set; }

        public List<LocalPurchaseDraftLine> Lines { get; set; } = new();
    }
}
