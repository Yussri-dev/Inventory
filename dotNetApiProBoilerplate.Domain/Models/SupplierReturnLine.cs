using Inventory.Domain.Abstraction;
using Inventory.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Models
{
    public class SupplierReturnLine : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid SupplierReturnId { get; set; }

        [ForeignKey(nameof(SupplierReturnId))]
        public SupplierReturn SupplierReturn { get; set; } = null!;

        [Required]
        public Guid ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;

        [Column(TypeName = "decimal(18,3)")]
        [Range(0, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal UnitPurchasePrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal VatRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineAmount => Quantity * UnitPurchasePrice;

        [MaxLength(500)]
        public string? Reason { get; set; }
    }

}
