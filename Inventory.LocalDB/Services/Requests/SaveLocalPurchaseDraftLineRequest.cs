namespace Inventory.LocalDB.Services.Requests
{
    public sealed class SaveLocalPurchaseDraftLineRequest
    {
        public Guid ProductLocalId { get; set; }

        public decimal Quantity { get; set; }

        public decimal BasePurchasePrice { get; set; }

        public decimal VatRate { get; set; }

        public int DisplayOrder { get; set; }

        public List<SaveLocalPurchaseDraftAdjustmentRequest> Adjustments
        {
            get;
            set;
        } = new();
    }
}
