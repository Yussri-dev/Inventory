using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.Sales.Requests
{
    /// <summary>
    /// Request to create a complete sale with lines and payment in a single atomic operation.
    /// This automatically creates SaleLines, StockMovements, and Payment records.
    /// </summary>
    public class CreateCompleteSaleRequest
    {
        /// <summary>
        /// Optional customer ID for the sale
        /// </summary>
        public Guid? CustomerId { get; set; }

        /// <summary>
        /// Optional Loyalty Card ID to accumulate points
        /// </summary>
        public Guid? LoyaltyCardId { get; set; }

        /// <summary>
        /// Cash session ID where this sale is recorded
        /// </summary>
        [Required]
        public Guid CashSessionId { get; set; }

        /// <summary>
        /// Date of the sale (defaults to current UTC time if not provided)
        /// </summary>
        public DateTime SaleDate { get; set; }

        /// <summary>
        /// Optional notes or comments about the sale
        /// </summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }

        /// <summary>
        /// List of products being sold with quantities and prices
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "At least one sale line is required")]
        public List<SaleLineItem> Lines { get; set; } = new();

        /// <summary>
        /// Multiple payment methods (optional - if not provided, sale is marked as unpaid)
        /// Supports mixed payments like Cash + Card, or Cash + Credit
        /// </summary>
        public List<PaymentInfo>? Payments { get; set; }

        /// <summary>
        /// Discount amount applied to the entire sale
        /// </summary>
        public decimal DiscountAmount { get; set; }

        // Calculated properties from Lines
        public decimal SubtotalAmount => Lines.Sum(l => l.LineAmountExclVat);
        public decimal VatAmount => Lines.Sum(l => l.LineAmountInclVat - l.LineAmountExclVat);
        public decimal TotalAmount => SubtotalAmount + VatAmount - DiscountAmount;

        /// <summary>
        /// Total amount paid across all payment methods
        /// </summary>
        public decimal PaidAmount => Payments?.Sum(p => p.Amount) ?? 0;

        /// <summary>
        /// Change amount to return to customer
        /// </summary>
        public decimal ChangeAmount { get; set; }
    }

    /// <summary>
    /// Represents a single line item in a sale
    /// </summary>
    public class SaleLineItem
    {
        /// <summary>
        /// Product being sold
        /// </summary>
        [Required]
        public Guid ProductId { get; set; }

        /// <summary>
        /// Quantity of the product
        /// </summary>
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Unit price of the product (excluding VAT)
        /// </summary>
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Unit price must be non-negative")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// VAT rate as a percentage (e.g., 20 for 20%)
        /// </summary>
        [Required]
        [Range(0, 100, ErrorMessage = "VAT rate must be between 0 and 100")]
        public decimal VatRate { get; set; }

        /// <summary>
        /// Discount percentage for this line (e.g., 10 for 10% off)
        /// </summary>
        [Range(0, 100, ErrorMessage = "Discount percent must be between 0 and 100")]
        public decimal DiscountPercent { get; set; }

        // Calculated properties
        public decimal LineAmountExclVat => Quantity * UnitPrice * (1 - DiscountPercent / 100);
        public decimal LineVatAmount => LineAmountExclVat * (VatRate / 100);
        public decimal LineAmountInclVat => LineAmountExclVat + LineVatAmount;
    }

    /// <summary>
    /// Payment information for a sale
    /// </summary>
    public class PaymentInfo
    {
        /// <summary>
        /// Amount paid
        /// </summary>
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than 0")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Payment method (e.g., "Cash", "Card", "Credit")
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = null!;

        /// <summary>
        /// Optional payment reference (e.g., transaction ID, check number)
        /// </summary>
        [MaxLength(200)]
        public string? Reference { get; set; }
    }
}