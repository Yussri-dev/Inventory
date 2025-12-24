
using Inventory.Domain.Abstraction;
using Inventory.Domain.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Entities
{
    public class CashCorrection : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid OriginalCashSessionId { get; set; }

        [ForeignKey(nameof(OriginalCashSessionId))]
        public CashSession OriginalCashSession { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required, MaxLength(1000)]
        public string Reason { get; set; } = null!;

        public DateTime CorrectedAt { get; set; }

        [Required]
        public Guid CorrectedByUserId { get; set; }

        [ForeignKey(nameof(CorrectedByUserId))]
        public ApplicationUser CorrectedByUser { get; set; } = null!;

        [Required]
        public Guid ApprovedByUserId { get; set; }

        [ForeignKey(nameof(ApprovedByUserId))]
        public ApplicationUser ApprovedByUser { get; set; } = null!;

        public DateTime ApprovedAt { get; set; }

        [MaxLength(500)]
        public string? ApprovalNotes { get; set; }
    }
}
