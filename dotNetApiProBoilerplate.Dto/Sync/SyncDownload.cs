namespace Inventory.Dto.Sync;

// A complete table snapshot, not a search page. Deleted IDs are explicit tombstones.
public sealed class SyncDownload<T>
{
    [System.Text.Json.Serialization.JsonRequired]
    public List<T> Items { get; set; } = new();
    [System.Text.Json.Serialization.JsonRequired]
    public List<Guid> DeletedIds { get; set; } = new();
}
