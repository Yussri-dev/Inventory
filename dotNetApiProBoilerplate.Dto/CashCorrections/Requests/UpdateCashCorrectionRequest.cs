using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.CashCorrections.Requests
{
    public class UpdateCashCorrectionRequest
    {
        public Guid Id { get; set; }

        [Required]
        public Guid OriginalCashSessionId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required, MaxLength(1000)]
        public string Reason { get; set; } = null!;

        public DateTime CorrectedAt { get; set; }

        [Required]
        public Guid CorrectedByUserId { get; set; }

        [Required]
        public Guid ApprovedByUserId { get; set; }

        public DateTime ApprovedAt { get; set; }

        [MaxLength(500)]
        public string? ApprovalNotes { get; set; }
    }
}
