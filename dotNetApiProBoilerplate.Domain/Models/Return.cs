
using Inventory.Domain.Abstraction;
using Inventory.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Entities
{
    public class Return : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string ReturnNumber { get; set; } = null!;

        [Required]
        public Guid SaleId { get; set; }

        [ForeignKey(nameof(SaleId))]
        public Sale Sale { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        public RefundMethod RefundMethod { get; set; }

        [MaxLength(1000)]
        public string? Reason { get; set; }

        public DateTime ReturnDate { get; set; }

        public bool IsProcessed { get; set; }
        public DateTime? ProcessedAt { get; set; }

        // Navigation properties
        public ICollection<ReturnLine> Lines { get; set; } = new List<ReturnLine>();
    }
}
