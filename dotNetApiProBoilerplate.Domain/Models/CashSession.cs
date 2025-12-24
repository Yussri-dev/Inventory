
using Inventory.Domain.Abstraction;
using Inventory.Domain.Enums;
using Inventory.Domain.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Entities
{
    public class CashSession : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string SessionNumber { get; set; } = null!;

        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OpeningAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ClosingAmountExpected { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ClosingAmountCounted { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Difference { get; set; }

        [Required]
        public CashSessionStatus Status { get; set; }

        [Required]
        public Guid OpenedByUserId { get; set; }

        [ForeignKey(nameof(OpenedByUserId))]
        public ApplicationUser OpenedByUser { get; set; } = null!;

        public Guid? ClosedByUserId { get; set; }

        [ForeignKey(nameof(ClosedByUserId))]
        public ApplicationUser? ClosedByUser { get; set; }

        [MaxLength(1000)]
        public string? OpeningNotes { get; set; }

        [MaxLength(1000)]
        public string? ClosingNotes { get; set; }

        // Navigation properties
        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
        public ICollection<CashMovement> CashMovements { get; set; } = new List<CashMovement>();
        public ICollection<CashReport> CashReports { get; set; } = new List<CashReport>();
    }
}
