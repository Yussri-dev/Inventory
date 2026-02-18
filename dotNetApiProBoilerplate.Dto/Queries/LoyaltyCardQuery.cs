namespace Inventory.Dto.Queries
{
    public class LoyaltyCardQuery
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;

        public Guid? CustomerId { get; init; }
        public bool? IsActive { get; init; }

        public string? Search { get; init; }
        public string SortBy { get; init; } = "IssuedAt";
        public bool Desc { get; init; } = true;
    }
}
