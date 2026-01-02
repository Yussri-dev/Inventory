namespace Inventory.Dto.Queries
{
    public class DamageQuery
    {
        // Pagination
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;

        // Filters
        public Guid? ProductId { get; init; }
        public bool? IsApproved { get; init; }
        public string? Category { get; init; }

        public DateTime? FromDate { get; init; }
        public DateTime? ToDate { get; init; }

        // Search
        public string? Search { get; init; } // DamageNumber, Reason

        // Sorting
        public string SortBy { get; init; } = "CreatedAt";
        public bool Desc { get; init; } = true;
    }
}
