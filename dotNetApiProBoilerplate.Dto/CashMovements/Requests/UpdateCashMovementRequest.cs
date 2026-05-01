using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.CashMovements.Requests
{
    public class UpdateCashMovementRequest
    {
        public Guid Id { get; set; }

        [Required]
        public Guid CashSessionId { get; set; }

        [Required]
        public CashMovementType Type { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceBefore { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceAfter { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        public DateTime MovementDate { get; set; }
        public Guid? ReferenceId { get; set; }
        public string? ReferenceType { get; set; }
    }
}
