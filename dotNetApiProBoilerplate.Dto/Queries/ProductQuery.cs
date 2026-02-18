using Inventory.Dto.Enums;

namespace Inventory.Dto.Queries
{
    public class ProductQuery
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? Search { get; init; }
        public ProductStatus? Status { get; init; }
        public string SortBy { get; init; } = "CreatedAt";
        public bool Desc { get; init; } = true;
    }
}
