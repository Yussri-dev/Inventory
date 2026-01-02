namespace Inventory.Dto.Queries
{
    public class SalesSummaryDailyQuery
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;

        public DateTime? FromDate { get; init; }
        public DateTime? ToDate { get; init; }

        public string SortBy { get; init; } = "Date";
        public bool Desc { get; init; } = true;
    }
}
