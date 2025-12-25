
using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.CashSessions.Requests
{
    public class UpdateCashSessionRequest
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

        public Guid? ClosedByUserId { get; set; }

        [MaxLength(1000)]
        public string? OpeningNotes { get; set; }

        [MaxLength(1000)]
        public string? ClosingNotes { get; set; }
    }
}
