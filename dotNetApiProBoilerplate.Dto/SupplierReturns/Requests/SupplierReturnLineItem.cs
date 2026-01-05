using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.SupplierReturns.Requests
{
    public class SupplierReturnLineItem
    {
        [Required]
        public Guid ProductId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}
