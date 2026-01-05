using System.ComponentModel.DataAnnotations;
using Inventory.Dto.Enums;

namespace Inventory.Dto.SupplierReturns.Requests
{
    public class CreateCompleteSupplierReturnRequest
    {
        [Required]
        public Guid SupplierId { get; set; }

        // Optional: Auto-generated if null
        [MaxLength(100)]
        public string? ReturnNumber { get; set; }

        public DateTime ReturnDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MinLength(1, ErrorMessage = "At least one line is required")]
        public List<SupplierReturnLineItem> Lines { get; set; } = new();

        [MaxLength(1000)]
        public string? Reason { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public decimal TotalAmount => Lines.Sum(l => l.Quantity * l.UnitPrice);
    }
}
