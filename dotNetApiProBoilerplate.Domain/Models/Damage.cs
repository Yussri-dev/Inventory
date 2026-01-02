
using Inventory.Domain.Abstraction;
using Inventory.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Models
{
    public class Damage : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string DamageNumber { get; set; } = null!;

        [Required]
        public Guid ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;

        [Column(TypeName = "decimal(18,3)")]
        [Range(0, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedValue { get; set; }

        [Required, MaxLength(1000)]
        public string Reason { get; set; } = null!;

        [MaxLength(100)]
        public string? Category { get; set; } // Breakage, Expiry, Theft, etc.

        public DateTime DamageDate { get; set; }

        public bool IsApproved { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public Guid? ApprovedByUserId { get; set; }

        [MaxLength(500)]
        public string? Photos { get; set; } // JSON array of photo URLs

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

}
