namespace Inventory.Dto.Queries
{
    public class SaleReceiptQuery
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;

        public Guid? SaleId { get; init; }
        public bool? IsPrinted { get; init; }
        public bool? IsEmailed { get; init; }

        public string? Search { get; init; }          // ReceiptNumber / Email
        public string SortBy { get; init; } = "GeneratedAt";
        public bool Desc { get; init; } = true;
    }
}
