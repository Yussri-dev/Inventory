using Inventory.Dto.Enums;

namespace Inventory.Dto.Queries
{
    public class CashMovementQuery
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;

        public Guid? CashSessionId { get; init; }
        public Guid? SaleId { get; init; }
        public CashMovementType? Type { get; init; }

        public DateTime? FromDate { get; init; }
        public DateTime? ToDate { get; init; }

        public string? Search { get; init; }

        public string SortBy { get; init; } = "MovementDate";
        public bool Desc { get; init; } = true;
    }
}
