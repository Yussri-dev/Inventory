namespace Inventory.Dto.Queries
{
    public class InventoryLineQuery
    {
        // Pagination
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;

        // Filters
        public Guid? InventorySessionId { get; init; }
        public Guid? ProductId { get; init; }

        /// <summary>
        /// true  = uniquement les lignes avec écart
        /// false = uniquement les lignes sans écart
        /// null  = toutes
        /// </summary>
        public bool? HasVariance { get; init; }

        // Sorting
        public string SortBy { get; init; } = "CreatedAt";
        public bool Desc { get; init; } = true;
    }
}
