
namespace Inventory.LocalDB.Services.Results
{

    public sealed class LocalDamageProductResult
    {
        public Guid ProductLocalId { get; set; }

        public Guid? ProductServerId { get; set; }

        public string Name { get; set; } =
            string.Empty;

        public string? Barcode { get; set; }

        public decimal PurchasePrice { get; set; }

        public decimal Quantity { get; set; }

        public decimal ReservedQuantity { get; set; }

        public decimal AvailableQuantity =>
            Math.Max(0, Quantity - ReservedQuantity);
    }
}
