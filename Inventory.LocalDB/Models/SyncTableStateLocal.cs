namespace Inventory.LocalDB.Models
{
    public class SyncTableStateLocal
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string EntityName { get; set; } = string.Empty;

        public long LocalVersion { get; set; }

        public long ServerVersion { get; set; }

        public DateTime? LastSyncUtc { get; set; }

        public string Syncmode { get; set; } = SyncMode.SimpleSync;

        public string? LastSyncErrorMessage { get; set; }
    }
}
