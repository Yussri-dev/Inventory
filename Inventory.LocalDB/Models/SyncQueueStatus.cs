namespace Inventory.LocalDB.Models
{
    public static class SyncQueueStatus
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Done = "Done";
        public const string Failed = "Failed";
        public const string Conflict = "Conflict";
    }
}
