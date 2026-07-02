namespace Inventory.LocalDB.Models
{
    public static class LocalPurchaseStatus
    {
        public const string Draft = "Draft";
        public const string Ordered = "Ordered";
        public const string PartiallyReceived = "PartiallyReceived";
        public const string Received = "Received";
        public const string Cancelled = "Cancelled";
    }
}