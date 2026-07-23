namespace Inventory.LocalDB.Services.Results
{
    public sealed class LocalReturnableSaleLineResult
    {
        public Guid LocalSaleLineId { get; init; }

        public Guid ProductLocalId { get; init; }

        public Guid? ProductServerId { get; init; }

        public string ProductName { get; init; } = string.Empty;

        public string? ProductBarcode { get; init; }

        public decimal SoldQuantity { get; init; }

        public decimal ReturnedQuantity { get; init; }

        public decimal AvailableQuantity { get; init; }

        public decimal EffectiveUnitPrice { get; init; }

        public decimal VatRate { get; init; }

        public Guid UnitProductLocalId { get; init; }

        public Guid? UnitProductServerId { get; init; }

        public bool IsPack { get; init; }

        public decimal UnitsPerPack { get; init; }

        public decimal UnitCostPrice { get; init; }
    }
}
