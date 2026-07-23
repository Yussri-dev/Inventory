namespace Inventory.LocalDB.Services.Results
{
    public sealed class LocalPurchaseResult
    {
        public Guid Id { get; set; }

        public Guid? ServerId { get; set; }

        public Guid ClientOperationId { get; set; }

        public Guid? SupplierLocalId { get; set; }

        public Guid? SupplierServerId { get; set; }

        public string LocalPurchaseNumber { get; set; } =
            string.Empty;

        public string? ServerPurchaseNumber { get; set; }

        public string? SupplierInvoiceNumber { get; set; }

        public decimal TotalAmountExclVat { get; set; }

        public decimal TotalVatAmount { get; set; }

        public decimal TotalAmountInclVat { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public string SyncStatus { get; set; } =
            string.Empty;

        public DateTime PurchaseDateUtc { get; set; }

        public DateTime? ExpectedDeliveryDateUtc { get; set; }

        public DateTime? DeliveryDateUtc { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? LastSyncedAtUtc { get; set; }

        public List<LocalPurchaseLineResult> Lines { get; set; } =
            new();
    }
}
