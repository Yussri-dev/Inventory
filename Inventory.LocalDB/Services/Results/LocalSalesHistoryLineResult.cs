namespace Inventory.LocalDB.Services.Results
{
    public sealed class LocalSalesHistoryLineResult
    {
        public Guid LocalId { get; set; }

        public string ProductName { get; set; } =
            string.Empty;

        public string? ProductBarcode { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal DiscountPercent { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal VatRate { get; set; }

        public decimal VatAmount { get; set; }

        public decimal LineTotal { get; set; }
    }
}
