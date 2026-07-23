using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.Purchases.Requests
{
    public sealed class PurchaseLineItem
    {
        [Required]
        public Guid ProductId { get; set; }

        [Range(
            typeof(decimal),
            "0.001",
            "999999999",
            ErrorMessage =
                "Quantity must be greater than zero.")]
        public decimal Quantity { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "999999999",
            ErrorMessage =
                "Unit price must be non-negative.")]
        public decimal UnitPrice { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "100",
            ErrorMessage =
                "VAT rate must be between zero and 100.")]
        public decimal VatRate { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "100",
            ErrorMessage =
                "Discount percent must be between zero and 100.")]
        public decimal DiscountPercent { get; set; }

        public decimal LineAmountExclVat =>
            Math.Round(
                Quantity *
                UnitPrice *
                (1m - DiscountPercent / 100m),
                2,
                MidpointRounding.AwayFromZero);

        public decimal LineVatAmount =>
            Math.Round(
                LineAmountExclVat *
                VatRate /
                100m,
                2,
                MidpointRounding.AwayFromZero);

        public decimal LineAmountInclVat =>
            LineAmountExclVat +
            LineVatAmount;
    }
}
