using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.Returns.Requests
{
    public sealed class ReturnLineItem
    {
        
        public Guid? SaleLineId { get; set; }

        
        [Required]
        public Guid ProductId { get; set; }

        [Range(
            typeof(decimal),
            "0.001",
            "999999999999999.999",
            ErrorMessage =
                "Quantity must be greater than zero.")]
        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

       
        [Range(
            typeof(decimal),
            "0",
            "999999999999999.99",
            ErrorMessage =
                "Unit price must be non-negative.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        
        [Range(
            typeof(decimal),
            "0",
            "100",
            ErrorMessage =
                "VAT rate must be between 0 and 100.")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal VatRate { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } =
            string.Empty;

        public bool RestockItem { get; set; }
    }
}
