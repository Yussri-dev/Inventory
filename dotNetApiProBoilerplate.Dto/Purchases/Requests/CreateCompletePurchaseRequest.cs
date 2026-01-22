using System.ComponentModel.DataAnnotations;
using Inventory.Dto.Sales.Requests; // Using PaymentInfo from here or move to common

namespace Inventory.Dto.Purchases.Requests
{
    /// <summary>
    /// Request to create a complete purchase with lines and payment in a single atomic operation.
    /// </summary>
    public sealed class CreateCompletePurchaseRequest
    {
        [Required]
        public Guid SupplierId { get; set; }

        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MinLength(1, ErrorMessage = "At least one purchase line is required")]
        public List<PurchaseLineItem> Lines { get; set; } = new();

        public PaymentInfo? Payment { get; set; }
    }

    //public class CreateCompletePurchaseRequest : CreatePurchaseRequest
    //{
    //    /// <summary>
    //    /// List of products being purchased
    //    /// </summary>
    //    [Required]
    //    [MinLength(1, ErrorMessage = "At least one purchase line is required")]
    //    public List<PurchaseLineItem> Lines { get; set; } = new();

    //    /// <summary>
    //    /// Payment information (optional - if not provided, purchase is unpaid/pending)
    //    /// </summary>
    //    public PaymentInfo? Payment { get; set; }
    //}

    public class PurchaseLineItem
    {
        [Required]
        public Guid ProductId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Unit price must be non-negative")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Range(0, 100, ErrorMessage = "VAT rate must be between 0 and 100")]
        public decimal VatRate { get; set; }

        [Range(0, 100, ErrorMessage = "Discount percent must be between 0 and 100")]
        public decimal DiscountPercent { get; set; }

        // Calculated properties
        public decimal LineAmountExclVat => Quantity * UnitPrice * (1 - DiscountPercent / 100);
        public decimal LineVatAmount => LineAmountExclVat * (VatRate / 100);
        public decimal LineAmountInclVat => LineAmountExclVat + LineVatAmount;
    }
}
