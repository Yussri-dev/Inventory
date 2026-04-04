namespace Inventory.Dto.Queries
{
    public class CustomerSaleQuery
    {
        public Guid? CustomerId { get; set; }
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
