namespace Inventory.Dto.Queries
{
    public class LoyaltyTransactionQuery
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;

        public Guid? LoyaltyCardId { get; init; }
        public Guid? SaleId { get; init; }

        public string? Search { get; init; } // Reason
        public string SortBy { get; init; } = "TransactionDate";
        public bool Desc { get; init; } = true;
    }
}
