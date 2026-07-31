
namespace Inventory.LocalDB.Models
{
    public sealed class LocalPurchaseDraftLine
    {
        public Guid Id { get; set; }

        public Guid PurchaseDraftId { get; set; }

        public Guid ProductLocalId { get; set; }

        public decimal Quantity { get; set; }

        public decimal BasePurchasePrice { get; set; }

        public decimal EffectiveUnitPrice { get; set; }

        public decimal VatRate { get; set; }

        public int DisplayOrder { get; set; }

        public LocalPurchaseDraft PurchaseDraft { get; set; } =
            null!;

        public List<LocalPurchaseDraftAdjustment> Adjustments { get; set; } =
            new();
    }
}
