namespace Inventory.Dto.Queries
{
    public class CashReportQuery
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;

        public Guid? CashSessionId { get; init; }
        public string? Type { get; init; }
        public Guid? GeneratedByUserId { get; init; }

        public DateTime? FromDate { get; init; }
        public DateTime? ToDate { get; init; }

        public string? Search { get; init; }

        public string SortBy { get; init; } = "GeneratedAt";
        public bool Desc { get; init; } = true;
    }
}
