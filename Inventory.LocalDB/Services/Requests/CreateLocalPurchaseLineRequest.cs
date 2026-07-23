using System.ComponentModel.DataAnnotations;


namespace Inventory.LocalDB.Services.Requests
{
    public sealed class CreateLocalPurchaseLineRequest
    {
        [Required]
        public Guid ProductLocalId { get; set; }

        [Range(
            typeof(decimal),
            "0.001",
            "999999999",
            ErrorMessage = "Quantity must be greater than zero.")]
        public decimal Quantity { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "999999999",
            ErrorMessage = "Unit price cannot be negative.")]
        public decimal UnitPurchasePrice { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "100",
            ErrorMessage = "Discount must be between 0 and 100.")]
        public decimal DiscountPercent { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "100",
            ErrorMessage = "VAT rate must be between 0 and 100.")]
        public decimal VatRate { get; set; }
    }
}
