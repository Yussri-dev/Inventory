using Inventory.Dto.Enums;

namespace Inventory.Dto.Queries
{
    public class ProductCatalogQuery
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public string? Search { get; init; }
        public ProductStatus? Status { get; init; }
        public string SortBy { get; init; } = "CreatedAt";
        public bool Desc { get; init; } = true;
    }
}
