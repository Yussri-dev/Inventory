using Inventory.Domain.Abstraction;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Entities
{
    public class ReturnLine : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ReturnId { get; set; }

        [ForeignKey(nameof(ReturnId))]
        public Return Return { get; set; } = null!;

        [Required]
        public Guid ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;

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
