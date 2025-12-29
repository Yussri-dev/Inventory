using Inventory.Dto.Enums;

namespace Inventory.Dto.Queries
{
    public class StockMouvementQuery
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;

        public Guid? ProductId { get; init; }
        public StockMovementType? Type { get; init; }

        public DateTime? FromDate { get; init; }
        public DateTime? ToDate { get; init; }

        public string SortBy { get; init; } = "CreatedAt";
        public bool Desc { get; init; } = true;
    }
}
