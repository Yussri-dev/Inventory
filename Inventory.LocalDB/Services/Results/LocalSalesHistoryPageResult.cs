namespace Inventory.LocalDB.Services.Results
{
    public sealed class LocalSalesHistoryPageResult
    {
        public IReadOnlyList<LocalSalesHistoryItemResult> Items { get; set; } =
            Array.Empty<LocalSalesHistoryItemResult>();

        public int TotalCount { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }
    }
}
