using Inventory.LocalDB.Enums;

namespace Inventory.LocalDB.Models
{
    public sealed class LocalPurchaseDraftAdjustment
    {
        public Guid Id { get; set; }

        public Guid PurchaseDraftLineId { get; set; }

        public PurchaseDraftAdjustmentType Type { get; set; }

        public decimal Value { get; set; }

        public int DisplayOrder { get; set; }

        public LocalPurchaseDraftLine PurchaseDraftLine { get; set; } =
            null!;
    }
}
