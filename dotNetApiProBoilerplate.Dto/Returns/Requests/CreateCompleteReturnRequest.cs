using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.Returns.Requests
{
    public class CreateCompleteReturnRequest : CreateReturnRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one return line is required")]
        public List<ReturnLineItem> Lines { get; set; } = new();
    }

    public class ReturnLineItem
    {
        [Required]
        public Guid ProductId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Unit price must be non-negative")]
        public decimal UnitPrice { get; set; }

        [Range(0, 100, ErrorMessage = "VAT rate must be between 0 and 100")]
        public decimal VatRate { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        public bool RestockItem { get; set; }
    }
}
