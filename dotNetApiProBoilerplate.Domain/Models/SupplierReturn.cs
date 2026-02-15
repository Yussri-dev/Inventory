
using Inventory.Domain.Abstraction;
using Inventory.Domain.Entities;
using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Models
{
    public class SupplierReturn : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string ReturnNumber { get; set; } = null!;

        [Required]
        public Guid SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public Supplier Supplier { get; set; } = null!;

        [Required, MaxLength(1000)]
        public string Reason { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        public SupplierReturnStatus Status { get; set; }

        public DateTime ReturnDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        public Guid? PurchaseId { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Navigation properties
        public ICollection<SupplierReturnLine> Lines { get; set; } = new List<SupplierReturnLine>();
    }

}
