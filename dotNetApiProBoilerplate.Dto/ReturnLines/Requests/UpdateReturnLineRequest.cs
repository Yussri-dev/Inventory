
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.ReturnLines.Requests
{
    public class UpdateReturnLineRequest
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ReturnId { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        [Range(0, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal VatRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineAmount => Quantity * UnitPrice;

        [MaxLength(500)]
        public string? Reason { get; set; }

        public bool RestockItem { get; set; }
    }
}
