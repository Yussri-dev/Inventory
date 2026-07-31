namespace Inventory.LocalDB.Services.Requests
{
    public sealed class SaveLocalPurchaseDraftRequest
    {
        public Guid? DraftId { get; set; }

        public Guid? SupplierLocalId { get; set; }

        public List<SaveLocalPurchaseDraftLineRequest> Lines { get; set; } =
            new();
    }
}
