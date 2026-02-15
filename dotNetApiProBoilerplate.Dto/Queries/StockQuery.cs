
namespace Inventory.Dto.Queries
{
    public class StockQuery
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public Guid? ProductId { get; init; }
        public string? Search { get; set; }

        public string SortBy { get; init; } = "CreatedAt";
        public bool Desc { get; init; } = true;
    }
}
