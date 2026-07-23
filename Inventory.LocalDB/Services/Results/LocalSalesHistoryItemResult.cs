namespace Inventory.LocalDB.Services.Results
{
    public sealed class LocalSalesHistoryItemResult
    {
        public Guid LocalId { get; set; }

        public Guid? ServerId { get; set; }

        public string InvoiceNumber { get; set; } =
            string.Empty;

        public string LocalInvoiceNumber { get; set; } =
            string.Empty;

        public string? ServerInvoiceNumber { get; set; }

        public DateTime SaleDateUtc { get; set; }

        public Guid? CustomerLocalId { get; set; }

        public Guid? CustomerServerId { get; set; }

        public string? CustomerName { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal ChangeAmount { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public string PaymentStatus { get; set; } =
            string.Empty;

        public string SyncStatus { get; set; } =
            string.Empty;
    }
}
