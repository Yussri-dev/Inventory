
using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.CashSessions.Requests
{
    public sealed class CreateCashSessionRequest
    {
        /*
         * Generated once by the client and reused for every retry.
         */
        [Required]
        public Guid ClientOperationId { get; set; }

        /*
         * Original local opening time. The server preserves it when an
         * offline session is uploaded later.
         */
        public DateTime? OpenedAtUtc { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "999999999999999.99",
            ErrorMessage =
                "Opening amount must be non-negative.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal OpeningAmount { get; set; }

        [MaxLength(1000)]
        public string? OpeningNotes { get; set; }
    }

    public sealed class CloseCashSessionRequest
    {
        [Range(
            typeof(decimal),
            "0",
            "999999999999999.99",
            ErrorMessage =
                "Actual cash must be non-negative.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ActualCash { get; set; }

        [MaxLength(1000)]
        public string? ClosingNotes { get; set; }
    }
}
