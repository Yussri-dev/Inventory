using Inventory.Dto.Enums;

namespace Inventory.Dto.Queries
{
    public class InventorySessionQuery
    {
        // Pagination
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;

        // Filters
        public InventoryStatus? Status { get; init; }
        public Guid? UserId { get; init; }
        public DateTime? FromDate { get; init; }
        public DateTime? ToDate { get; init; }

        // Sorting
        public string SortBy { get; init; } = "StartedAt";
        public bool Desc { get; init; } = true;
    }
}
