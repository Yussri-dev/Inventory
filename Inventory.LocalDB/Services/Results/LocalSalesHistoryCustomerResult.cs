namespace Inventory.LocalDB.Services.Results
{
    public sealed class LocalSalesHistoryCustomerResult
    {
        public Guid LocalId { get; set; }

        public Guid? ServerId { get; set; }

        public string Name { get; set; } =
            string.Empty;

        public string? Phone { get; set; }
    }
}
