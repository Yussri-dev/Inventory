using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.Damages.Results
{
    public class DamageResult
    {
        public Guid Id { get; set; }

        public string DamageNumber { get; set; } = null!;

        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = null!;

        public decimal Quantity { get; set; }
        public decimal EstimatedValue { get; set; }

        public string Reason { get; set; } = null!;
        public string? Category { get; set; }

        public DateTime DamageDate { get; set; }

        public DamageStatus Status { get; set; }

        public DateTime? ValidatedAt { get; set; }
        public Guid? ValidatedByUserId { get; set; }

        public string? Photos { get; set; }
        public string? Notes { get; set; }
    }
}
