namespace Inventory.Dto.Queries
{
    public class PromotionQuery
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;

        public string? Search { get; init; }          // Name / Code
        public bool? IsActive { get; init; }
        public string? Type { get; init; }            // Percentage, FixedAmount, BuyXGetY

        public DateTime? FromDate { get; init; }
        public DateTime? ToDate { get; init; }

        public string SortBy { get; init; } = "StartDate";
        public bool Desc { get; init; } = true;
    }
}
