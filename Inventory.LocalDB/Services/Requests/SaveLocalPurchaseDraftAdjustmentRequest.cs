using Inventory.LocalDB.Enums;

namespace Inventory.LocalDB.Services.Requests
{
    public sealed class SaveLocalPurchaseDraftAdjustmentRequest
    {
        public PurchaseDraftAdjustmentType Type { get; set; }

        public decimal Value { get; set; }

        public int DisplayOrder { get; set; }
    }
}
