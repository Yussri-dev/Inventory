using Inventory.Dto.Enums;

namespace Inventory.Dto.Queries
{
    public class SupplierReturnQuery
    {
        // Pagination
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;

        // Filters
        public Guid? SupplierId { get; init; }
        public SupplierReturnStatus? Status { get; init; }

        public DateTime? FromDate { get; init; }
        public DateTime? ToDate { get; init; }

        // Search
        public string? Search { get; init; }

        // Sorting
        public string SortBy { get; init; } = "CreatedAt";
        public bool Desc { get; init; } = true;
    }
}
