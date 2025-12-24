using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.CashCorrections.Results
{
    public class CashCorrectionResult
    {
        public Guid Id { get; set; }

        public Guid OriginalCashSessionId { get; set; }


        public decimal Amount { get; set; }

        public string Reason { get; set; } = null!;

        public DateTime CorrectedAt { get; set; }

        public Guid CorrectedByUserId { get; set; }

        public Guid ApprovedByUserId { get; set; }

        public DateTime ApprovedAt { get; set; }

        public string? ApprovalNotes { get; set; }
    }
}
