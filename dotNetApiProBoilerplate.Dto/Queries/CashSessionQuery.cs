using Inventory.Dto.Enums;

namespace Inventory.Dto.Queries
{
    public class CashSessionQuery
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;

        public string? Search { get; init; }
        public CashSessionStatus? Status { get; init; }

        public Guid? OpenedByUserId { get; init; }

        public DateTime? FromDate { get; init; }
        public DateTime? ToDate { get; init; }

        public string SortBy { get; init; } = "OpenedAt";
        public bool Desc { get; init; } = true;
    }
}
