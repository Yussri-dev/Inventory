namespace Inventory.LocalDB.Models
{
    public class LocalProductScanResult
    {
        public Guid ProductLocalId { get; set; }

        public Guid? ProductServerId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string? ProductBarcode { get; set; }

        public Guid UnitProductLocalId { get; set; }

        public Guid? UnitProductServerId { get; set; }

        public string UnitProductName { get; set; } = string.Empty;

        public string? UnitProductBarcode { get; set; }

        public bool IsPack { get; set; }

        public decimal Quantity { get; set; } = 1;

        public decimal UnitQuantity { get; set; } = 1;

        public decimal UnitPrice { get; set; }

        public decimal PurchasePrice { get; set; }

        public decimal VatRate { get; set; }
    }
}
