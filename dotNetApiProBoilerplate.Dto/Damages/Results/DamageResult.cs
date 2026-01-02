using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.Damages.Results
{
    public class DamageResult
    {
        public Guid Id { get; set; }

        public string DamageNumber { get; set; } = null!;

        public Guid ProductId { get; set; }

        public decimal Quantity { get; set; }

        public decimal EstimatedValue { get; set; }

        public string Reason { get; set; } = null!;

        public string? Category { get; set; } // Breakage, Expiry, Theft, etc.

        public DateTime DamageDate { get; set; }

        public bool IsApproved { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public Guid? ApprovedByUserId { get; set; }

        public string? Photos { get; set; } // JSON array of photo URLs

        public string? Notes { get; set; }
    }
}
